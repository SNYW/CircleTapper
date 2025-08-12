using System;
using Persistence;
using UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Managers
{
    public static class ObjectiveManager
    {
        public static int CurrentObjective;
        public static bool AllObjectivesComplete => UpgradeManager.AllUpgradesComplete();
        public static void Init()
        {
            CurrentObjective = SaveManager.Instance.gameData.currentObjective;
        }

        public static void OnGameLoad()
        {
            CurrentObjective = Mathf.Max(1, SaveManager.Instance.gameData.currentObjective);
            Object.FindFirstObjectByType<ObjectiveTrackerPanel>().Init();
        }

        public static bool CanClaimObjective()
        {
            return PurchaseManager.GetCurrentCurrency() >= GetCurrentObjectiveCost();
        }
        
        public static void ClaimObjective()
        {
            if (AllObjectivesComplete || !CanClaimObjective() || !PurchaseManager.TryPurchaseItem(GetCurrentObjectiveCost(), false)) return;
            
            CurrentObjective++;
            PurchaseManager.AddUpgradePoints(1);
        }

        public static void ResetObjectives()
        {
            CurrentObjective = 1;
            SystemEventManager.Send(SystemEventManager.GameEvent.CurrencyAdded,0);
        }

        public static int GetCurrentObjectiveCost()
        {
            int multiplier = CurrentObjective switch
            {
                < 10 => 100,
                < 50 => 1000,
                _ => 2000
            };

            return CurrentObjective * multiplier;
        }
    }
}