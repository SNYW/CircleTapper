using System.Collections.Generic;
using System.Linq;
using Gameplay;
using Persistence;
using UnityEngine;

namespace Managers
{
    public static class UpgradeManager
    {
        private static Dictionary<string, UpgradeDefinition> definitions;
        private static Dictionary<string, Upgrade> upgrades;
        
        public static void Init()
        {
            definitions = new Dictionary<string, UpgradeDefinition>();
            UpgradeDefinition[] list = Resources.LoadAll<UpgradeDefinition>("Data/Upgrades/");

            foreach (var upgradeDefinition in list)
            {
                definitions.Add(upgradeDefinition.upgradeName, upgradeDefinition);
            }

            upgrades = new Dictionary<string, Upgrade>();
        }
        
        public static bool TryGetUpgrade(string name, out Upgrade upgrade)
        {
            upgrade = null;
            if (!upgrades.TryGetValue(name, out var retrievedDef))
            {
                return false;
            }

            upgrade = retrievedDef;
            return true;
        }

        public static bool TryGetUpgradeDefinition(string name, out UpgradeDefinition def)
        {
            def = null;
            if (!definitions.TryGetValue(name, out var retrievedDef))
            {
                Debug.LogError($"Could not find upgrade definition with name {name}");
                return false;
            }

            def = retrievedDef;
            return true;
        }

        public static void LevelUpUpgrade(UpgradeDefinition definition)
        {
            if (upgrades.TryGetValue(definition.upgradeName, out var upgrade))
            {
                upgrade.currentLevel++;
                SaveManager.Instance.SaveUpgrade(upgrade);
            }
            else
            {
                var newUpgrade = new Upgrade
                {
                    upgradeDefinition = definition,
                    currentLevel = 1
                };
                
                upgrades.Add(definition.upgradeName, newUpgrade);
                SaveManager.Instance.SaveUpgrade(newUpgrade);
            }
        }

        public static List<UpgradeSaveObject> GetUpgradeSaveData()
        {
            var saveData = new List<UpgradeSaveObject>();

            foreach (var kvp in upgrades)
            {
                saveData.Add(kvp.Value.ToSaveObject());
            }

            return saveData;
        }

        public static void OnGameLoad(GameData gameData)
        {
            gameData.upgrades ??= new List<UpgradeSaveObject>();

            foreach (var upgradeSaveObject in gameData.upgrades)
            {
                if (!TryGetUpgradeDefinition(upgradeSaveObject.upgradeName, out var def)) continue;
                
                var newUpgrade = new Upgrade();
                newUpgrade.Init(upgradeSaveObject);
                upgrades.Add(upgradeSaveObject.upgradeName, newUpgrade);
            }
        }

        public static void ResetUpgrades()
        {
            upgrades = new Dictionary<string, Upgrade>();
        }

        public static bool CanPurchaseAnyUpgrades()
        {
            return definitions.Any(d => d.Value.CanPurchase());
        }
    }
}