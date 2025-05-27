using System.Linq;
using Gameplay;
using Persistence;
using UnityEngine;

namespace Managers
{
    public static class PurchaseManager
    {
        private static long _currentCurrency;
        private static long _currentUpgradePoints;

        public static void Init()
        {
       
        }

        public static void AddCurrency(long value)
        {
            _currentCurrency += value;
            SystemEventManager.Send(SystemEventManager.GameEvent.CurrencyAdded, _currentCurrency);
        }
    
        public static void AddUpgradePoints(long value)
        {
            _currentUpgradePoints += value;
            SystemEventManager.Send(SystemEventManager.GameEvent.UpgradePointAdded, _currentUpgradePoints);
        }

        public static int GetPassiveIncomeAmount()
        {
            return GridManager.GetAllBoardItems().Sum(bo => bo.chainLevel+1);
        }

        public static void OnGameLoad(GameData data)
        {
            _currentCurrency = 0;
            _currentUpgradePoints = 0;
            AddCurrency(data.currentPoints);
            AddUpgradePoints(data.currentUpgradePoints);
        }

        public static void ResetCurrency()
        {
            _currentCurrency = 0;
            _currentUpgradePoints = 0;
        }

        public static bool CanPurchaseItem(int cost)
        {
            return cost <= _currentCurrency && GridManager.GetClosestCell(Vector2.zero) != null;
        }

        public static bool CanPurchaseUpgrade(int cost)
        {
            return cost <= _currentUpgradePoints;
        }

        public static bool TryPurchaseItem(int cost)
        {
            if (!CanPurchaseItem(cost)) return false;

            _currentCurrency -= cost;
            SystemEventManager.Send(SystemEventManager.GameEvent.CurrencySpent, cost);
            return true;
        }
        
        public static void TryPurchaseUpgrade(UpgradeDefinition definition)
        {
            _currentUpgradePoints -= definition.GetPurchasePrice();
            SystemEventManager.Send(SystemEventManager.GameEvent.UpgradePointSpent, _currentUpgradePoints);
        }

        public static long GetCurrentCurrency()
        {
            return _currentCurrency;
        }

        public static long GetCurrentUpgradePoints()
        {
            return _currentUpgradePoints;
        }
    }
}