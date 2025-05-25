using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Persistence;
using TMPro;
using UnityEngine;

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

    private float currentRemovedSegments;
    private float targetRemovedSegments;
    private float lerpSpeed = 10f;

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

            _storedParticles += c.startValue;
            neighbourValueText.text = _storedParticles.ToString();

            _remainingCooldown--;
            targetRemovedSegments = (float)_remainingCooldown / clickSpeed;
            SaveObjectState();

            if (_remainingCooldown > 0 || parentCell == null) return;

            if (_storedParticles > 0)
            {
                _particlesToSpawn += _storedParticles;
                _storedParticles = 0;
                neighbourValueText.text = "0";
                FMODUnity.RuntimeManager.PlayOneShotAttached(HexCompleteSFX, gameObject);

                foreach (var clickParticle in clickParticles)
                    clickParticle.Play();
            }

            targetScale = Vector3.one * 1.2f;
            _remainingCooldown = clickSpeed;
            targetRemovedSegments = 1f;
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
        currentRemovedSegments = (float)_remainingCooldown / clickSpeed;
        targetRemovedSegments = currentRemovedSegments;
        _propertyBlock.SetFloat(RemovedSegments, currentRemovedSegments);
        spriteRenderer.SetPropertyBlock(_propertyBlock);

        if (parentCell == null)
            GridManager.GetClosestCell(transform.position).SetChildObject(this);

        StartCoroutine(SpawnCurrency());
        neighbourValueText.text = _storedParticles.ToString();
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

    private void Update()
    {
        // Shader value lerp
        if (!Mathf.Approximately(currentRemovedSegments, targetRemovedSegments))
        {
            currentRemovedSegments = Mathf.MoveTowards(currentRemovedSegments, targetRemovedSegments, Time.deltaTime * lerpSpeed);
            _propertyBlock.SetFloat(RemovedSegments, currentRemovedSegments);
            spriteRenderer.SetPropertyBlock(_propertyBlock);
        }

        // Scale animation
        if (transform.localScale != targetScale)
        {
            transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, scaleSpeed * Time.deltaTime);

            if (transform.localScale == targetScale && targetScale != Vector3.one)
                targetScale = Vector3.one;
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
            if (parentCell != null)
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
        StopAllCoroutines();
        _remainingCooldown = saveData.value;
        _storedParticles = saveData.carryoverValue;
        GridManager.GetGridCell(new Vector2Int(saveData.xPosition, saveData.yPosition)).SetChildObject(this);
        Init(saveData.value);
        SaveObjectState();
    }

    protected override void SaveObjectState()
    {
        if (parentCell != null)
            SaveManager.Instance.AddObject(parentCell.gridPosition, ToSaveData());
    }

    public override string GetValue() => _remainingCooldown.ToString();

    public override string GetMaterialValue() => _propertyBlock.GetFloat(RemovedSegments).ToString(CultureInfo.InvariantCulture);
}