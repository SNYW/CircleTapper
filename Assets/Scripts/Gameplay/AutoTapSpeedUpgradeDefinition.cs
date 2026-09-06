using Progression;
using Economy;
using Core;
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
            if (ServiceLocator.Get<CurrencyService>().TrySpendUpgradePoints(GetPurchasePrice()))
            {
                ServiceLocator.Get<UpgradeService>().LevelUp(upgradeName);
                FMODUnity.RuntimeManager.PlayOneShot("event:/UI_Button_UpgradeAutoTap"); //audio
            }
        }

        public override bool CanPurchase()
        {
            return !IsMaxed() && ServiceLocator.Get<CurrencyService>().CanAffordUpgrade(GetPurchasePrice());
        }

        public override bool IsMaxed()
        {
            return ServiceLocator.Get<UpgradeService>().GetLevel(upgradeName) >= maxLevel;
        }

        public override int GetPurchasePrice()
        {
            return 5;
        }

        public override string GetLevelInfo()
        {
            return $"{ServiceLocator.Get<UpgradeService>().GetLevel(upgradeName)}/{maxLevel}";
        }
    }
}