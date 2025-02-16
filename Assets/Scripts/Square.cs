using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Square : BoardObject
{
    public int clickSpeed;
    public List<ParticleSystem> clickParticles;
    public SpriteRenderer spriteRenderer;

    private MaterialPropertyBlock _propertyBlock;
    private static readonly int RemovedSegments = Shader.PropertyToID("_RemovedSegments");
    private static readonly int SegmentCount = Shader.PropertyToID("_SegmentCount");

    private int _remainingCooldown;

    public List<GridManager.Direction> tapTargets;

    private void Start()
    {
        _propertyBlock = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(_propertyBlock);
        
        _propertyBlock.SetFloat(SegmentCount, clickSpeed);
        _propertyBlock.SetFloat(RemovedSegments, 0f);
        
        spriteRenderer.SetPropertyBlock(_propertyBlock);
        
        if(ParentCell == null)
            GridManager.GetClosestCell(transform.position).SetChildObject(this);
        StartCoroutine(ClickNeighbours());

        _remainingCooldown = clickSpeed;
        LerpRemovedSegments(1);
    }

    public override void BeginDrag(Vector2 startPos)
    {
        StopAllCoroutines();
        SaveManager.RemoveSaveData(ParentCell.gridPosition);
        base.BeginDrag(startPos);
    }

    public override void EndDrag(Vector2 eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ClickNeighbours());
        SaveManager.SaveBoardObject(ToSaveData());
        base.EndDrag(eventData);
    }

    private IEnumerator ClickNeighbours()
    {
        while (gameObject.activeSelf)
        {
            yield return new WaitForSeconds(1);

            _remainingCooldown--;
            LerpRemovedSegments((float)_remainingCooldown/clickSpeed);

            if (_remainingCooldown <= 0)
            {
                if(ParentCell == null) continue;
            
                foreach (var direction in tapTargets)
                {
                    if (ParentCell.Neighbors[direction].heldObject is Circle circle)
                    {
                        circle.OnTap();
                    }
                }

                foreach (var clickParticle in clickParticles)
                {
                    clickParticle.Play();
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
            SaveManager.SaveBoardObject(ToSaveData());
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
    
    public override BoardObjectSaveData ToSaveData()
    {
        return new SquareSaveData()
        {
            remainingCooldown = _remainingCooldown,
            level = chainLevel,
            position = ParentCell.gridPosition
        };
    }

    public override void FromSaveData(BoardObjectSaveData saveData)
    {
        if(saveData is not SquareSaveData data) return;

        _remainingCooldown = data.remainingCooldown;
        GridManager.GetClosestCell(data.position).SetChildObject(this);
        LerpRemovedSegments((float)_remainingCooldown/clickSpeed);
    }
}
