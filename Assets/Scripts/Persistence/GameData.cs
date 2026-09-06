using System;
using System.Collections.Generic;
using UnityEngine;

namespace Persistence
{
    [Serializable]
    public class GameData
    {
        /// <summary>
        /// Schema version, stamped on every write. A save whose version does not match
        /// <see cref="SaveService.CurrentSaveVersion"/> is discarded — acceptable while the game
        /// is in beta. Before public launch this must become a real migration path, or the first
        /// shape change wipes live players.
        /// </summary>
        public int saveVersion;

        public long currentPoints;
        public long currentUpgradePoints;
        public int currentObjective;
        public List<BoardObjectSaveData> boardObjects = new();
        public List<Vector2Int> unlockedCells = new();
        public List<UpgradeSaveObject> upgrades = new();
    }
}
