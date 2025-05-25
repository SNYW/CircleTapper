using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using Persistence;
using Random = UnityEngine.Random;

public class Circle : BoardObject
{
    public int startValue;
    public int currentValue;
    public SpriteRenderer spriteRenderer;

    [Header("Particle Spawn")] public CurrencyParticle particle;
    public float spawnCooldown;
    private int _particlesToSpawn;

    private MaterialPropertyBlock _propertyBlock;
    private static readonly int RemovedSegments = Shader.PropertyToID("_RemovedSegments");
    private static readonly int SegmentCount = Shader.PropertyToID("_SegmentCount");

    private float currentRemovedSegments;
    private float targetRemovedSegments;
    private float lerpSpeed = 10f;

    public FMODUnity.EventReference CircleCompleteSFX;
    public FMODUnity.EventReference CircleTapSFX;

    public override void Init()
    {
        Init(startValue);
    }

    private void Init(int initCurrentValue)
    {
        currentValue = initCurrentValue;

        _propertyBlock = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(_propertyBlock);

        _propertyBlock.SetFloat(SegmentCount, startValue);
        currentRemovedSegments = 1f - (float)currentValue / startValue;
        targetRemovedSegments = currentRemovedSegments;
        _propertyBlock.SetFloat(RemovedSegments, currentRemovedSegments);
        spriteRenderer.SetPropertyBlock(_propertyBlock);

        StartCoroutine(SpawnCurrency());
    }

    private IEnumerator SpawnCurrency()
    {
        while (gameObject.activeSelf)
        {
            yield return new WaitForSeconds(spawnCooldown);

            if (_particlesToSpawn <= 0) continue;

            var randPos = transform.position + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), 0);
            Instantiate(particle, randPos, Quaternion.identity);
            _particlesToSpawn--;
            if (parentCell != null)
                SaveObjectState();
        }
    }

    public override void OnTap()
    {
        if (currentValue <= 0) return;
        
        currentValue = Mathf.Clamp(currentValue - 1, 0, startValue);
        bool isComplete = currentValue <= 0;
        StartCoroutine(BounceScale(isComplete ? 1.6f : 1.2f));

        targetRemovedSegments = 1f - (float)currentValue / startValue;
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

    private void Complete()
    {
        _particlesToSpawn += startValue;
        currentValue = startValue;
        targetRemovedSegments = 0f;
        SystemEventManager.Send(SystemEventManager.GameEvent.CircleComplete, this);
        FMODUnity.RuntimeManager.PlayOneShotAttached(CircleCompleteSFX, gameObject);
    }

    private void Update()
    {
        if (Mathf.Approximately(currentRemovedSegments, targetRemovedSegments) && !Mathf.Approximately(currentRemovedSegments, 1f)) return;

        currentRemovedSegments = Mathf.MoveTowards(currentRemovedSegments, targetRemovedSegments, Time.deltaTime * lerpSpeed);

        _propertyBlock.SetFloat(RemovedSegments, currentRemovedSegments);
        spriteRenderer.SetPropertyBlock(_propertyBlock);

        if (Mathf.Approximately(currentRemovedSegments, 1f))
        {
            Complete();
        }
    }

    public override BoardObjectSaveData ToSaveData()
    {
        return new BoardObjectSaveData
        {
            type = BoardObjectType.Circle.ToString(),
            value = currentValue,
            level = chainLevel,
            carryoverValue = _particlesToSpawn,
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
        
        _particlesToSpawn = saveData.carryoverValue;
        gridCell.SetChildObject(this);
        Init(saveData.value);
        SaveObjectState();
    }

    protected override void SaveObjectState()
    {
        if (parentCell != null) SaveManager.Instance.AddObject(parentCell.gridPosition, ToSaveData());
    }

    public override string GetValue() => currentValue.ToString();
    public override string GetMaterialValue() => _propertyBlock.GetFloat(RemovedSegments).ToString(CultureInfo.InvariantCulture);
}