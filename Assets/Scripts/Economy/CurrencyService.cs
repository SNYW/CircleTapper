using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using Persistence;
using UnityEngine;

namespace Economy
{
    /// <summary>
    /// The player's two currencies: points earned from the board, and upgrade points earned by
    /// claiming objectives.
    /// <para>
    /// Deliberately knows nothing about the board or the grid. "Can I afford this?" is a
    /// currency question; "is there anywhere to put it?" is a board question, and belongs to the
    /// caller. Writes straight through to <see cref="SaveService"/>, so there is nothing to
    /// collect at save time.
    /// </para>
    /// </summary>
    public class CurrencyService : IGameService, IAsyncInitializable, IServiceDisposable
    {
        private readonly SaveService _save;

        public CurrencyService(SaveService save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
        }

        public long Points { get; private set; }
        public long UpgradePoints { get; private set; }

        /// <summary>Fires as (previous, current) on any change, so consumers need not poll.</summary>
        public event Action<long, long> PointsChanged;

        public event Action<long, long> UpgradePointsChanged;

        public UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SyncFromSave();

            // A reset or a delete-save is a new session; re-read rather than keep stale totals.
            _save.Loaded += SyncFromSave;

            return UniTask.CompletedTask;
        }

        public void AddPoints(long amount)
        {
            if (amount <= 0) return;
            SetPoints(Points + amount);
        }

        public void AddUpgradePoints(long amount)
        {
            if (amount <= 0) return;
            SetUpgradePoints(UpgradePoints + amount);
        }

        public bool CanAfford(long cost) => cost <= Points;

        public bool CanAffordUpgrade(long cost) => cost <= UpgradePoints;

        /// <summary>Spends only if the whole cost is affordable.</summary>
        public bool TrySpend(long cost)
        {
            if (cost < 0 || !CanAfford(cost)) return false;

            SetPoints(Points - cost);
            return true;
        }

        public bool TrySpendUpgradePoints(long cost)
        {
            if (cost < 0 || !CanAffordUpgrade(cost)) return false;

            SetUpgradePoints(UpgradePoints - cost);
            return true;
        }

        public void DisposeService()
        {
            _save.Loaded -= SyncFromSave;
            PointsChanged = null;
            UpgradePointsChanged = null;
        }

        private void SyncFromSave()
        {
            SetPoints(_save.Data.currentPoints);
            SetUpgradePoints(_save.Data.currentUpgradePoints);
        }

        private void SetPoints(long value)
        {
            long previous = Points;
            Points = Math.Max(0, value);
            if (Points == previous) return;

            _save.Data.currentPoints = Points;
            _save.MarkDirty();
            PointsChanged?.Invoke(previous, Points);
        }

        private void SetUpgradePoints(long value)
        {
            long previous = UpgradePoints;
            UpgradePoints = Math.Max(0, value);
            if (UpgradePoints == previous) return;

            _save.Data.currentUpgradePoints = UpgradePoints;
            _save.MarkDirty();
            UpgradePointsChanged?.Invoke(previous, UpgradePoints);
        }
    }
}
