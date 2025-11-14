using Managers;
using TMPro;
using UnityEngine;
using static SystemEventManager;

public class CurrencyCounter : MonoBehaviour
{
    public float lerpSpeed;
    public TMP_Text currencyText;
    public TMP_Text passiveText;

    private long _currentCurrency;
    private long _targetCurrency;
    private long _passiveIncomeAmount;
    
    private void Start()
    {
        UpdateCurrencyText(PurchaseManager.GetCurrentCurrency());
        Subscribe(GameEvent.CircleComplete, OnCircleComplete);
        Subscribe(GameEvent.CurrencySpent, OnCircleComplete);
        Subscribe(GameEvent.CurrencyAdded, OnCircleComplete);
    }

    private void OnCircleComplete(object obj)
    {
        _targetCurrency = PurchaseManager.GetCurrentCurrency();
    }

    private void Update()
    {
        if (_targetCurrency != _currentCurrency)
        {
            _currentCurrency = (long)Mathf.Lerp(_currentCurrency, _targetCurrency, lerpSpeed);
            UpdateCurrencyText(_currentCurrency);
        }

        _passiveIncomeAmount = PurchaseManager.GetPassiveIncomeAmount();
        passiveText.text = $"+{_passiveIncomeAmount}/s";
    }

    private void OnDisable()
    {
        Unsubscribe(GameEvent.CircleComplete, OnCircleComplete);
        Unsubscribe(GameEvent.CurrencySpent, OnCircleComplete);
        Unsubscribe(GameEvent.CurrencyAdded, OnCircleComplete);
    }

    private void UpdateCurrencyText(long c)
    {
        currencyText.text = c.ToString();
    }
}
