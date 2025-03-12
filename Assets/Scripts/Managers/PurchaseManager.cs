using Persistence;

public static class PurchaseManager
{
    private static long _currentCurrency;

    public static void Init()
    {
        SystemEventManager.Subscribe(SystemEventManager.GameEvent.CurrencyAdded, OnCurrencyAdded);
    }

    public static void OnGameLoad(GameData data)
    {
        _currentCurrency = data.currentPoints;
        SystemEventManager.Send(SystemEventManager.GameEvent.CurrencyAdded, data.currentPoints);
    }

    private static void OnCurrencyAdded(object obj)
    {
        if (obj is int value)
        {
            _currentCurrency += value;
        }
    }

    public static void ResetCurrency()
    {
        _currentCurrency = 0;
    }

    public static bool CanPurchase(int cost)
    {
        return cost <= _currentCurrency;
    }

    public static bool TryPurchaseItem(int cost)
    {
        if (!CanPurchase(cost)) return false;

        _currentCurrency -= cost;
        SystemEventManager.Send(SystemEventManager.GameEvent.CurrencySpent, cost);
        return true;
    }

    public static void Dispose()
    {
        SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.CurrencyAdded, OnCurrencyAdded);
    }

    public static long GetCurrentCurrency()
    {
        return _currentCurrency;
    }
}