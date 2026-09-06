using Core;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Persistence;
using TMPro;
using UnityEngine;

public class Hex : BoardObject
{
    public int clickSpeed;
    public List<ParticleSystem> clickParticles;
    public SpriteRenderer spriteRenderer;
    public TMP_Text neighbourValueText;

    [Header("Particle Spawn")]
    public CurrencyParticle particle;

    /// <summary>How often banked currency is paid out as particles.</summary>
    private const float SpawnInterval = 0.01f;

    private readonly CurrencyEmitter _emitter = new();
    private SegmentProgress _progress;

    private int _storedParticles;
    private int _remainingCooldown;

    public List<GridManager.Direction> tapTargets;
    public FMODUnity.EventReference HexCompleteSFX;

    private Vector3 targetScale = Vector3.one;
    private float scaleSpeed = 10f;

    private void Awake()
    {
        SystemEventManager.Subscribe(SystemEventManager.GameEvent.CircleComplete, OnCircleComplete);
    }

    private void OnCircleComplete(object obj)
    {
        if (obj is not Circle c || parentCell == null) return;

        foreach (var dir in tapTargets)
        {
            if (!parentCell.Neighbors.TryGetValue(dir, out var cell)) continue;
            if (cell != c.parentCell) continue;

            _storedParticles += c.GetPointValue()*2;
            neighbourValueText.text = _storedParticles.ToString();

            _remainingCooldown--;
            _progress.Target = CooldownFraction;
            SaveObjectState();

            if (_remainingCooldown > 0 || parentCell == null) return;

            if (_storedParticles > 0)
            {
                _emitter.Add(_storedParticles);
                _storedParticles = 0;
                neighbourValueText.text = "0";
                FMODUnity.RuntimeManager.PlayOneShotAttached(HexCompleteSFX, gameObject);

                foreach (var clickParticle in clickParticles)
                    clickParticle.Play();
            }

            targetScale = Vector3.one * 1.2f;
            _remainingCooldown = clickSpeed;
            _progress.Target = 1f;
        }
    }

    public override void Init()
    {
        Init(clickSpeed);
    }

    private void Init(int remainingCooldown)
    {
        _remainingCooldown = remainingCooldown;
        _progress = new SegmentProgress(spriteRenderer, clickSpeed, CooldownFraction);

        if (parentCell == null)
            GridManager.GetClosestCell(transform.position).SetChildObject(this);

        neighbourValueText.text = _storedParticles.ToString();

        EmitCurrencyLoop(RestartLoops()).Forget();
    }

    public override void BeginDrag(Vector2 startPos)
    {
        // Payout pauses while the hex is in the air, as stopping the coroutine used to.
        StopLoops();
        base.BeginDrag(startPos);
    }

    public override void EndDrag(Vector2 eventData)
    {
        EmitCurrencyLoop(RestartLoops()).Forget();
        base.EndDrag(eventData);
    }

    /// <summary>Proportion of the cooldown still to run.</summary>
    private float CooldownFraction => (float)_remainingCooldown / clickSpeed;

    private void Update()
    {
        _progress.Advance(Time.deltaTime);

        // Scale animation
        if (transform.localScale != targetScale)
        {
            transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, scaleSpeed * Time.deltaTime);

            if (transform.localScale == targetScale && targetScale != Vector3.one)
                targetScale = Vector3.one;
        }
    }

    private async UniTaskVoid EmitCurrencyLoop(CancellationToken token)
    {
        TimeSpan interval = TimeSpan.FromSeconds(SpawnInterval);

        try
        {
            while (true)
            {
                await UniTask.Delay(interval, cancellationToken: token);

                if (_emitter.TryEmit(transform.position)) SaveObjectState();
            }
        }
        catch (OperationCanceledException)
        {
            // Picked up, disabled or destroyed.
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
        _remainingCooldown = saveData.value;
        _storedParticles = saveData.carryoverValue;
        GridManager.GetGridCell(new Vector2Int(saveData.xPosition, saveData.yPosition)).SetChildObject(this);
        Init(saveData.value);
        SaveObjectState();
    }

    public override string GetValue() => _remainingCooldown.ToString();

    public override string GetMaterialValue() => _progress.ToDebugString();

    private void OnDisable()
    {
        SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.CircleComplete, OnCircleComplete);
    }
}