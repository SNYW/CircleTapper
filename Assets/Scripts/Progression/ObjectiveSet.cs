using System.Collections.Generic;
using UnityEngine;

namespace Progression
{
    /// <summary>
    /// The authored objectives, in order. Runs out deliberately — past the last one the game
    /// falls back to the open-ended earn-points curve, so this asset only has to describe the
    /// opening of the game rather than all of it.
    /// </summary>
    [CreateAssetMenu(menuName = "Game Data/ Objective Set", fileName = "ObjectiveSet")]
    public class ObjectiveSet : ScriptableObject
    {
        [SerializeField] private List<ObjectiveStep> steps = new();

        [Header("Once the authored objectives run out")]
        [Tooltip("Cost of the first open-ended objective. Should exceed what the tutorial paid " +
                 "out, or the player claims several for free the moment it takes over.")]
        [SerializeField] private long fallbackBaseCost = 1000;

        [Tooltip("Multiplied in for each open-ended objective after the first.")]
        [SerializeField] private float fallbackGrowth = 1.6f;

        public int Count => steps.Count;

        public long FallbackBaseCost => fallbackBaseCost;

        public float FallbackGrowth => fallbackGrowth;

        /// <summary>
        /// The first authored Buy objective for this kind of object, or -1 if it is never asked
        /// for. Used to keep an object out of the shop until the tutorial introduces it.
        /// </summary>
        public int IndexOfFirstBuy(BoardObjectType objectType)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                ObjectiveStep step = steps[i];
                if (step.goal == ObjectiveGoal.Buy && step.objectType == objectType) return i;
            }

            return -1;
        }

        /// <summary>
        /// Builds a set in code. For tests and editor tooling — the real one is authored as an
        /// asset and loaded from Resources.
        /// </summary>
        public static ObjectiveSet Create(params ObjectiveStep[] steps)
        {
            ObjectiveSet set = CreateInstance<ObjectiveSet>();
            set.steps = new List<ObjectiveStep>(steps);
            return set;
        }

        /// <summary>Null once the authored objectives are exhausted.</summary>
        public ObjectiveStep At(int index)
            => index >= 0 && index < steps.Count ? steps[index] : null;
    }
}
