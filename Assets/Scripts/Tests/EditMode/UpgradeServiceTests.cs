using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Persistence;
using Progression;

namespace CircleTapper.Tests
{
    public class UpgradeServiceTests
    {
        private const string GridSize = "Grid Size";
        private const string CircleValue = "Circle Value +2";

        private FakeDataService _data;
        private SaveService _save;
        private UpgradeService _upgrades;

        [SetUp]
        public void SetUp() => _data = new FakeDataService();

        private UpgradeService Initialized(params UpgradeSaveObject[] owned)
        {
            GameData save = FakeDataService.ValidSave();
            save.upgrades = new List<UpgradeSaveObject>(owned);
            _data.Primary = save;

            _save = new SaveService(_data);
            _save.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            _upgrades = new UpgradeService(_save);
            _upgrades.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            return _upgrades;
        }

        private static UpgradeSaveObject Owned(string name, int level)
            => new() { upgradeName = name, currentLevel = level };

        [Test]
        public void AnUnboughtUpgrade_IsLevelZeroRatherThanMissing()
        {
            Initialized();

            Assert.AreEqual(0, _upgrades.GetLevel(GridSize));
            Assert.IsFalse(_upgrades.IsOwned(GridSize));
        }

        [Test]
        public void LevelsAreReadFromTheSave()
        {
            Initialized(Owned(GridSize, 4), Owned(CircleValue, 2));

            Assert.AreEqual(4, _upgrades.GetLevel(GridSize));
            Assert.AreEqual(2, _upgrades.GetLevel(CircleValue));
            Assert.IsTrue(_upgrades.IsOwned(GridSize));
        }

        [Test]
        public void LevellingUp_ReturnsAndStoresTheNewLevel()
        {
            Initialized(Owned(GridSize, 4));

            int level = _upgrades.LevelUp(GridSize);

            Assert.AreEqual(5, level);
            Assert.AreEqual(5, _upgrades.GetLevel(GridSize));
        }

        [Test]
        public void LevellingUpSomethingUnowned_StartsAtOne()
        {
            Initialized();

            Assert.AreEqual(1, _upgrades.LevelUp(CircleValue));
        }

        [Test]
        public void LevellingUp_RaisesTheChangeEvent()
        {
            Initialized();
            string changed = null;
            int newLevel = 0;
            _upgrades.LevelChanged += (name, level) => { changed = name; newLevel = level; };

            _upgrades.LevelUp(CircleValue);

            Assert.AreEqual(CircleValue, changed);
            Assert.AreEqual(1, newLevel);
        }

        [Test]
        public void LevellingUp_WritesImmediatelyRatherThanWaitingForTheTimer()
        {
            Initialized();
            int before = _data.SaveCount;

            _upgrades.LevelUp(GridSize);

            Assert.AreEqual(before + 1, _data.SaveCount, "a paid-for upgrade must not be lost");
        }

        [Test]
        public void LevellingUpRepeatedly_UpdatesInPlaceRatherThanDuplicating()
        {
            Initialized();

            _upgrades.LevelUp(GridSize);
            _upgrades.LevelUp(GridSize);
            _upgrades.LevelUp(GridSize);

            Assert.AreEqual(3, _upgrades.GetLevel(GridSize));
            Assert.AreEqual(1, _save.Data.upgrades.Count, "one entry per upgrade, not one per purchase");
            Assert.AreEqual(3, _save.Data.upgrades[0].currentLevel);
        }

        [Test]
        public void LevelsSurviveIntoTheSavedData()
        {
            Initialized(Owned(GridSize, 1));

            _upgrades.LevelUp(GridSize);

            UpgradeSaveObject stored = _save.Data.upgrades.Find(u => u.upgradeName == GridSize);
            Assert.IsNotNull(stored);
            Assert.AreEqual(2, stored.currentLevel);
        }

        [Test]
        public void AnUnknownName_IsLevelZeroAndNotAnError()
        {
            Initialized(Owned(GridSize, 3));

            Assert.AreEqual(0, _upgrades.GetLevel("Does Not Exist"));
            Assert.AreEqual(0, _upgrades.GetLevel(null));
        }

        [Test]
        public void ResettingTheSave_ClearsEveryLevel()
        {
            Initialized(Owned(GridSize, 6), Owned(CircleValue, 3));

            _save.ResetToNewGame();

            Assert.AreEqual(0, _upgrades.GetLevel(GridSize));
            Assert.AreEqual(0, _upgrades.GetLevel(CircleValue));
            Assert.IsEmpty(_upgrades.Levels);
        }

        [Test]
        public void Disposing_StopsListeningToTheSave()
        {
            Initialized(Owned(GridSize, 6));

            _upgrades.DisposeService();
            _save.ResetToNewGame();

            Assert.AreEqual(6, _upgrades.GetLevel(GridSize), "a disposed service must not still react");
        }
    }
}
