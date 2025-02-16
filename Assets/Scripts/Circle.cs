using System;
using System.Collections;
using TMPro;
using UnityEngine;
using DG.Tweening;
using Random = UnityEngine.Random;

public class Circle : BoardObject
{
    public TMP_Text numberDisplay;
    public int startValue;
    public int maxValue;
    public int currentValue;
    public SpriteRenderer spriteRenderer;

    [Header("Particle Spawn")] 
    public CurrencyParticle particle;
    public float spawnCooldown;
    private int _particlesToSpawn;

    private int _currentStartValue;
    private bool _canTap = true;
    private MaterialPropertyBlock _propertyBlock;

    private static readonly int RemovedSegments = Shader.PropertyToID("_RemovedSegments");
    private static readonly int SegmentCount = Shader.PropertyToID("_SegmentCount");

    private void Start()
    {
        currentValue = startValue;
        _currentStartValue = startValue;
        numberDisplay.SetText(currentValue.ToString());

        _propertyBlock = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(_propertyBlock);
        
        _propertyBlock.SetFloat(SegmentCount, _currentStartValue);
        _propertyBlock.SetFloat(RemovedSegments, 0f);
        
        spriteRenderer.SetPropertyBlock(_propertyBlock);

        if (ParentCell == null)
            GridManager.GetClosestCell(transform.position).SetChildObject(this);

        StartCoroutine(SpawnCurrency());
    }

    private IEnumerator SpawnCurrency()
    {
        while (gameObject.activeSelf)
        {
            yield return new WaitForSeconds(spawnCooldown);

            if (_particlesToSpawn <= 0) continue;

            var randPos = transform.position + new Vector3(Random.Range(-0.1f,0.1f), Random.Range(-0.1f,0.1f), 0);
            Instantiate(particle, randPos, Quaternion.identity);
            _particlesToSpawn--;
        }
    }

    public override void OnTap()
    {
        if (!_canTap) return;

        _canTap = false;
        
        try
        {
            currentValue--;
            var isComplete = currentValue <= 0;

            float newValue = (float)(_currentStartValue - currentValue) / _currentStartValue;
            LerpRemovedSegments(newValue);
        
            transform.DOScale(isComplete ? 0.65f : 0.52f, 0.1f)
                .SetEase(Ease.OutQuad)
                .OnComplete(
                    () =>
                    {
                        transform.DOScale(0.5f, 0.1f)
                            .SetEase(Ease.InQuad).SetTarget(gameObject);

                        _canTap = !isComplete;
                    })
                .SetTarget(gameObject);
        }
        catch (Exception e)
        {
            //Fuck Exceptions
        }
        
        numberDisplay.SetText(Mathf.Clamp(currentValue, 0,maxValue).ToString());
    }

    private void Complete()
    {
        _canTap = false;
        _particlesToSpawn += _currentStartValue;
        _currentStartValue = Mathf.Clamp(_currentStartValue * 2, 0, maxValue);
        currentValue = _currentStartValue;

        try
        {
            _propertyBlock.SetFloat(SegmentCount, _currentStartValue);
            spriteRenderer.SetPropertyBlock(_propertyBlock);
            LerpRemovedSegments(0f);
        }
        catch (Exception e)
        {
            //Ignored, can happen on merge
        }
        
        numberDisplay.SetText(Mathf.Clamp(currentValue, 0,maxValue).ToString());
    }

    private void LerpRemovedSegments(float newValue)
    {
        _canTap = false;
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
                .SetTarget(gameObject)
                .OnComplete(() =>
                {
                    if (Mathf.Approximately(newValue, 1))
                        Complete();

                    _canTap = true;
                });
        }
        else
        {
            Debug.LogWarning("MaterialPropertyBlock does not have a '_RemovedSegments' property.");
        }
    }

    public override BoardObjectSaveData ToSaveData()
    {
        return new CircleSaveData
        {
            currentValue = currentValue,
            level = chainLevel,
            particlesToSpawn = _particlesToSpawn,
            xPosition = ParentCell.gridPosition.x,
            yPosition = ParentCell.gridPosition.y
        };
    }

    public override void FromSaveData(BoardObjectSaveData saveData)
    {
        if(saveData is not CircleSaveData data) return;

        currentValue = data.currentValue;
        _particlesToSpawn = data.particlesToSpawn;
        GridManager.GetClosestCell(new Vector2(data.xPosition, data.yPosition)).SetChildObject(this);
        float newValue = (float)(_currentStartValue - currentValue) / _currentStartValue;
        LerpRemovedSegments(newValue);
    }
}
