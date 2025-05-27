using Managers;

namespace Gameplay
{
    public class UpgradeSaveObject
    {
        public string upgradeName;
        public int currentLevel;
    }
    
    public class Upgrade
    {
        public UpgradeDefinition upgradeDefinition;
        public int currentLevel;

        public int GetPurchasePrice()
        {
            return upgradeDefinition.GetPurchasePrice();
        }

        public void Init(UpgradeSaveObject save)
        {
            if (!UpgradeManager.TryGetUpgradeDefinition(save.upgradeName, out var def)) return;

            upgradeDefinition = def;
            currentLevel = save.currentLevel;
        }

        public UpgradeSaveObject ToSaveObject()
        {
            return new UpgradeSaveObject
            {
                upgradeName = upgradeDefinition.upgradeName,
                currentLevel = currentLevel
            };
        }
    }
    
}