using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
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
    private float LerpValue => 1-(float)currentValue / startValue;
    private static readonly int RemovedSegments = Shader.PropertyToID("_RemovedSegments");
    private static readonly int SegmentCount = Shader.PropertyToID("_SegmentCount");

    public FMODUnity.EventReference CircleCompleteSFX; //audio
    public FMODUnity.EventReference CircleTapSFX; //audio

    public override void Init()
    {
        Init(startValue);
    }

    private void Init(int initCurrentValue)
    {
        if (_propertyBlock != null) return;

        currentValue = initCurrentValue;

        _propertyBlock = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(_propertyBlock);

        _propertyBlock.SetFloat(SegmentCount, startValue);
        _propertyBlock.SetFloat(RemovedSegments, LerpValue);

        spriteRenderer.SetPropertyBlock(_propertyBlock);

        StartCoroutine(SpawnCurrency());
        LerpRemovedSegments(LerpValue);
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
            if(parentCell!= null)
               SaveObjectState();
        }
    }

    public override void OnTap()
    {
        if(currentValue == 0) return;
        
        try
        {
            
            currentValue = Mathf.Clamp(currentValue-1, 0, startValue);
            var isComplete = currentValue <= 0;

            transform.DOScale(isComplete ? 0.65f : 0.52f, 0.1f)
                .SetEase(Ease.OutQuad)
                .SetTarget(gameObject)
                .OnComplete(() =>
                {
                    transform.DOScale(0.5f, 0.1f).SetEase(Ease.InQuad).SetTarget(gameObject);
                    LerpRemovedSegments(LerpValue);
                    FMODUnity.RuntimeManager.PlayOneShotAttached(CircleTapSFX, gameObject); //audio
                });
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        SaveObjectState();
    }

    private void Complete()
    {
        _particlesToSpawn += startValue;
        currentValue = startValue;
        LerpRemovedSegments(0f);
        SystemEventManager.Send(SystemEventManager.GameEvent.CircleComplete, this);
        FMODUnity.RuntimeManager.PlayOneShotAttached(CircleCompleteSFX, gameObject); //audio
    }

    private void LerpRemovedSegments(float newValue)
    {
        spriteRenderer.GetPropertyBlock(_propertyBlock);
        float currentRemovedSegments = _propertyBlock.GetFloat(RemovedSegments);

        DOTween.To(() => currentRemovedSegments, value =>
            {
                _propertyBlock.SetFloat(RemovedSegments, value);
                spriteRenderer.SetPropertyBlock(_propertyBlock);
            }, newValue, 0.1f)
            .SetTarget(gameObject)
            .OnComplete(() =>
            {
                if (Mathf.Approximately(newValue, 1f))
                {
                    Complete();
                }
            });
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

        DOTween.KillAll(gameObject);
        StopAllCoroutines();

        _particlesToSpawn = 0;
        GridManager.GetGridCell(new Vector2Int(saveData.xPosition, saveData.yPosition)).SetChildObject(this);
        Init(saveData.value);
        StartCoroutine(SpawnCurrency());
        SaveObjectState();
    }

    protected override void SaveObjectState()
    {
        if(parentCell != null)
            SaveManager.Instance.AddObject(parentCell.gridPosition, ToSaveData());
    }
}