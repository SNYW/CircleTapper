using Progression;
using Economy;
using Core;
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
            if (!ServiceLocator.Get<CurrencyService>().TrySpendUpgradePoints(GetPurchasePrice())) return;
            
            ServiceLocator.Get<UpgradeService>().LevelUp(upgradeName);
            FMODUnity.RuntimeManager.PlayOneShot("event:/UI_Button_UpgradeAutoTap");
        }

        public override bool CanPurchase()
        {
            return !IsMaxed() && ServiceLocator.Get<CurrencyService>().CanAffordUpgrade(GetPurchasePrice());
        }

        public override int GetPurchasePrice()
        {
            return 2;
        }

        public override string GetLevelInfo()
        {
            return $"{ServiceLocator.Get<UpgradeService>().GetLevel(upgradeName)}/{maxLevel}";
        }

        public override bool IsMaxed()
        {
            return ServiceLocator.Get<UpgradeService>().GetLevel(upgradeName) >= maxLevel;
        }
    }
}