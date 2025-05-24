using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using Persistence;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class Hex : BoardObject
{
    public int clickSpeed;
    public List<ParticleSystem> clickParticles;
    public SpriteRenderer spriteRenderer;
    public TMP_Text neighbourValueText;

    private MaterialPropertyBlock _propertyBlock;
    private static readonly int RemovedSegments = Shader.PropertyToID("_RemovedSegments");
    private static readonly int SegmentCount = Shader.PropertyToID("_SegmentCount");
    
    [Header("Particle Spawn")] 
    public CurrencyParticle particle;

    private int _particlesToSpawn;
    private int _storedParticles;

    private int _remainingCooldown;
    private float LerpValue => (float)_remainingCooldown / clickSpeed;

    public List<GridManager.Direction> tapTargets;

    public FMODUnity.EventReference HexCompleteSFX; //audio

    private void Awake()
    {
        SystemEventManager.Subscribe(SystemEventManager.GameEvent.CircleComplete, OnCircleComplete);
    }

    private void OnCircleComplete(object obj)
    {
        if (obj is not Circle c) return;
        if (parentCell == null) return;

        foreach (var dir in tapTargets)
        {
            if (!parentCell.Neighbors.TryGetValue(dir, out var cell)) continue;

            if (cell != c.parentCell) continue;
            
            _storedParticles += c.startValue;
            
        
            neighbourValueText.text = _storedParticles.ToString();
            _remainingCooldown--;
            LerpRemovedSegments(LerpValue);
            SaveObjectState();

            if (_remainingCooldown > 0) return;
            if(parentCell == null) return;
            
            if (_storedParticles != 0)
            {
                _particlesToSpawn += _storedParticles;
                _storedParticles = 0;
                neighbourValueText.text = "0";
                FMODUnity.RuntimeManager.PlayOneShotAttached(HexCompleteSFX, gameObject); //audio

                foreach (var clickParticle in clickParticles)
                {
                    clickParticle.Play();
                }
            }
                
            transform.DOScale(1.2f, 0.1f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => transform.DOScale(1f, 0.1f)
                    .SetEase(Ease.InQuad)
                    .SetTarget(gameObject))
                .SetTarget(gameObject);
                
            _remainingCooldown = clickSpeed;
            LerpRemovedSegments(1);
        }
    }

    public override void Init()
    {
        Init(clickSpeed);
    }

    private void Init(int remainingCooldown)
    {
        _remainingCooldown = remainingCooldown;
        _propertyBlock = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(_propertyBlock);
        
        _propertyBlock.SetFloat(SegmentCount, clickSpeed);
        _propertyBlock.SetFloat(RemovedSegments, LerpValue);
        
        spriteRenderer.SetPropertyBlock(_propertyBlock);
        
        if(parentCell == null)
            GridManager.GetClosestCell(transform.position).SetChildObject(this);
        StartCoroutine(SpawnCurrency());
        neighbourValueText.text = _storedParticles.ToString();

        LerpRemovedSegments(LerpValue);
    }

    public override void BeginDrag(Vector2 startPos)
    {
        StopAllCoroutines();
        base.BeginDrag(startPos);
    }

    public override void EndDrag(Vector2 eventData)
    {
        StopAllCoroutines();
        StartCoroutine(SpawnCurrency());
        base.EndDrag(eventData);
    }
    
    private void LerpRemovedSegments(float newValue)
    {
        if (spriteRenderer == null) return;
        
        spriteRenderer.GetPropertyBlock(_propertyBlock);
        
        if (_propertyBlock.HasProperty(RemovedSegments))
        {
            float currentRemovedSegments = _propertyBlock.GetFloat(RemovedSegments);

            DOTween.To(() => currentRemovedSegments, value =>
                {
                    _propertyBlock.SetFloat(RemovedSegments, value);
                    spriteRenderer.SetPropertyBlock(_propertyBlock);
                }, newValue, 0.2f)
                .SetTarget(gameObject);
        }
        else
        {
            Debug.LogWarning("MaterialPropertyBlock does not have a '_RemovedSegments' property.");
        }
    }
    
    private IEnumerator SpawnCurrency()
    {
        while (gameObject.activeSelf)
        {
            yield return new WaitForSeconds(0.01f);

            if (_particlesToSpawn <= 0) continue;

            var randPos = transform.position + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), 0);
            Instantiate(particle, randPos, Quaternion.identity);
            _particlesToSpawn--;            
            if (parentCell!= null)
                SaveObjectState();
        }
    }
    
    public override BoardObjectSaveData ToSaveData()
    {
        return new BoardObjectSaveData
        {
            type = BoardObjectType.Hex.ToString(),
            value = _remainingCooldown,
            level = chainLevel,
            carryoverValue = _storedParticles,
            xPosition = parentCell.gridPosition.x,
            yPosition = parentCell.gridPosition.y
        };
    }

    public override void FromSaveData(BoardObjectSaveData saveData)
    {
        DOTween.KillAll(gameObject);
        StopAllCoroutines();
        
        _remainingCooldown = saveData.value;
        _storedParticles = saveData.carryoverValue;
        GridManager.GetGridCell(new Vector2Int(saveData.xPosition, saveData.yPosition)).SetChildObject(this);
        Init(saveData.value);
    }
    
    protected override void SaveObjectState()
    {
        if(parentCell != null)
            SaveManager.Instance.AddObject(parentCell.gridPosition, ToSaveData());
    }
    
    public override string GetValue()
    {
        return _remainingCooldown.ToString();
    }

    public override string GetMaterialValue()
    {
        return _propertyBlock.GetFloat(RemovedSegments).ToString(CultureInfo.InvariantCulture);
    }
}
