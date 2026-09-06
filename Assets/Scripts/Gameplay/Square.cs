using Core;
using System.Collections;
using System.Collections.Generic;
using Persistence;
using UnityEngine;

public class Square : BoardObject
{
    public int clickSpeed;
    public List<ParticleSystem> clickParticles;
    public SpriteRenderer spriteRenderer;

    private int _remainingCooldown;
    private SegmentProgress _progress;

    public List<GridManager.Direction> tapTargets;

    public FMODUnity.EventReference SquareCompleteSFX;

    public override void Init()
    {
        Init(clickSpeed);
    }

    public void Init(int remainingCooldown)
    {
        _remainingCooldown = remainingCooldown;
        _progress = new SegmentProgress(spriteRenderer, clickSpeed, CooldownFraction);
        
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
            _progress.Target = Mathf.Clamp01(CooldownFraction);
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
            _progress.Target = 1f;
        }
    }

    /// <summary>Proportion of the cooldown still to run.</summary>
    private float CooldownFraction => (float)_remainingCooldown / clickSpeed;

    private void Update() => _progress.Advance(Time.deltaTime);

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

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public override string GetValue()
    {
        return _remainingCooldown.ToString();
    }

    public override string GetMaterialValue() => _progress.ToDebugString();
}