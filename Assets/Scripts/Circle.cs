using TMPro;
using UnityEngine;
using DG.Tweening;
using static SystemEventManager.GameEvent;

public class Circle : BoardObject
{
    public TMP_Text numberDisplay;
    public int startValue;
    public int maxValue;
    public int currentValue;
    public SpriteRenderer spriteRenderer;

    private int _currentStartValue;
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
    }

    public override void OnTap()
    {
        currentValue--;
        var isComplete = currentValue <= 0;
        if (isComplete)
        {
            Complete();
        }

        numberDisplay.SetText(currentValue.ToString());

        float newValue = (float)(_currentStartValue - currentValue) / _currentStartValue;
        LerpRemovedSegments(newValue);
        
        transform.DOScale(isComplete ? 0.65f : 0.52f, 0.1f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => transform.DOScale(0.5f, 0.1f)
                .SetEase(Ease.InQuad));
    }

    private void Complete()
    {
        SystemEventManager.Send(CircleComplete, _currentStartValue);
        _currentStartValue = Mathf.Clamp(_currentStartValue * 2, 0, maxValue);
        currentValue = _currentStartValue;

        _propertyBlock.SetFloat(SegmentCount, _currentStartValue);
        spriteRenderer.SetPropertyBlock(_propertyBlock);
        LerpRemovedSegments(0f);
    }

    private void LerpRemovedSegments(float newValue)
    {
        spriteRenderer.GetPropertyBlock(_propertyBlock);
        
        if (_propertyBlock.HasProperty(RemovedSegments))
        {
            float currentRemovedSegments = _propertyBlock.GetFloat(RemovedSegments);

            DOTween.To(() => currentRemovedSegments, value =>
            {
                _propertyBlock.SetFloat(RemovedSegments, value);
                spriteRenderer.SetPropertyBlock(_propertyBlock);
            }, newValue, 0.5f);
        }
        else
        {
            Debug.LogWarning("MaterialPropertyBlock does not have a '_RemovedSegments' property.");
        }
    }

    public Vector2 GetPosition()
    {
        return transform.position;
    }
}
