using System;
using System.Collections.Generic;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using Persistence;

namespace Progression
{
    /// <summary>
    /// Which upgrades the player owns, and at what level.
    /// <para>
    /// Deliberately stores nothing but a name and a number. The ScriptableObjects that describe
    /// what an upgrade *does* — cost, cap, effect per level — are assets, and belong to a
    /// catalogue in the game assembly. Keeping them out of here is what makes the player's
    /// progression testable without loading a project.
    /// </para>
    /// </summary>
    public class UpgradeService : IGameService, IAsyncInitializable, IServiceDisposable
    {
        private readonly SaveService _save;
        private readonly Dictionary<string, int> _levels = new();

        public UpgradeService(SaveService save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
        }

        public IReadOnlyDictionary<string, int> Levels => _levels;

        /// <summary>Fires as (upgradeName, newLevel).</summary>
        public event Action<string, int> LevelChanged;

        public UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SyncFromSave();
            _save.Loaded += SyncFromSave;

            return UniTask.CompletedTask;
        }

        /// <summary>Zero for an upgrade that has never been bought.</summary>
        public int GetLevel(string upgradeName)
            => upgradeName != null && _levels.TryGetValue(upgradeName, out int level) ? level : 0;

        public bool IsOwned(string upgradeName) => GetLevel(upgradeName) > 0;

        /// <summary>
        /// Raises the level by one and writes it through immediately — a purchase the player has
        /// paid for must never be lost to a crash. Returns the new level.
        /// </summary>
        public int LevelUp(string upgradeName)
        {
            if (string.IsNullOrEmpty(upgradeName))
                throw new ArgumentException("An upgrade needs a name.", nameof(upgradeName));

            int level = GetLevel(upgradeName) + 1;
            _levels[upgradeName] = level;

            _save.SaveUpgrade(new UpgradeSaveObject { upgradeName = upgradeName, currentLevel = level });
            LevelChanged?.Invoke(upgradeName, level);

            return level;
        }

        public void DisposeService()
        {
            _save.Loaded -= SyncFromSave;
            LevelChanged = null;
        }

        private void SyncFromSave()
        {
            _levels.Clear();

            foreach (UpgradeSaveObject upgrade in _save.Data.upgrades)
            {
                if (string.IsNullOrEmpty(upgrade.upgradeName)) continue;
                _levels[upgrade.upgradeName] = upgrade.currentLevel;
            }
        }
    }
}
