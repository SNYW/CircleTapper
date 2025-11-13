using Managers;
using UnityEngine;

namespace Gameplay
{
    [CreateAssetMenu(menuName = "Game Data/ CircleBonus Upgrade Definition", fileName = "CircleBonusUpgradeDefinition")]
    public class CircleCompletionBonusUpgradeDefinition : UpgradeDefinition
    {
        public int bonusPerLevel;
        
        public override void OnLevelUp()
        {
            if (!PurchaseManager.TryPurchaseUpgrade(this)) return;
            
            UpgradeManager.LevelUpUpgrade(this);
            FMODUnity.RuntimeManager.PlayOneShot("event:/UI_Button_UpgradeAutoTap");
        }

        public override bool CanPurchase()
        {
            return !IsMaxed() && PurchaseManager.CanPurchaseUpgrade(GetPurchasePrice());
        }

        public override int GetPurchasePrice()
        {
            return 2;
        }

        public override string GetLevelInfo()
        {
            var upgraded = UpgradeManager.TryGetUpgrade(upgradeName, out var upgrade);

            var value = upgraded ? upgrade.currentLevel : 0;
            return $"{value}/{maxLevel}";
        }

        public override bool IsMaxed()
        {
            if (UpgradeManager.TryGetUpgrade(upgradeName, out var upgrade))
            {
                return upgrade.currentLevel >= maxLevel;
            }

            return false;
        }
    }
}