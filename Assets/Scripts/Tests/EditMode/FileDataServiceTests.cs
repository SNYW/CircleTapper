using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Persistence;
using UnityEngine;
using UnityEngine.TestTools;

namespace CircleTapper.Tests
{
    /// <summary>
    /// Integration tests against real files in a temp directory.
    /// <para>
    /// The rest of the save suite runs on a fake, which cannot exercise the thing that actually
    /// protects a player's progress: writing to a temp file, rotating the previous save to a
    /// backup, and only then moving the new one into place.
    /// </para>
    /// </summary>
    public class FileDataServiceTests
    {
        private const string SaveName = "TestSave";

        private string _directory;
        private FileDataService _service;

        private string Primary => Path.Combine(_directory, SaveName + ".json");
        private string Backup => Path.Combine(_directory, SaveName + ".bak");
        private string Temp => Path.Combine(_directory, SaveName + ".tmp");

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "CircleTapperTests", Path.GetRandomFileName());
            Directory.CreateDirectory(_directory);

            _service = new FileDataService(new JsonSerializer(), _directory);
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;

            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        private static GameData SaveWith(long points)
        {
            return new GameData
            {
                saveVersion = SaveService.CurrentSaveVersion,
                currentPoints = points,
                currentObjective = 1,
                boardObjects = new List<BoardObjectSaveData>(),
                unlockedCells = new List<Vector2Int> { Vector2Int.zero },
                upgrades = new List<UpgradeSaveObject>()
            };
        }

        // --- the basics --------------------------------------------------------

        [Test]
        public void NothingExistsBeforeTheFirstWrite()
        {
            Assert.IsFalse(_service.Exists(SaveName));
        }

        [Test]
        public void WhatIsSavedIsWhatLoads()
        {
            _service.Save(SaveName, SaveWith(4242));

            GameData loaded = _service.Load(SaveName);

            Assert.AreEqual(4242, loaded.currentPoints);
            Assert.AreEqual(SaveService.CurrentSaveVersion, loaded.saveVersion);
            Assert.IsTrue(_service.Exists(SaveName));
        }

        [Test]
        public void LoadingWhatIsNotThere_Throws()
        {
            Assert.Throws<FileNotFoundException>(() => _service.Load(SaveName));
        }

        [Test]
        public void LoadingRubbish_Throws()
        {
            File.WriteAllText(Primary, "this is not json");
            LogAssert.ignoreFailingMessages = true;

            Assert.Catch(() => _service.Load(SaveName));
        }

        // --- the point of the whole design -------------------------------------

        [Test]
        public void ASuccessfulWrite_LeavesNoTempFileBehind()
        {
            _service.Save(SaveName, SaveWith(1));

            Assert.IsFalse(File.Exists(Temp), "a leftover temp file means the move did not happen");
        }

        [Test]
        public void TheFirstWrite_CreatesNoBackup()
        {
            _service.Save(SaveName, SaveWith(1));

            Assert.IsFalse(File.Exists(Backup), "there was no previous save to rotate out");
        }

        [Test]
        public void TheSecondWrite_RotatesTheFirstIntoTheBackup()
        {
            _service.Save(SaveName, SaveWith(100));
            _service.Save(SaveName, SaveWith(200));

            Assert.IsTrue(File.Exists(Backup));
            Assert.AreEqual(200, _service.Load(SaveName).currentPoints, "primary holds the newest");

            Assert.IsTrue(_service.TryLoadBackup(SaveName, out GameData backup));
            Assert.AreEqual(100, backup.currentPoints, "backup holds the previous");
        }

        [Test]
        public void RepeatedWrites_OnlyEverKeepTheLastTwo()
        {
            _service.Save(SaveName, SaveWith(1));
            _service.Save(SaveName, SaveWith(2));
            _service.Save(SaveName, SaveWith(3));

            Assert.AreEqual(3, _service.Load(SaveName).currentPoints);

            _service.TryLoadBackup(SaveName, out GameData backup);
            Assert.AreEqual(2, backup.currentPoints, "the backup is the previous write, not the first");
        }

        [Test]
        public void ATempFileLeftByAnInterruptedWrite_DoesNotBreakTheNextOne()
        {
            _service.Save(SaveName, SaveWith(10));

            // What a kill between WriteAllText and the move would leave behind.
            File.WriteAllText(Temp, "half written garbage");

            _service.Save(SaveName, SaveWith(20));

            Assert.AreEqual(20, _service.Load(SaveName).currentPoints);
            Assert.IsFalse(File.Exists(Temp), "the stale temp file should have been replaced and moved");
        }

        [Test]
        public void TheSaveIsNeverTheFileBeingWritten()
        {
            _service.Save(SaveName, SaveWith(1));
            string afterFirst = File.ReadAllText(Primary);

            _service.Save(SaveName, SaveWith(2));

            // The previous contents survive intact in the backup rather than being overwritten
            // in place, which is what makes a kill mid-write survivable.
            Assert.AreEqual(afterFirst, File.ReadAllText(Backup));
        }

        // --- never destroy a player's data -------------------------------------

        [Test]
        public void QuarantinePreservesTheFileRatherThanDeletingIt()
        {
            File.WriteAllText(Primary, "corrupt but precious");
            LogAssert.ignoreFailingMessages = true;

            _service.QuarantineCorrupt(SaveName);

            Assert.IsFalse(File.Exists(Primary), "it should be moved out of the way");

            string[] quarantined = Directory.GetFiles(_directory, SaveName + ".corrupt.*");
            Assert.AreEqual(1, quarantined.Length, "exactly one quarantined copy");
            Assert.AreEqual("corrupt but precious", File.ReadAllText(quarantined[0]), "content intact");
        }

        [Test]
        public void QuarantiningNothing_IsHarmless()
        {
            Assert.DoesNotThrow(() => _service.QuarantineCorrupt(SaveName));
        }

        [Test]
        public void BackupRecovery_ReportsFailureRatherThanThrowing()
        {
            Assert.IsFalse(_service.TryLoadBackup(SaveName, out GameData none));
            Assert.IsNull(none);

            File.WriteAllText(Backup, "not json either");
            LogAssert.ignoreFailingMessages = true;

            Assert.IsFalse(_service.TryLoadBackup(SaveName, out GameData broken));
            Assert.IsNull(broken);
        }

        [Test]
        public void DeleteRemovesEveryTrace()
        {
            _service.Save(SaveName, SaveWith(1));
            _service.Save(SaveName, SaveWith(2));
            File.WriteAllText(Temp, "leftover");

            _service.Delete(SaveName);

            Assert.IsFalse(File.Exists(Primary));
            Assert.IsFalse(File.Exists(Backup));
            Assert.IsFalse(File.Exists(Temp));
            Assert.IsFalse(_service.Exists(SaveName));
        }

        [Test]
        public void DeletingNothing_IsHarmless()
        {
            Assert.DoesNotThrow(() => _service.Delete(SaveName));
        }
    }
}
