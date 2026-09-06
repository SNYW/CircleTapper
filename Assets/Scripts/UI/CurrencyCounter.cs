using Economy;
using Core;
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
    private CurrencyService _currency;
    
    private void Start()
    {
        _currency = ServiceLocator.Get<CurrencyService>();
        UpdateCurrencyText(_currency.Points);
        Subscribe(GameEvent.CurrencySpent, OnCurrencyChanged);
        Subscribe(GameEvent.CurrencyAdded, OnCurrencyChanged);
    }

    private void OnCurrencyChanged(object obj)
    {
        _targetCurrency = _currency.Points;
    }

    private void Update()
    {
        if (_targetCurrency != _currentCurrency)
        {
            _currentCurrency = (long)Mathf.Lerp(_currentCurrency, _targetCurrency, lerpSpeed);
            UpdateCurrencyText(_currentCurrency);
        }

        _passiveIncomeAmount = GridManager.GetPassiveIncomeAmount();
        passiveText.text = $"+{_passiveIncomeAmount}/s";
    }

    private void OnDisable()
    {
        Unsubscribe(GameEvent.CurrencySpent, OnCurrencyChanged);
        Unsubscribe(GameEvent.CurrencyAdded, OnCurrencyChanged);
    }

    private void UpdateCurrencyText(long c)
    {
        currencyText.text = c.ToString();
    }
}
