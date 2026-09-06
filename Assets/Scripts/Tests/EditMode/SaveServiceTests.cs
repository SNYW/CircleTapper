using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Persistence;
using UnityEngine;
using UnityEngine.TestTools;

namespace CircleTapper.Tests
{
    public class SaveServiceTests
    {
        private FakeDataService _data;
        private SaveService _save;

        /// <summary>Longer than the service's flush interval, so one tick is enough to write.</summary>
        private const float PastFlushInterval = 6f;

        [SetUp]
        public void SetUp() => _data = new FakeDataService();

        // Several tests exercise error paths deliberately and silence the expected logging.
        // Reset it here or that leaks into every test that runs afterwards.
        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        private SaveService Initialized()
        {
            _save = new SaveService(_data);
            _save.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            return _save;
        }

        // --- loading -------------------------------------------------------

        [Test]
        public void NoSaveOnDisk_StartsNewGame()
        {
            Initialized();

            Assert.IsTrue(_save.IsNewGame);
            Assert.AreEqual(SaveService.CurrentSaveVersion, _save.Data.saveVersion);
        }

        [Test]
        public void ValidSave_IsLoaded()
        {
            _data.Primary = FakeDataService.ValidSave(points: 1234);

            Initialized();

            Assert.IsFalse(_save.IsNewGame);
            Assert.AreEqual(1234, _save.Data.currentPoints);
        }

        [Test]
        public void VersionMismatch_StartsFreshRatherThanLoadingIt()
        {
            GameData stale = FakeDataService.ValidSave(points: 999);
            stale.saveVersion = SaveService.CurrentSaveVersion + 1;
            _data.Primary = stale;
            LogAssert.ignoreFailingMessages = true;

            Initialized();

            Assert.IsTrue(_save.IsNewGame);
            Assert.AreEqual(0, _save.Data.currentPoints, "stale save must not carry over");
        }

        [Test]
        public void SaveWithNoUnlockedCells_IsTreatedAsUnplayable()
        {
            GameData degenerate = FakeDataService.ValidSave(points: 500);
            degenerate.unlockedCells = new List<Vector2Int>();
            _data.Primary = degenerate;
            LogAssert.ignoreFailingMessages = true;

            Initialized();

            Assert.IsTrue(_save.IsNewGame);
        }

        [Test]
        public void CorruptSave_RecoversFromBackup()
        {
            _data.PrimaryIsCorrupt = true;
            _data.Backup = FakeDataService.ValidSave(points: 777);
            LogAssert.ignoreFailingMessages = true;

            Initialized();

            Assert.IsFalse(_save.IsNewGame, "a readable backup means this is not a new game");
            Assert.AreEqual(777, _save.Data.currentPoints);
        }

        [Test]
        public void CorruptSaveWithNoUsableBackup_IsQuarantinedNotDeleted()
        {
            _data.PrimaryIsCorrupt = true;
            _data.BackupIsCorrupt = true;
            LogAssert.ignoreFailingMessages = true;

            Initialized();

            Assert.AreEqual(1, _data.QuarantineCount, "the unreadable save must be preserved");
            Assert.IsTrue(_save.IsNewGame);
        }

        // --- write coalescing ----------------------------------------------

        [Test]
        public void MarkDirty_DoesNotWriteImmediately()
        {
            Initialized();
            int before = _data.SaveCount;

            _save.MarkDirty();

            Assert.AreEqual(before, _data.SaveCount, "dirty must not mean write now");
        }

        [Test]
        public void DirtyThenTickPastInterval_Writes()
        {
            Initialized();
            int before = _data.SaveCount;

            _save.MarkDirty();
            _save.Tick(PastFlushInterval);

            Assert.AreEqual(before + 1, _data.SaveCount);
        }

        [Test]
        public void TickWhileClean_NeverWrites()
        {
            Initialized();
            int before = _data.SaveCount;

            for (int i = 0; i < 10; i++) _save.Tick(PastFlushInterval);

            Assert.AreEqual(before, _data.SaveCount);
        }

        [Test]
        public void ManyDirtyMarks_CollapseIntoOneWrite()
        {
            Initialized();
            int before = _data.SaveCount;

            for (int i = 0; i < 500; i++) _save.MarkDirty();
            _save.Tick(PastFlushInterval);

            Assert.AreEqual(before + 1, _data.SaveCount, "coalescing is the whole point");
        }

        // --- progression is never deferred ---------------------------------

        [Test]
        public void RecordingAnUnlockedCell_WritesImmediately()
        {
            Initialized();
            int before = _data.SaveCount;

            bool recorded = _save.TryRecordUnlockedCell(new Vector2Int(3, 4));

            Assert.IsTrue(recorded);
            Assert.AreEqual(before + 1, _data.SaveCount, "progression must not wait for the timer");
        }

        [Test]
        public void RecordingTheSameCellTwice_IsRejected()
        {
            Initialized();
            _save.TryRecordUnlockedCell(new Vector2Int(3, 4));

            Assert.IsFalse(_save.TryRecordUnlockedCell(new Vector2Int(3, 4)));
            Assert.AreEqual(1, CountOccurrences(_save.Data.unlockedCells, new Vector2Int(3, 4)));
        }

        [Test]
        public void SavingAnUpgrade_WritesImmediatelyAndUpdatesInPlace()
        {
            Initialized();

            _save.SaveUpgrade(new UpgradeSaveObject { upgradeName = "Grid Size", currentLevel = 1 });
            int afterFirst = _data.SaveCount;
            _save.SaveUpgrade(new UpgradeSaveObject { upgradeName = "Grid Size", currentLevel = 2 });

            Assert.AreEqual(afterFirst + 1, _data.SaveCount);
            Assert.AreEqual(1, _save.Data.upgrades.Count, "the same upgrade must not be duplicated");
            Assert.AreEqual(2, _save.Data.upgrades[0].currentLevel);
        }

        // --- the bug that ate a board --------------------------------------

        [Test]
        public void SnapshotOfBoardObjects_IsDetachedFromTheLiveCollection()
        {
            Initialized();
            _save.SetBoardObject(Vector2Int.zero, new BoardObjectSaveData { type = "Circle" });

            List<BoardObjectSaveData> snapshot = _save.SnapshotBoardObjects();
            // Restoring a board writes each object back as it spawns; that must not disturb
            // anything already being iterated.
            _save.SetBoardObject(Vector2Int.one, new BoardObjectSaveData { type = "Square" });

            Assert.AreEqual(1, snapshot.Count, "the snapshot must not see later writes");
            Assert.AreEqual(2, _save.BoardObjects.Count);
        }

        [Test]
        public void RemovingABoardObject_TakesItOutOfTheNextWrite()
        {
            Initialized();
            _save.SetBoardObject(Vector2Int.zero, new BoardObjectSaveData { type = "Circle" });

            _save.RemoveBoardObject(Vector2Int.zero);
            _save.Flush();

            Assert.AreEqual(0, _save.Data.boardObjects.Count);
        }

        // --- writing --------------------------------------------------------

        [Test]
        public void EveryWrite_StampsTheCurrentVersion()
        {
            Initialized();
            _save.Data.saveVersion = 0;

            _save.Flush();

            Assert.AreEqual(SaveService.CurrentSaveVersion, _data.Primary.saveVersion);
        }

        [Test]
        public void DeletingTheSave_ClearsDiskAndMemory()
        {
            _data.Primary = FakeDataService.ValidSave(points: 50);
            Initialized();

            _save.DeleteSave();

            Assert.IsNull(_data.Primary);
            Assert.AreEqual(0, _save.Data.currentPoints);
            Assert.IsTrue(_save.IsNewGame);
        }

        [Test]
        public void PausingTheApp_FlushesPendingWrites()
        {
            Initialized();
            _save.MarkDirty();
            int before = _data.SaveCount;

            _save.OnApplicationPaused(true);

            Assert.AreEqual(before + 1, _data.SaveCount, "backgrounding must not lose progress");
        }

        private static int CountOccurrences(List<Vector2Int> cells, Vector2Int value)
        {
            int count = 0;
            foreach (Vector2Int cell in cells)
            {
                if (cell == value) count++;
            }

            return count;
        }
    }
}
