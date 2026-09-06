using Core;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Persistence;
using UnityEngine;

public class Square : BoardObject
{
    private const float TickSeconds = 1f;
    private const float PopSeconds = 0.1f;
    private const float PopScale = 1.2f;

    public int clickSpeed;
    public List<ParticleSystem> clickParticles;
    public SpriteRenderer spriteRenderer;

    private int _remainingCooldown;
    private SegmentProgress _progress;

    public List<GridManager.Direction> tapTargets;

    public FMODUnity.EventReference SquareCompleteSFX;

    public override void Init()
    {
        Init(clickSpeed);
    }

    public void Init(int remainingCooldown)
    {
        _remainingCooldown = remainingCooldown;
        _progress = new SegmentProgress(spriteRenderer, clickSpeed, CooldownFraction);
        
        if (parentCell == null)
            GridManager.GetClosestCell(transform.position).SetChildObject(this);

        ClickNeighboursLoop(RestartLoops()).Forget();
    }

    public override void BeginDrag(Vector2 startPos)
    {
        // The cooldown pauses while the square is in the air, as stopping the coroutine used to.
        StopLoops();
        base.BeginDrag(startPos);
    }

    public override void EndDrag(Vector2 eventData)
    {
        ClickNeighboursLoop(RestartLoops()).Forget();
        base.EndDrag(eventData);
    }

    private async UniTaskVoid ClickNeighboursLoop(CancellationToken token)
    {
        TimeSpan tick = TimeSpan.FromSeconds(TickSeconds);
        TimeSpan pop = TimeSpan.FromSeconds(PopSeconds);

        try
        {
            while (true)
            {
                await UniTask.Delay(tick, cancellationToken: token);

                _remainingCooldown--;
                _progress.Target = Mathf.Clamp01(CooldownFraction);
                SaveObjectState();

                if (_remainingCooldown > 0) continue;
                if (parentCell == null) continue;

                FireAtNeighbours();

                foreach (ParticleSystem clickParticle in clickParticles) clickParticle.Play();

                // Awaiting the pop delays the next tick, so a full cycle really takes
                // clickSpeed + PopSeconds. Preserved from the coroutine deliberately — dropping
                // it would silently speed every square up.
                transform.localScale = Vector3.one * PopScale;
                await UniTask.Delay(pop, cancellationToken: token);
                transform.localScale = Vector3.one;

                _remainingCooldown = clickSpeed;
                _progress.Target = 1f;
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation also fires when the square is destroyed — by a merge or a delete — and
            // it arrives a tick later, by which point the object is gone. Only the drag case has
            // anything left to tidy, so check the object still exists before touching it.
            if (this != null) transform.localScale = Vector3.one;
        }
    }

    private void FireAtNeighbours()
    {
        foreach (GridManager.Direction direction in tapTargets)
        {
            if (!parentCell.Neighbors.TryGetValue(direction, out GridCell neighbor)) continue;

            // The old version played this from both branches of an if/else on the same condition.
            if (neighbor.heldObject is Circle circle) circle.OnTap();
            FMODUnity.RuntimeManager.PlayOneShotAttached(SquareCompleteSFX, gameObject);
        }
    }

    /// <summary>Proportion of the cooldown still to run.</summary>
    private float CooldownFraction => (float)_remainingCooldown / clickSpeed;

    private void Update() => _progress.Advance(Time.deltaTime);

    public override BoardObjectSaveData ToSaveData()
    {
        return new BoardObjectSaveData
        {
            type = BoardObjectType.Square.ToString(),
            value = _remainingCooldown,
            level = chainLevel,
            carryoverValue = _remainingCooldown,
            xPosition = parentCell.gridPosition.x,
            yPosition = parentCell.gridPosition.y
        };
    }

    public override void FromSaveData(BoardObjectSaveData saveData)
    {
        _remainingCooldown = saveData.value;
        GridManager.GetGridCell(new Vector2Int(saveData.xPosition, saveData.yPosition)).SetChildObject(this);
        Init(saveData.value);
    }

    public override string GetValue()
    {
        return _remainingCooldown.ToString();
    }

    public override string GetMaterialValue() => _progress.ToDebugString();
}