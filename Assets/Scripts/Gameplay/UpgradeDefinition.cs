using System;
using UnityEngine;

namespace Gameplay
{
    public abstract class UpgradeDefinition : ScriptableObject
    {
        public string upgradeName;
        public int maxLevel;

        public abstract void OnLevelUp();
        public abstract bool CanPurchase();
        public abstract int GetPurchasePrice();
        public abstract string GetLevelInfo();
        public abstract bool IsMaxed();
    }
}