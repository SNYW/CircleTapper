using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using Economy;
using Persistence;

namespace Progression
{
    /// <summary>
    /// The rolling "earn this many points, get an upgrade point" objective.
    /// <para>
    /// Only ever one objective is live, identified by its number, so the whole of the player's
    /// progress here is a single integer. The cost curve is a pure function of that number.
    /// </para>
    /// </summary>
    public class ObjectiveService : IGameService, IAsyncInitializable, IServiceDisposable
    {
        /// <summary>Awarded for claiming one objective.</summary>
        public const int UpgradePointsPerClaim = 1;

        private const int FirstObjective = 1;

        private readonly SaveService _save;
        private readonly CurrencyService _currency;

        public ObjectiveService(SaveService save, CurrencyService currency)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _currency = currency ?? throw new ArgumentNullException(nameof(currency));
        }

        public int Current { get; private set; } = FirstObjective;

        /// <summary>What the current objective costs to claim.</summary>
        public int CurrentCost => CostOf(Current);

        public bool CanClaim => _currency.Points >= CurrentCost;

        /// <summary>Fires with the new objective number.</summary>
        public event Action<int> CurrentChanged;

        public UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SyncFromSave();
            _save.Loaded += SyncFromSave;

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Spends the objective's cost and advances to the next one, awarding an upgrade point.
        /// False if the player cannot afford it, in which case nothing changes.
        /// </summary>
        public bool TryClaim()
        {
            if (!_currency.TrySpend(CurrentCost)) return false;

            SetCurrent(Current + 1);
            _currency.AddUpgradePoints(UpgradePointsPerClaim);

            // Progression, and it just cost the player their points. Do not defer this.
            _save.Flush();
            return true;
        }

        /// <summary>
        /// Cost of a given objective. Steepens at 20 and again at 30.
        /// <para>Balance lives in code for now; it belongs in a ScriptableObject.</para>
        /// </summary>
        public static int CostOf(int objective)
        {
            int multiplier = objective switch
            {
                < 20 => 10,
                < 30 => 50,
                _ => 100
            };

            return objective * multiplier;
        }

        public void DisposeService()
        {
            _save.Loaded -= SyncFromSave;
            CurrentChanged = null;
        }

        private void SyncFromSave() => SetCurrent(Math.Max(FirstObjective, _save.Data.currentObjective));

        private void SetCurrent(int value)
        {
            if (value == Current && _save.Data.currentObjective == value) return;

            Current = value;
            _save.Data.currentObjective = value;
            _save.MarkDirty();
            CurrentChanged?.Invoke(value);
        }
    }
}
