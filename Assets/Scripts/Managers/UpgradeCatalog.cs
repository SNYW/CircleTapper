using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using Gameplay;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// The upgrade definitions available in the game, loaded from Resources and keyed by name.
    /// <para>
    /// Lives in the game assembly rather than alongside <see cref="Progression.UpgradeService"/>
    /// because it deals in ScriptableObjects, and the definitions reach into the board, the
    /// currency and FMOD. The service owns the player's levels; this owns the catalogue.
    /// </para>
    /// </summary>
    public class UpgradeCatalog : IGameService, IAsyncInitializable
    {
        private const string ResourcesPath = "Data/Upgrades/";

        private readonly Dictionary<string, UpgradeDefinition> _definitions = new();

        public IReadOnlyCollection<UpgradeDefinition> All => _definitions.Values;

        public UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _definitions.Clear();

            foreach (UpgradeDefinition definition in Resources.LoadAll<UpgradeDefinition>(ResourcesPath))
            {
                if (string.IsNullOrEmpty(definition.upgradeName))
                {
                    Debug.LogError($"Upgrade definition '{definition.name}' has no upgradeName; skipping it.");
                    continue;
                }

                if (!_definitions.TryAdd(definition.upgradeName, definition))
                {
                    Debug.LogError($"Two upgrade definitions share the name '{definition.upgradeName}'.");
                }
            }

            return UniTask.CompletedTask;
        }

        public bool TryGet(string upgradeName, out UpgradeDefinition definition)
            => _definitions.TryGetValue(upgradeName ?? string.Empty, out definition);

        public bool TryGet<T>(string upgradeName, out T definition) where T : UpgradeDefinition
        {
            definition = TryGet(upgradeName, out UpgradeDefinition found) ? found as T : null;
            return definition != null;
        }

        public bool CanPurchaseAny() => _definitions.Values.Any(definition => definition.CanPurchase());

        public bool AllComplete() => _definitions.Values.All(definition => definition.IsMaxed());
    }
}
