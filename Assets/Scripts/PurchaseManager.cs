public static class PurchaseManager
{
        private static long _currentCurrency;

        public static void Init()
        {
                _currentCurrency = 0;
                SystemEventManager.Subscribe(SystemEventManager.GameEvent.CircleComplete, OnCircleComplete);
        }

        private static void OnCircleComplete(object obj)
        {
                if (obj is int value)
                {
                        _currentCurrency += value;
                }
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
                _currentCurrency = 0;
                SystemEventManager.Unsubscribe(SystemEventManager.GameEvent.CircleComplete, OnCircleComplete);
        }

        public static long GetCurrentCurrency()
        {
                return _currentCurrency;
        }
}