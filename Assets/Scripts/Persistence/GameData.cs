using System;
using System.Collections.Generic;
using UnityEngine;

namespace Persistence
{
    [Serializable]
    public class GameData
    {
        public long currentPoints;
        public string currentObjective;
        public List<BoardObjectSaveData> boardObjects = new();
        public List<Vector2Int> unlockedCells;
    }
}