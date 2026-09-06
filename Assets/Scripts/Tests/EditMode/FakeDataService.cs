using System.Collections.Generic;
using System.IO;
using Persistence;
using UnityEngine;

namespace CircleTapper.Tests
{
    /// <summary>
    /// In-memory <see cref="IDataService"/> with switches for the failure modes that matter:
    /// no save, an unreadable save, and an unreadable backup.
    /// </summary>
    public class FakeDataService : IDataService
    {
        public GameData Primary;
        public GameData Backup;

        public bool PrimaryIsCorrupt;
        public bool BackupIsCorrupt;

        public int SaveCount;
        public int QuarantineCount;
        public readonly List<GameData> Written = new();

        public void Save(string fileName, GameData data)
        {
            SaveCount++;
            Written.Add(data);
            Primary = data;
            PrimaryIsCorrupt = false;
        }

        public bool Exists(string fileName) => Primary != null || PrimaryIsCorrupt;

        public GameData Load(string fileName)
        {
            if (PrimaryIsCorrupt) throw new InvalidDataException("corrupt (test)");
            if (Primary == null) throw new FileNotFoundException("missing (test)");

            return Primary;
        }

        public bool TryLoadBackup(string fileName, out GameData data)
        {
            data = null;
            if (BackupIsCorrupt || Backup == null) return false;

            data = Backup;
            return true;
        }

        public void QuarantineCorrupt(string fileName)
        {
            QuarantineCount++;
            PrimaryIsCorrupt = false;
            Primary = null;
        }

        public void Delete(string fileName)
        {
            Primary = null;
            Backup = null;
            PrimaryIsCorrupt = false;
        }

        /// <summary>A save that will load cleanly: current version, at least one unlocked cell.</summary>
        public static GameData ValidSave(long points = 0, long upgradePoints = 0)
        {
            return new GameData
            {
                saveVersion = SaveService.CurrentSaveVersion,
                currentPoints = points,
                currentUpgradePoints = upgradePoints,
                currentObjective = 1,
                boardObjects = new List<BoardObjectSaveData>(),
                unlockedCells = new List<Vector2Int> { Vector2Int.zero },
                upgrades = new List<UpgradeSaveObject>()
            };
        }
    }
}
