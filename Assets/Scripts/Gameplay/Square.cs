using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Persistence;
using Unity.VisualScripting;
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
    private float currentRemovedSegments;
    private float targetRemovedSegments;
    private float lerpSpeed = 10f;

    public List<GridManager.Direction> tapTargets;

    public FMODUnity.EventReference SquareCompleteSFX;

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
        currentRemovedSegments = (float)_remainingCooldown / clickSpeed;
        targetRemovedSegments = currentRemovedSegments;
        _propertyBlock.SetFloat(RemovedSegments, currentRemovedSegments);
        spriteRenderer.SetPropertyBlock(_propertyBlock);

        if (parentCell == null)
            GridManager.GetClosestCell(transform.position).SetChildObject(this);

        StartCoroutine(ClickNeighbours());
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
            yield return new WaitForSeconds(1f);

            _remainingCooldown--;
            targetRemovedSegments = Mathf.Clamp01((float)_remainingCooldown / clickSpeed);
            SaveObjectState();

            if (_remainingCooldown > 0) continue;
            if (parentCell == null) continue;

            foreach (var direction in tapTargets)
            {
                if (!parentCell.Neighbors.TryGetValue(direction, out var neighbor)) continue;
                if (neighbor.heldObject is Circle circle)
                {
                    circle.OnTap();
                    FMODUnity.RuntimeManager.PlayOneShotAttached(SquareCompleteSFX, gameObject);
                }
                else
                {
                    FMODUnity.RuntimeManager.PlayOneShotAttached(SquareCompleteSFX, gameObject);
                }
            }

            foreach (var clickParticle in clickParticles)
            {
                clickParticle.Play();
            }

            transform.localScale = Vector3.one * 1.2f;
            yield return new WaitForSeconds(0.1f);
            transform.localScale = Vector3.one;

            _remainingCooldown = clickSpeed;
            targetRemovedSegments = 1f;
        }
    }

    private void Update()
    {
        if (Mathf.Approximately(currentRemovedSegments, targetRemovedSegments)) return;

        currentRemovedSegments = Mathf.MoveTowards(currentRemovedSegments, targetRemovedSegments, Time.deltaTime * lerpSpeed);

        _propertyBlock.SetFloat(RemovedSegments, currentRemovedSegments);
        spriteRenderer.SetPropertyBlock(_propertyBlock);
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
        StopAllCoroutines();

        _remainingCooldown = saveData.value;
        GridManager.GetGridCell(new Vector2Int(saveData.xPosition, saveData.yPosition)).SetChildObject(this);
        Init(saveData.value);
    }

    protected override void SaveObjectState()
    {
        if (parentCell != null)
            SaveManager.Instance.AddObject(parentCell.gridPosition, ToSaveData());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
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