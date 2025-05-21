using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Persistence;
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
    private float LerpValue => (float)_remainingCooldown / clickSpeed;

    public List<GridManager.Direction> tapTargets;

    public FMODUnity.EventReference SquareCompleteSFX; //audio 

    public override void Init()
    {
        Init(clickSpeed);
    }

    public void Init(int remainingCooldown)
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
        StartCoroutine(ClickNeighbours());
        base.EndDrag(eventData);
    }

    private IEnumerator ClickNeighbours()
    {
        while (gameObject.activeSelf)
        {
            yield return new WaitForSeconds(1);

            _remainingCooldown--;
            LerpRemovedSegments((float)_remainingCooldown/clickSpeed);
            SaveObjectState();

            if (_remainingCooldown > 0) continue;
            if(parentCell == null) continue;
            
            foreach (var direction in tapTargets)
            {
                if (!parentCell.Neighbors.TryGetValue(direction, out var neighbor)) continue;
                if (neighbor.heldObject is Circle circle)
                {
                    circle.OnTap();
                    FMODUnity.RuntimeManager.PlayOneShotAttached(SquareCompleteSFX, gameObject); //audio
                }
                else
                {
                    FMODUnity.RuntimeManager.PlayOneShotAttached(SquareCompleteSFX, gameObject); //audio
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
        DOTween.KillAll(gameObject);
        StopAllCoroutines();
        
        _remainingCooldown = saveData.value;
        GridManager.GetGridCell(new Vector2Int(saveData.xPosition, saveData.yPosition)).SetChildObject(this);
        Init(saveData.value);
    }
    
    protected override void SaveObjectState()
    {
        if(parentCell != null)
            SaveManager.Instance.AddObject(parentCell.gridPosition, ToSaveData());
    }
}
