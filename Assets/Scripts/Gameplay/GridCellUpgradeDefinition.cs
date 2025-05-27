using Gameplay;
using Persistence;
using UnityEngine;

namespace Managers
{
    [CreateAssetMenu(menuName = "Game Data/ Grid Upgrade Definition", fileName = "GridUpgradeDefinition")]
    public class GridCellUpgradeDefinition : UpgradeDefinition
    {
        public int unitAmount;
        public int costPerUnit;

        public override void OnLevelUp()
        {
            var cellToUnlock = GridManager.GetClosestCell(Vector2.zero, false, true);

            if (cellToUnlock == null) return;

            SaveManager.Instance.UnlockCell(cellToUnlock);
            UpgradeManager.LevelUpUpgrade(this);
            PurchaseManager.TryPurchaseUpgrade(this);
            EffectsManager.Instance.SpawnEffect(EffectsManager.EffectType.Spawn, cellToUnlock.transform.position);
        }

        public override bool CanPurchase()
        {
            var hasCell = GridManager.GetClosestCell(Vector2.zero, false, true) != null;
            return hasCell && PurchaseManager.CanPurchaseUpgrade(GetPurchasePrice());
        }

        public override int GetPurchasePrice()
        {
            return Mathf.Clamp(SaveManager.Instance.gameData.unlockedCells.Count / unitAmount * costPerUnit, 1, 5);
        }

        public override string GetLevelInfo()
        {
            var upgraded = UpgradeManager.TryGetUpgrade(upgradeName, out var upgrade);

            var value = upgraded ? upgrade.currentLevel : 0;
            return $"{value}/{GridManager.GetCellCount()-1}";
        }

        public override bool IsMaxed()
        {
            var upgraded = UpgradeManager.TryGetUpgrade(upgradeName, out var upgrade);

            var value = upgraded ? upgrade.currentLevel : 0;
            return value >= GridManager.GetCellCount()-1;
        }
    }
}