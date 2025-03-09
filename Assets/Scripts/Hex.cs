using System;
using System.Collections;
using System.Collections.Generic;
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
    private int _neighbourValue;

    private int _remainingCooldown;
    private float LerpValue => (float)_remainingCooldown / clickSpeed;

    public List<GridManager.Direction> tapTargets;

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
        StartCoroutine(ClickNeighbours());
        StartCoroutine(SpawnCurrency());
        UpdateNeighbourValue();

        LerpRemovedSegments(LerpValue);
    }

    private void Update()
    {
       UpdateNeighbourValue();
    }

    private void UpdateNeighbourValue()
    {
        _neighbourValue = 0;
        if(parentCell == null) return;
        
        foreach (var dir in tapTargets)
        {
            if (parentCell.Neighbors.TryGetValue(dir, out var bo) && bo.heldObject is Circle circle)
            {
                _neighbourValue += circle.startValue;
            }
        }

        neighbourValueText.text = _neighbourValue.ToString();
    }

    public override void BeginDrag(Vector2 startPos)
    {
        StopAllCoroutines();
        base.BeginDrag(startPos);
    }

    public override void EndDrag(Vector2 eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ClickNeighbours());
        StartCoroutine(SpawnCurrency());
        base.EndDrag(eventData);
    }

    private IEnumerator ClickNeighbours()
    {
        while (gameObject.activeSelf)
        {
            yield return new WaitForSeconds(1);

            _remainingCooldown--;
            LerpRemovedSegments(LerpValue);
            SaveObjectState();

            if (_remainingCooldown > 0) continue;
            if(parentCell == null) continue;
            
            if (_neighbourValue != 0)
            {
                _particlesToSpawn += _neighbourValue;

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
            yield return new WaitForSeconds(0.1f);

            if (_particlesToSpawn <= 0) continue;

            var randPos = transform.position + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), 0);
            Instantiate(particle, randPos, Quaternion.identity);
            _particlesToSpawn--;
            if(parentCell!= null)
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
            carryoverValue = _particlesToSpawn,
            xPosition = parentCell.gridPosition.x,
            yPosition = parentCell.gridPosition.y
        };
    }

    public override void FromSaveData(BoardObjectSaveData saveData)
    {
        DOTween.KillAll(gameObject);
        StopAllCoroutines();
        
        _remainingCooldown = saveData.value;
        _particlesToSpawn = saveData.carryoverValue;
        GridManager.GetGridCell(new Vector2Int(saveData.xPosition, saveData.yPosition)).SetChildObject(this);
        Init(saveData.value);
    }
    
    protected override void SaveObjectState()
    {
        if(parentCell != null)
            SaveManager.Instance.AddObject(parentCell.gridPosition, ToSaveData());
    }
}
