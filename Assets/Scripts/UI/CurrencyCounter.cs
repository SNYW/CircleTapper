using System;
using Core;
using Economy;
using TMPro;
using UnityEngine;
using static SystemEventManager;

/// <summary>
/// The currency readout, and the "+n/s" passive income line beneath it.
/// <para>
/// Both used to be recomputed every frame. The passive figure is derived from what is on the
/// board, so it only changes when the board does; and assigning <c>TMP_Text.text</c> rebuilds the
/// text mesh whether or not the string differs, which made the per-frame assignment costlier than
/// the arithmetic behind it.
/// </para>
/// </summary>
public class CurrencyCounter : MonoBehaviour
{
    public float lerpSpeed;
    public TMP_Text currencyText;
    public TMP_Text passiveText;

    private CurrencyService _currency;

    private long _displayed;
    private long _target;
    private int _passiveIncome = -1;

    private void Start()
    {
        _currency = ServiceLocator.Get<CurrencyService>();

        _displayed = _currency.Points;
        _target = _displayed;
        SetCurrencyText(_displayed);
        RefreshPassiveIncome();

        Subscribe(GameEvent.CurrencySpent, OnCurrencyChanged);
        Subscribe(GameEvent.CurrencyAdded, OnCurrencyChanged);
        Subscribe(GameEvent.BoardChanged, OnBoardChanged);
        Subscribe(GameEvent.GameLoaded, OnBoardChanged);
    }

    private void OnDestroy()
    {
        Unsubscribe(GameEvent.CurrencySpent, OnCurrencyChanged);
        Unsubscribe(GameEvent.CurrencyAdded, OnCurrencyChanged);
        Unsubscribe(GameEvent.BoardChanged, OnBoardChanged);
        Unsubscribe(GameEvent.GameLoaded, OnBoardChanged);
    }

    private void OnCurrencyChanged(object payload) => _target = _currency.Points;

    private void OnBoardChanged(object payload) => RefreshPassiveIncome();

    /// <summary>
    /// Only runs while the displayed number is still catching up to the real one. Idle frames
    /// cost a single comparison.
    /// </summary>
    private void Update()
    {
        if (_displayed == _target) return;

        long delta = _target - _displayed;
        long step = (long)Math.Round(delta * (double)lerpSpeed);

        // Rounding to zero on a small delta would stall the count short of the target forever.
        if (step == 0) step = delta > 0 ? 1 : -1;

        _displayed += step;
        SetCurrencyText(_displayed);
    }

    private void RefreshPassiveIncome()
    {
        int income = GridManager.GetPassiveIncomeAmount();
        if (income == _passiveIncome) return;

        _passiveIncome = income;
        passiveText.text = $"+{income}/s";
    }

    private void SetCurrencyText(long value) => currencyText.text = value.ToString();
}
