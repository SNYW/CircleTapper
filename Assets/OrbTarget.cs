using UnityEngine;

public class OrbTarget : MonoBehaviour
{
    private Vector3 originalScale;
    private Vector3 targetScale;
    private float scaleSpeed = 10f;
    private bool isAnimating = false;

    private void OnEnable()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        SystemEventManager.Subscribe(SystemEventManager.GameEvent.CurrencyAdded, OnCurrencyAdded);
    }

    private void OnCurrencyAdded(object obj)
    {
        targetScale = originalScale * 1.4f;
        isAnimating = true;
    }

    private void Update()
    {
        if (!isAnimating) return;

        transform.localScale = Vector3.MoveTowards(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);

        if (transform.localScale == targetScale)
        {
            if (targetScale != originalScale)
            {
                targetScale = originalScale;
            }
            else
            {
                isAnimating = false;
            }
        }
    }

    private void OnDisable()
    {
        SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.CurrencyAdded, OnCurrencyAdded);
    }
}