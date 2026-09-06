using Progression;
using Core;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Gameplay;
using Managers;
using UnityEngine;
using Persistence;
using Random = UnityEngine.Random;

public class Circle : BoardObject
{
    private const float TapBounceScale = 1.2f;
    private const float CompleteBounceScale = 1.6f;
    private const float BounceSeconds = 0.2f;
    private static readonly Vector3 RestScale = Vector3.one * 0.5f;

    public int startValue;
    public int currentValue;
    public SpriteRenderer spriteRenderer;

    public float spawnCooldown;

    private readonly CurrencyEmitter _emitter = new();
    private SegmentProgress _progress;

    public FMODUnity.EventReference CircleCompleteSFX;
    public FMODUnity.EventReference CircleTapSFX;

    public override void Init()
    {
        Init(startValue);
    }

    private void Init(int initCurrentValue)
    {
        currentValue = initCurrentValue;
        _progress = new SegmentProgress(spriteRenderer, startValue, RemovedFraction);

        EmitCurrencyLoop(RestartLoops()).Forget();
    }

    /// <summary>
    /// Pays banked currency out a particle at a time. The interval runs whether or not anything
    /// is owed, which is what the coroutine this replaced did.
    /// </summary>
    private async UniTaskVoid EmitCurrencyLoop(CancellationToken token)
    {
        TimeSpan interval = TimeSpan.FromSeconds(spawnCooldown);

        try
        {
            while (true)
            {
                await UniTask.Delay(interval, cancellationToken: token);

                if (_emitter.TryEmit(transform.position)) SaveObjectState();
            }
        }
        catch (OperationCanceledException)
        {
            // The circle was dropped, disabled or destroyed. Nothing to clean up.
        }
    }

    public override void OnTap()
    {
        if (currentValue <= 0) return;
        
        currentValue = Mathf.Clamp(currentValue - 1, 0, startValue);
        bool isComplete = currentValue <= 0;
        Bounce(isComplete ? CompleteBounceScale : TapBounceScale);

        _progress.Target = RemovedFraction;
        FMODUnity.RuntimeManager.PlayOneShotAttached(CircleTapSFX, gameObject);

        SaveObjectState();
    }

    /// <summary>Snaps up, then eases linearly back to rest — the same curve as the hand-rolled lerp.</summary>
    private void Bounce(float scaleMultiplier)
    {
        transform.DOKill();
        transform.localScale *= scaleMultiplier;
        transform
            .DOScale(RestScale, BounceSeconds)
            .SetEase(Ease.Linear)
            .SetLink(gameObject);
    }

    public int GetPointValue()
    {
        const string upgradeName = "Circle Value +2";

        if (!ServiceLocator.Get<UpgradeCatalog>()
                .TryGet(upgradeName, out CircleCompletionBonusUpgradeDefinition def)) return startValue;

        return startValue + ServiceLocator.Get<UpgradeService>().GetLevel(upgradeName) * def.bonusPerLevel;
    }

    private void Complete()
    {
        _emitter.Add(GetPointValue());
        currentValue = startValue;
        _progress.Target = 0f;

        NotifyWatchingHexes();
        FMODUnity.RuntimeManager.PlayOneShotAttached(CircleCompleteSFX, gameObject);
    }

    /// <summary>
    /// Tells any adjacent hex directly. This used to be a global broadcast that every hex on the
    /// board filtered for itself, so the cost scaled with the number of hexes; walking this
    /// circle's own neighbours is at most eight lookups however big the board gets.
    /// </summary>
    private void NotifyWatchingHexes()
    {
        if (parentCell?.Neighbors == null) return;

        foreach (KeyValuePair<GridManager.Direction, GridCell> neighbour in parentCell.Neighbors)
        {
            if (neighbour.Value.heldObject is Hex hex)
            {
                hex.OnWatchedCircleCompleted(this, neighbour.Key);
            }
        }
    }

    /// <summary>How much of the ring is gone, 0 when untouched and 1 when the circle is done.</summary>
    private float RemovedFraction => 1f - (float)currentValue / startValue;

    private void Update()
    {
        _progress.Advance(Time.deltaTime);

        if (_progress.IsFull) Complete();
    }

    public override BoardObjectSaveData ToSaveData()
    {
        return new BoardObjectSaveData
        {
            type = BoardObjectType.Circle.ToString(),
            value = currentValue,
            level = chainLevel,
            carryoverValue = _emitter.Pending,
            xPosition = parentCell.gridPosition.x,
            yPosition = parentCell.gridPosition.y
        };
    }

    public override void FromSaveData(BoardObjectSaveData saveData)
    {
        var gridCell = GridManager.GetGridCell(new Vector2Int(saveData.xPosition, saveData.yPosition));
        if (gridCell == null)
        {
            Debug.LogError($"Tried to spawn an item on a populated position {saveData.xPosition},{saveData.yPosition}");
            return;
        }
        
        _emitter.Clear();
        _emitter.Add(saveData.carryoverValue);
        gridCell.SetChildObject(this);
        Init(saveData.value);
        SaveObjectState();
    }

    public override string GetValue() => currentValue.ToString();
    public override string GetMaterialValue() => _progress.ToDebugString();
}