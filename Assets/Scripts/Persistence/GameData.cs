using System;
using System.Collections.Generic;

namespace Persistence
{
    [Serializable]
    public class GameData
    {
        public long currentPoints;
        public string currentObjective;
        public List<BoardObjectSaveData> boardObjects = new();
    }
}