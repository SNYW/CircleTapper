using System;

namespace Persistence
{
    /// <summary>Serialized form of a purchased upgrade. Lives here so it ships with the save.</summary>
    [Serializable]
    public class UpgradeSaveObject
    {
        public string upgradeName;
        public int currentLevel;
    }
}
