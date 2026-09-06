using Core;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Economy;
using Managers;
using Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SystemEventManager;

/// <summary>
/// Buys a board object. Everything here used to be recomputed every frame — a scene-wide
/// <c>FindObjectsByType</c> in LateUpdate and a full grid scan in Update, per button. Both answers
/// only change when the board does, so both are event-driven now.
/// </summary>
public class BuyButton : MonoBehaviour
{
    public int cost;
    public BoardObject objectToBuy;
    public TMP_Text costText;

    public FMODUnity.EventReference BuyButtonSFX;

    private int _currentCost;
    private bool _hasFreeCell;
    private Button _button;
    private bool _refreshQueued;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _currentCost = cost;
        costText.text = _currentCost.ToString();

        // Expensive, and only changes when the board or the grid does.
        Subscribe(GameEvent.BoardChanged, OnBoardChanged);
        Subscribe(GameEvent.GridCellUnlocked, OnBoardChanged);
        Subscribe(GameEvent.GameLoaded, OnBoardChanged);

        // Cheap, but changes constantly — this is what makes the button light up when the player
        // can finally afford it, which the board events alone would never catch.
        Subscribe(GameEvent.CurrencyAdded, OnCurrencyChanged);
        Subscribe(GameEvent.CurrencySpent, OnCurrencyChanged);
    }

    private void Start() => ScheduleRefresh();

    private void OnDestroy()
    {
        Unsubscribe(GameEvent.BoardChanged, OnBoardChanged);
        Unsubscribe(GameEvent.GridCellUnlocked, OnBoardChanged);
        Unsubscribe(GameEvent.GameLoaded, OnBoardChanged);
        Unsubscribe(GameEvent.CurrencyAdded, OnCurrencyChanged);
        Unsubscribe(GameEvent.CurrencySpent, OnCurrencyChanged);
    }

    private void OnBoardChanged(object payload) => ScheduleRefresh();

    private void OnCurrencyChanged(object payload) => RefreshInteractable();

    /// <summary>
    /// Defers the expensive refresh by a frame, and coalesces a burst of board events into one.
    /// <para>
    /// The frame's delay matters: <c>BoardChanged</c> is sent from <c>OnDestroy</c>, and a
    /// destroyed object is still counted by <c>FindObjectsByType</c> at that moment. Counting
    /// immediately would leave the price permanently one object too high.
    /// </para>
    /// </summary>
    private void ScheduleRefresh()
    {
        if (_refreshQueued) return;

        _refreshQueued = true;
        RefreshNextFrame(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid RefreshNextFrame(CancellationToken token)
    {
        try
        {
            await UniTask.NextFrame(token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        _refreshQueued = false;
        Refresh();
    }

    private void Refresh()
    {
        _currentCost = cost * CountBoardObjects();
        _hasFreeCell = GridManager.GetClosestCell(Vector2.zero) != null;

        costText.text = _currentCost.ToString();
        RefreshInteractable();
    }

    private void RefreshInteractable()
    {
        bool canBuy = GameManager.DEBUGMODE
                      || (_hasFreeCell && ServiceLocator.Get<CurrencyService>().CanAfford(_currentCost));

        if (_button.interactable != canBuy) _button.interactable = canBuy;
    }

    private static int CountBoardObjects()
        => ServiceLocator.Get<SaveService>().BoardObjects.Count;

    public void OnMouseDown()
    {
        GridCell cell = GridManager.GetClosestCell(Vector2.zero);
        if (cell == null) return;

        if (!GameManager.DEBUGMODE && !ServiceLocator.Get<CurrencyService>().TrySpend(_currentCost)) return;

        BoardObject bought = Instantiate(objectToBuy);
        cell.SetChildObject(bought);
        bought.Init();

        Send(GameEvent.BoardChanged, bought);
        EffectsManager.Instance.SpawnEffect(EffectsManager.EffectType.Spawn, bought.transform.position);
        FMODUnity.RuntimeManager.PlayOneShotAttached(BuyButtonSFX, gameObject);
    }
}
