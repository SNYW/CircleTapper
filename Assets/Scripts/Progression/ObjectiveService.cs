using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using Economy;
using Persistence;
using UnityEngine;

namespace Progression
{
    /// <summary>
    /// The rolling objective, and the upgrade point it pays out.
    /// <para>
    /// The opening objectives are authored and teach the game — tap, buy, merge, then the other
    /// object types. Past those it falls back to an open-ended earn-points curve, so the asset
    /// only has to describe the start of the game rather than all of it.
    /// </para>
    /// </summary>
    public class ObjectiveService : IGameService, IAsyncInitializable, IServiceDisposable
    {
        public const int UpgradePointsPerClaim = 1;

        private const int FirstObjective = 1;

        private const long DefaultBaseCost = 1000;
        private const float DefaultGrowth = 1.6f;
        private const string SetResourcePath = "Data/Objectives/ObjectiveSet";
        private const string EarnPointsLabel = "Next Upgrade";

        private readonly SaveService _save;
        private readonly CurrencyService _currency;

        private ObjectiveSet _set;

        /// <param name="set">
        /// The authored objectives. Loaded from Resources when omitted; tests pass their own.
        /// </param>
        public ObjectiveService(SaveService save, CurrencyService currency, ObjectiveSet set = null)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _currency = currency ?? throw new ArgumentNullException(nameof(currency));
            _set = set;
        }

        /// <summary>Which objective is live, counting from one.</summary>
        public int Current { get; private set; } = FirstObjective;

        /// <summary>Progress towards a task objective. Unused while earning points.</summary>
        public int TaskProgress { get; private set; }

        /// <summary>Fires with the new objective number.</summary>
        public event Action<int> CurrentChanged;

        /// <summary>Fires when progress moves without the objective changing.</summary>
        public event Action Progressed;

        public UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_set == null)
            {
                _set = Resources.Load<ObjectiveSet>(SetResourcePath);

                if (_set == null)
                {
                    Debug.LogWarning(
                        $"No ObjectiveSet at Resources/{SetResourcePath}; earn-points objectives only.");
                }
            }

            SyncFromSave();
            _save.Loaded += SyncFromSave;

            return UniTask.CompletedTask;
        }

        /// <summary>The authored step for the current objective, or null once they run out.</summary>
        private ObjectiveStep Step => _set != null ? _set.At(Current - 1) : null;

        public string Label => Step?.label ?? EarnPointsLabel;

        /// <summary>Where the player is, in whatever units this objective counts.</summary>
        public long Progress => IsEarnPoints ? _currency.Points : TaskProgress;

        /// <summary>What this objective needs, in the same units as <see cref="Progress"/>.</summary>
        public long Target
        {
            get
            {
                ObjectiveStep step = Step;
                if (step != null)
                {
                    return step.goal == ObjectiveGoal.EarnPoints ? step.target : Mathf.Max(step.target, 1);
                }

                return _set != null
                    ? CostOf(CurveStep, _set.FallbackBaseCost, _set.FallbackGrowth)
                    : CostOf(CurveStep);
            }
        }

        /// <summary>Position along the open-ended curve, counting from one past the authored set.</summary>
        private int CurveStep => Current - (_set != null ? _set.Count : 0);

        /// <summary>
        /// Whether this kind of object is available to buy yet. It stays out of the shop until the
        /// tutorial objective that introduces it comes up, so the buy panel reveals itself.
        /// </summary>
        public bool IsUnlocked(BoardObjectType objectType)
        {
            if (_set == null) return true;

            int introducedAt = _set.IndexOfFirstBuy(objectType);
            if (introducedAt < 0) return true;

            // Index is zero-based, Current counts from one.
            return Current >= introducedAt + 1;
        }

        public bool CanClaim => Progress >= Target;

        private bool IsEarnPoints
        {
            get
            {
                ObjectiveStep step = Step;
                return step == null || step.goal == ObjectiveGoal.EarnPoints;
            }
        }

        /// <summary>
        /// Counts an action the player took. Only the live objective is listening, and only if it
        /// asked for this kind of action.
        /// </summary>
        public void Report(ObjectiveGoal goal, BoardObjectType objectType = BoardObjectType.Circle)
        {
            ObjectiveStep step = Step;
            if (step == null || !step.Matches(goal, objectType)) return;
            if (TaskProgress >= step.target) return;

            TaskProgress++;
            _save.Data.objectiveProgress = TaskProgress;
            _save.MarkDirty();

            Progressed?.Invoke();
        }

        /// <summary>
        /// Advances to the next objective, awarding an upgrade point. Earn-points objectives take
        /// the currency; the authored task objectives are free, because doing the thing was the
        /// price. False if the objective is not finished, in which case nothing changes.
        /// </summary>
        public bool TryClaim()
        {
            if (!CanClaim) return false;

            // Only charge for the ones that are actually a purchase.
            if (IsEarnPoints && !_currency.TrySpend(Target)) return false;

            SetCurrent(Current + 1);
            _currency.AddUpgradePoints(UpgradePointsPerClaim);

            // Progression, and it may have just cost the player their points. Do not defer this.
            _save.Flush();
            return true;
        }

        /// <summary>
        /// Cost of the open-ended objectives, which take over once the authored ones run out.
        /// <para>
        /// <paramref name="curveStep"/> counts from one at the first open-ended objective, not
        /// from the start of the game — otherwise the curve would restart at a price the player
        /// blew past during the tutorial and hand them several upgrade points for nothing.
        /// </para>
        /// </summary>
        public static long CostOf(int curveStep, long baseCost = DefaultBaseCost, float growth = DefaultGrowth)
        {
            int stepsPast = Math.Max(0, curveStep - 1);
            return (long)Math.Round(baseCost * Math.Pow(growth, stepsPast));
        }

        public void DisposeService()
        {
            _save.Loaded -= SyncFromSave;
            CurrentChanged = null;
            Progressed = null;
        }

        private void SyncFromSave()
        {
            TaskProgress = Mathf.Max(0, _save.Data.objectiveProgress);
            SetCurrent(Math.Max(FirstObjective, _save.Data.currentObjective));
        }

        private void SetCurrent(int value)
        {
            bool changed = value != Current;

            Current = value;
            TaskProgress = changed ? 0 : TaskProgress;

            _save.Data.currentObjective = value;
            _save.Data.objectiveProgress = TaskProgress;
            _save.MarkDirty();

            if (changed) CurrentChanged?.Invoke(value);
        }
    }
}
