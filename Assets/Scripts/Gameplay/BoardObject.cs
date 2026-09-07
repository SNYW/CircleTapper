using Progression;
using Core;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Managers;
using Persistence;
using Unity.Mathematics;
using UnityEngine;

public abstract class BoardObject : MonoBehaviour, ISaveable
{
    /// <summary>Scale the merged object springs up from, as a fraction of its resting size.</summary>
    private const float MergePopFrom = 0.4f;

    private const float MergePopSeconds = 0.35f;

    /// <summary>How much the object under the finger swells while it would merge if dropped.</summary>
    private const float CandidatePulseScale = 1.15f;

    private const float CandidatePulseSeconds = 0.3f;

    /// <summary>How long the displaced object takes to slide into the vacated cell.</summary>
    private const float SwapSeconds = 0.2f;

    public int chainLevel;
    public GridCell parentCell;
    public BoardObject onMergeSpawn;
    public List<GameObject> influenceIndicators;

    public FMODUnity.EventReference MergeObjectSFX;

    private CancellationTokenSource _loopSource;

    private GridCell _dragOriginCell;
    private BoardObject _mergeCandidate;
    private Vector3 _candidateRestScale;
    private Tween _candidatePulse;

    private void OnEnable()
    {
        SetIndicators(false);

        if (ServiceLocator.TryGet(out BoardObjectTickService ticks)) ticks.Register(this);

        OnEnabled();
        SystemEventManager.Send(SystemEventManager.GameEvent.BoardChanged, this);
    }

    /// <summary>
    /// Enable/disable hooks for subclasses.
    /// <para>
    /// Declaring OnEnable or OnDisable in a subclass would HIDE this class's, because Unity
    /// dispatches a message to the most-derived declaration only — Hex did exactly that and
    /// silently stopped stopping its own loops. Override these instead.
    /// </para>
    /// </summary>
    protected virtual void OnEnabled() { }

    protected virtual void OnDisabled() { }

    /// <summary>
    /// Per-frame work, driven by <see cref="BoardObjectTickService"/> rather than an Update of
    /// this object's own.
    /// </summary>
    public virtual void Tick(float deltaTime) { }

    public virtual void Init() { }

    public virtual void BeginDrag(Vector2 touchPosition)
    {
        // Remembered so a drop onto an occupied cell can send its occupant back here.
        _dragOriginCell = parentCell;
        parentCell?.RemoveChildObject();
    }

    public virtual void OnDrag(Vector2 worldPosition)
    {
        SetIndicators(true);
        transform.position = worldPosition;

        TryGetMergeTarget(worldPosition, out BoardObject target);
        SetMergeCandidate(target);
    }

    public virtual void EndDrag(Vector2 touchPosition)
    {
        SetMergeCandidate(null);

        if (TryGetMergeTarget(touchPosition, out BoardObject mergeTarget))
        {
            _dragOriginCell = null;
            OnMerge(mergeTarget);
            return;
        }

        GridCell cell = GridManager.GetClosestCell(touchPosition, true);
        if (cell == null) return;

        BoardObject occupant = cell.heldObject;

        if (occupant != null && occupant != this)
        {
            if (TrySwapWith(cell, occupant)) return;

            // Nowhere to send it, so settle for the nearest free cell instead.
            cell = GridManager.GetClosestCell(touchPosition);
            if (cell == null) return;
        }

        cell.SetChildObject(this);
        SetIndicators(false);
        _dragOriginCell = null;
    }

    /// <summary>
    /// Trades places with whatever is already here, sending it back to the cell this object was
    /// picked up from. Dropping onto something used to bounce the dragged object off to some
    /// other free cell, which is not where the player aimed.
    /// </summary>
    private bool TrySwapWith(GridCell cell, BoardObject occupant)
    {
        // Nothing to swap into: this was not dragged off the board, or its old cell was taken.
        if (_dragOriginCell == null || _dragOriginCell.heldObject != null) return false;
        if (_dragOriginCell == cell) return false;

        Vector3 occupantFrom = occupant.transform.position;

        // Clear the target first. SetChildObject removes whatever it finds, so moving the
        // occupant before this would simply be undone a line later.
        cell.RemoveChildObject();

        _dragOriginCell.SetChildObject(occupant);
        cell.SetChildObject(this);

        // SetChildObject snaps position, so put it back and slide it across.
        occupant.transform.position = occupantFrom;
        occupant.transform.DOKill();
        occupant.transform
            .DOMove(_dragOriginCell.transform.position, SwapSeconds)
            .SetEase(Ease.OutQuad)
            .SetLink(occupant.gameObject);

        SetIndicators(false);
        _dragOriginCell = null;
        return true;
    }

    /// <summary>
    /// Whether dropping here would merge, and into what. One definition used both to preview the
    /// merge mid-drag and to perform it on release, so the two can never disagree.
    /// </summary>
    private bool TryGetMergeTarget(Vector2 position, out BoardObject target)
    {
        target = null;

        if (onMergeSpawn == null) return false;

        GridCell cell = GridManager.GetClosestCell(position, true);
        BoardObject held = cell != null ? cell.heldObject : null;

        if (held == null || held == this) return false;
        if (held.GetType() != GetType()) return false;
        if (held.gameObject.name != gameObject.name) return false;

        target = held;
        return true;
    }

    /// <summary>
    /// Swells whatever would be merged into, so the outcome is visible before committing to it.
    /// </summary>
    private void SetMergeCandidate(BoardObject candidate)
    {
        if (candidate == _mergeCandidate) return;

        // Put the previous one back before adopting a new one.
        if (_mergeCandidate != null)
        {
            _candidatePulse?.Kill();
            _mergeCandidate.transform.localScale = _candidateRestScale;
        }

        _candidatePulse = null;
        _mergeCandidate = candidate;

        if (_mergeCandidate == null) return;

        _candidateRestScale = _mergeCandidate.transform.localScale;
        _candidatePulse = _mergeCandidate.transform
            .DOScale(_candidateRestScale * CandidatePulseScale, CandidatePulseSeconds)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(_mergeCandidate.gameObject);
    }

    public virtual void OnTap() { }

    public virtual void OnMerge(BoardObject targetObj)
    {
        Vector3 mergePosition = targetObj.transform.position;

        BoardObject newItem = Instantiate(onMergeSpawn, mergePosition, quaternion.identity);
        ServiceLocator.Get<SaveService>().RemoveBoardObject(targetObj.parentCell.gridPosition);
        targetObj.parentCell.SetChildObject(newItem);
        newItem.Init();

        Destroy(targetObj.gameObject);
        Destroy(gameObject);

        if (ServiceLocator.TryGet(out ObjectiveService objectives))
        {
            objectives.Report(ObjectiveGoal.Merge, ObjectType);
        }

        PlayMergeFeedback(newItem, mergePosition);
    }

    /// <summary>
    /// Merging is the best moment in the game and had nothing but a sound, which was itself being
    /// cut short. The new object springs up past its resting size rather than appearing, so the
    /// upgrade reads as an event rather than a swap.
    /// </summary>
    private void PlayMergeFeedback(BoardObject newItem, Vector3 mergePosition)
    {
        Transform spawned = newItem.transform;

        // Its own prefab scale, not Vector3.one — circles rest at half size.
        Vector3 restScale = spawned.localScale;

        spawned.DOKill();
        spawned.localScale = restScale * MergePopFrom;
        spawned
            .DOScale(restScale, MergePopSeconds)
            .SetEase(Ease.OutBack)
            .SetLink(newItem.gameObject);

        if (EffectsManager.Instance != null)
        {
            EffectsManager.Instance.SpawnEffect(EffectsManager.EffectType.Spawn, mergePosition);
        }

        // Unattached and positional: this used to be attached to a GameObject destroyed on the
        // line above, so the sound was being stopped as it started.
        FMODUnity.RuntimeManager.PlayOneShot(MergeObjectSFX, mergePosition);
    }

    /// <summary>
    /// Starts a fresh token for this object's repeating work, cancelling whatever was running.
    /// Linked to the object's own destruction, so a loop can never outlive its board object.
    /// </summary>
    protected CancellationToken RestartLoops()
    {
        StopLoops();
        _loopSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        return _loopSource.Token;
    }

    /// <summary>Cancels the object's repeating work. This is what pausing on drag does.</summary>
    protected void StopLoops()
    {
        if (_loopSource == null) return;

        _loopSource.Cancel();
        _loopSource.Dispose();
        _loopSource = null;
    }

    private void OnDestroy()
    {
        StopLoops();
        // Tweens are linked to their GameObject, so DOTween kills them on destroy by itself.
        SystemEventManager.Send(SystemEventManager.GameEvent.BoardChanged, this);
    }

    private void SetIndicators(bool active)
    {
        if (influenceIndicators is not { Count: > 0 }) return;
        foreach (var indicator in influenceIndicators)
            indicator.gameObject.SetActive(active);
    }

    public abstract BoardObjectSaveData ToSaveData();

    public abstract void FromSaveData(BoardObjectSaveData saveData);

    /// <summary>
    /// Writes this object's state into the save. A no-op while the object is unparented — mid-drag
    /// it belongs to no cell, so there is no position to key it by.
    /// </summary>
    protected void SaveObjectState()
    {
        if (parentCell == null) return;

        ServiceLocator.Get<SaveService>().SetBoardObject(parentCell.gridPosition, ToSaveData());
    }

    private void OnDisable()
    {
        // A drag interrupted by a disable must not leave the target stuck mid-pulse.
        SetMergeCandidate(null);

        StopLoops();
        OnDisabled();

        // TryGet, because on shutdown the locator may already be cleared.
        if (ServiceLocator.TryGet(out BoardObjectTickService ticks)) ticks.Unregister(this);
    }

    /// <summary>What this is, for saving and for objectives that care which kind was used.</summary>
    public abstract BoardObjectType ObjectType { get; }

    public abstract string GetValue(); 
    public abstract string GetMaterialValue();
}