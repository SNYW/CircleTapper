using Managers;
using UnityEngine;

namespace Gameplay
{
    [CreateAssetMenu(menuName = "Game Data/ AutoTap Upgrade Definition", fileName = "AutoTapUpgradeDefinition")]
    public class AutoTapSpeedUpgradeDefinition : UpgradeDefinition
    {
        public float speedPerLevel;

        public override void OnLevelUp()
        {   
            UpgradeManager.LevelUpUpgrade(this);
            PurchaseManager.TryPurchaseUpgrade(this);
        }

        public override bool CanPurchase()
        {
            return !IsMaxed() && PurchaseManager.CanPurchaseUpgrade(GetPurchasePrice());
        }

        public override bool IsMaxed()
        {
            if (UpgradeManager.TryGetUpgrade(upgradeName, out var upgrade))
            {
                return upgrade.currentLevel >= maxLevel;
            }

            return false;
        }

        public override int GetPurchasePrice()
        {
            return 5;
        }

        public override string GetLevelInfo()
        {
            var upgraded = UpgradeManager.TryGetUpgrade(upgradeName, out var upgrade);

            var value = upgraded ? upgrade.currentLevel : 0;
            return $"{value}/{maxLevel}";
        }
    }
}