using System;
using System.Collections.Generic;

namespace Persistence
{
    [Serializable]
    public class GameData
    {
        public string currentPoints;
        public List<BoardObjectSaveData> boardObjects = new();
    }
}