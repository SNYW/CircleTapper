using System;
using UnityEngine;

namespace Progression
{
    /// <summary>What an objective asks the player to do.</summary>
    public enum ObjectiveGoal
    {
        /// <summary>Accumulate currency. The open-ended goal the game falls back to.</summary>
        EarnPoints,

        /// <summary>Tap circles. Only counts the player's own taps.</summary>
        Tap,

        /// <summary>Buy a particular kind of board object.</summary>
        Buy,

        /// <summary>Merge a particular kind of board object.</summary>
        Merge
    }

    /// <summary>
    /// One authored objective. The early ones teach the game — tap, buy, merge, then the other
    /// object types — before it hands over to the open-ended earn-points curve.
    /// </summary>
    [Serializable]
    public class ObjectiveStep
    {
        [Tooltip("Shown to the player, e.g. \"Merge two circles\".")]
        public string label = "Objective";

        public ObjectiveGoal goal = ObjectiveGoal.EarnPoints;

        [Tooltip("How many. For EarnPoints this is the currency required.")]
        public int target = 1;

        [Tooltip("Which object this is about. Ignored unless the goal is Buy or Merge.")]
        public BoardObjectType objectType = BoardObjectType.Circle;

        /// <summary>
        /// Only earn-points objectives cost anything. A tutorial step is a task, not a purchase,
        /// so claiming one takes nothing from the player.
        /// </summary>
        public bool CostsCurrency => goal == ObjectiveGoal.EarnPoints;

        /// <summary>Whether a reported action counts towards this step.</summary>
        public bool Matches(ObjectiveGoal reported, BoardObjectType reportedType)
        {
            if (reported != goal) return false;

            // Tapping is not about a particular object; buying and merging are.
            bool caresAboutType = goal == ObjectiveGoal.Buy || goal == ObjectiveGoal.Merge;

            return !caresAboutType || reportedType == objectType;
        }
    }
}
