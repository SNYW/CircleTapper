using Progression;
using Core;
using System;
using System.Collections;
using Gameplay;
using Managers;
using UnityEngine;
using Persistence;
using Random = UnityEngine.Random;

public class Circle : BoardObject
{
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

        StartCoroutine(SpawnCurrency());
    }

    private IEnumerator SpawnCurrency()
    {
        while (gameObject.activeSelf)
        {
            yield return new WaitForSeconds(spawnCooldown);

            if (_emitter.TryEmit(transform.position)) SaveObjectState();
        }
    }

    public override void OnTap()
    {
        if (currentValue <= 0) return;
        
        currentValue = Mathf.Clamp(currentValue - 1, 0, startValue);
        bool isComplete = currentValue <= 0;
        StartCoroutine(BounceScale(isComplete ? 1.6f : 1.2f));

        _progress.Target = RemovedFraction;
        FMODUnity.RuntimeManager.PlayOneShotAttached(CircleTapSFX, gameObject);

        SaveObjectState();
    }

    private IEnumerator BounceScale(float scaleMult)
    {
        Vector3 original = Vector3.one * 0.5f;
        Vector3 peak = transform.localScale * scaleMult;
        float t = 0;
        float duration = 0.2f;

        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(peak, original, t / duration);
            yield return null;
        }

        transform.localScale = original;
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
        SystemEventManager.Send(SystemEventManager.GameEvent.CircleComplete, this);
        FMODUnity.RuntimeManager.PlayOneShotAttached(CircleCompleteSFX, gameObject);
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
        StopAllCoroutines();

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