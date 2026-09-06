using System.Collections.Generic;
using Core;
using DG.Tweening;
using Persistence;
using UnityEngine;

/// <summary>
/// Charges from every circle completion on the board, then fires a beam straight up, ticking each
/// circle in the column above it.
/// <para>
/// Global input, local output — the opposite of the hex, which takes from its neighbours and pays
/// out to the player. You position a triangle for where it <i>delivers</i>, not where it receives,
/// and its range grows by merging.
/// </para>
/// </summary>
public class Triangle : BoardObject
{
    [Header("Beam")]
    [Tooltip("Cells above this one the beam reaches. Clamped to the height of the grid.")]
    public int beamRange = 2;

    public SpriteRenderer spriteRenderer;
    public LineRenderer beam;
    public List<ParticleSystem> fireParticles;

    public FMODUnity.EventReference BeamFireSFX;

    /// <summary>
    /// Circle completions needed to fire, fixed rather than per-tier: the charge ring is drawn
    /// around a triangle, and more than three segments renders wrong. Range is what merging
    /// upgrades — tiers differ in reach, not in how long they take to charge.
    /// </summary>
    private const int CompletionsToCharge = 3;

    private const float BeamFadeSeconds = 0.6f;
    private const float BeamStartWidth = 0.25f;

    /// <summary>Reused so a firing beam does not allocate.</summary>
    private readonly List<GridCell> _column = new();

    private SegmentProgress _progress;
    private Tween _beamFade;
    private int _remainingCharge;

    /// <summary>Never reaches past the top of the grid, however far a merged triangle claims.</summary>
    private int EffectiveRange => Mathf.Min(beamRange, Mathf.Max(GridManager.Height, 1));

    /// <summary>Proportion of the charge still to go, for the ring.</summary>
    private float ChargeFraction => (float)_remainingCharge / CompletionsToCharge;

    public override void Init() => Init(CompletionsToCharge);

    private void Init(int remainingCharge)
    {
        _remainingCharge = remainingCharge;
        _progress = new SegmentProgress(spriteRenderer, CompletionsToCharge, ChargeFraction);

        // Left enabled at zero width rather than toggled, so nothing has to run when the fade
        // ends — see the no-logic-in-OnComplete rule in CLAUDE.md.
        if (beam != null)
        {
            beam.widthMultiplier = 0f;
            beam.enabled = true;
        }

        if (parentCell == null) GridManager.GetClosestCell(transform.position).SetChildObject(this);
    }

    protected override void OnEnabled()
    {
        if (ServiceLocator.TryGet(out TriangleChargeService charge)) charge.Register(this);
    }

    protected override void OnDisabled()
    {
        if (ServiceLocator.TryGet(out TriangleChargeService charge)) charge.Unregister(this);
    }

    public override void Tick(float deltaTime)
    {
        if (_progress == null) return;

        _progress.Advance(deltaTime);
    }

    /// <summary>
    /// Called for every circle that completes anywhere, by <see cref="TriangleChargeService"/>.
    /// </summary>
    public void OnCircleCompletedAnywhere(Circle circle)
    {
        // Unparented mid-drag: it is off the board, so it should not be charging.
        if (parentCell == null) return;

        _remainingCharge--;
        _progress.Target = Mathf.Clamp01(ChargeFraction);
        SaveObjectState();

        if (_remainingCharge > 0) return;

        Fire();

        _remainingCharge = CompletionsToCharge;
        _progress.Target = 1f;
    }

    private void Fire()
    {
        GridManager.CollectColumnAbove(parentCell.gridPosition, EffectiveRange, _column);

        // No blocking: the beam passes through whatever is in the way and ticks every circle.
        foreach (GridCell cell in _column)
        {
            if (cell.heldObject is Circle circle) circle.OnTap();
        }

        ShowBeam();

        foreach (ParticleSystem particle in fireParticles) particle.Play();

        FMODUnity.RuntimeManager.PlayOneShotAttached(BeamFireSFX, gameObject);
    }

    /// <summary>
    /// Placeholder visual: a line up the column that fades out. Meant to be replaced by something
    /// with an actual shader behind it.
    /// </summary>
    private void ShowBeam()
    {
        if (beam == null) return;

        Vector3 start = transform.position;
        Vector3 end = _column.Count > 0
            ? _column[_column.Count - 1].transform.position
            : start + Vector3.up * EffectiveRange;

        beam.positionCount = 2;
        beam.SetPosition(0, start);
        beam.SetPosition(1, end);

        // Tracked explicitly: DOTween.To has no target, so beam.DOKill() would not find it.
        _beamFade?.Kill();
        beam.widthMultiplier = BeamStartWidth;

        // InQuad holds the beam wide before dropping away, so it reads as a discharge rather
        // than a flicker.
        _beamFade = DOTween
            .To(() => beam.widthMultiplier, width => beam.widthMultiplier = width, 0f, BeamFadeSeconds)
            .SetEase(Ease.InQuad)
            .SetLink(gameObject);
    }

    public override BoardObjectSaveData ToSaveData()
    {
        return new BoardObjectSaveData
        {
            type = BoardObjectType.Triangle.ToString(),
            value = _remainingCharge,
            level = chainLevel,
            carryoverValue = 0,
            xPosition = parentCell.gridPosition.x,
            yPosition = parentCell.gridPosition.y
        };
    }

    public override void FromSaveData(BoardObjectSaveData saveData)
    {
        _remainingCharge = saveData.value;

        GridManager.GetGridCell(new Vector2Int(saveData.xPosition, saveData.yPosition)).SetChildObject(this);
        Init(saveData.value);
        SaveObjectState();
    }

    public override string GetValue() => _remainingCharge.ToString();

    public override string GetMaterialValue() => _progress.ToDebugString();
}
