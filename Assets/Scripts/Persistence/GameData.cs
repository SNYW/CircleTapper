using System;
using System.Collections.Generic;
using Gameplay;
using UnityEngine;

namespace Persistence
{
    [Serializable]
    public class GameData
    {
        public long currentPoints;
        public long currentUpgradePoints;
        public string currentObjective;
        public List<BoardObjectSaveData> boardObjects = new();
        public List<Vector2Int> unlockedCells;
        public List<UpgradeSaveObject> upgrades;
    }
}