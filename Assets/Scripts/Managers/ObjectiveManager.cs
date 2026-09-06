using Economy;
using Core;
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
        public static bool AllObjectivesComplete => ServiceLocator.Get<UpgradeCatalog>().AllComplete();
        public static void OnGameLoad()
        {
            CurrentObjective = Mathf.Max(1, ServiceLocator.Get<SaveService>().Data.currentObjective);
            Object.FindFirstObjectByType<ObjectiveTrackerPanel>().Init();
        }

        public static bool CanClaimObjective()
        {
            return ServiceLocator.Get<CurrencyService>().Points >= GetCurrentObjectiveCost();
        }
        
        public static void ClaimObjective()
        {
            CurrencyService currency = ServiceLocator.Get<CurrencyService>();
            if (AllObjectivesComplete || !CanClaimObjective() || !currency.TrySpend(GetCurrentObjectiveCost())) return;

            CurrentObjective++;
            currency.AddUpgradePoints(1);
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
                < 20 => 10,
                < 30 => 50,
                _ => 100
            };

            return CurrentObjective * multiplier;
        }
    }
}