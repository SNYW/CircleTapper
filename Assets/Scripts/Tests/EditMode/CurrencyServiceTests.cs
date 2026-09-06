using System.Threading;
using Economy;
using NUnit.Framework;
using Persistence;

namespace CircleTapper.Tests
{
    public class CurrencyServiceTests
    {
        private FakeDataService _data;
        private SaveService _save;
        private CurrencyService _currency;

        [SetUp]
        public void SetUp() => _data = new FakeDataService();

        private CurrencyService Initialized(long points = 0, long upgradePoints = 0)
        {
            _data.Primary = FakeDataService.ValidSave(points, upgradePoints);

            _save = new SaveService(_data);
            _save.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            _currency = new CurrencyService(_save);
            _currency.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            return _currency;
        }

        [Test]
        public void Initializing_TakesTotalsFromTheSave()
        {
            Initialized(points: 250, upgradePoints: 7);

            Assert.AreEqual(250, _currency.Points);
            Assert.AreEqual(7, _currency.UpgradePoints);
        }

        [Test]
        public void AddingPoints_ReportsPreviousAndCurrent()
        {
            Initialized(points: 100);
            long previous = -1, current = -1;
            _currency.PointsChanged += (p, c) => { previous = p; current = c; };

            _currency.AddPoints(50);

            Assert.AreEqual(100, previous);
            Assert.AreEqual(150, current);
            Assert.AreEqual(150, _currency.Points);
        }

        [Test]
        public void AddingNothingOrLess_ChangesNothingAndRaisesNoEvent()
        {
            Initialized(points: 100);
            int raised = 0;
            _currency.PointsChanged += (_, _) => raised++;

            _currency.AddPoints(0);
            _currency.AddPoints(-25);

            Assert.AreEqual(100, _currency.Points);
            Assert.AreEqual(0, raised);
        }

        [Test]
        public void SpendingMoreThanYouHave_IsRefusedAndCostsNothing()
        {
            Initialized(points: 40);

            bool spent = _currency.TrySpend(41);

            Assert.IsFalse(spent);
            Assert.AreEqual(40, _currency.Points, "a refused purchase must not deduct");
        }

        [Test]
        public void SpendingExactlyWhatYouHave_IsAllowed()
        {
            Initialized(points: 40);

            Assert.IsTrue(_currency.TrySpend(40));
            Assert.AreEqual(0, _currency.Points);
        }

        [Test]
        public void SpendingANegativeAmount_IsRefused()
        {
            Initialized(points: 40);

            Assert.IsFalse(_currency.TrySpend(-10), "spending a negative must not be free money");
            Assert.AreEqual(40, _currency.Points);
        }

        [Test]
        public void UpgradePoints_AreSpentSeparatelyFromPoints()
        {
            Initialized(points: 1000, upgradePoints: 2);

            Assert.IsFalse(_currency.TrySpendUpgradePoints(3), "points must not pay for upgrades");
            Assert.IsTrue(_currency.TrySpendUpgradePoints(2));
            Assert.AreEqual(0, _currency.UpgradePoints);
            Assert.AreEqual(1000, _currency.Points);
        }

        [Test]
        public void EveryChange_WritesThroughToTheSave()
        {
            Initialized(points: 10, upgradePoints: 1);

            _currency.AddPoints(5);
            _currency.AddUpgradePoints(2);

            Assert.AreEqual(15, _save.Data.currentPoints);
            Assert.AreEqual(3, _save.Data.currentUpgradePoints);
        }

        [Test]
        public void ChangingCurrency_MarksTheSaveDirtyWithoutWritingImmediately()
        {
            Initialized(points: 10);
            int before = _data.SaveCount;

            _currency.AddPoints(5);

            Assert.AreEqual(before, _data.SaveCount, "currency changes ride the coalesced write");
            _save.Tick(6f);
            Assert.AreEqual(before + 1, _data.SaveCount);
        }

        [Test]
        public void ResettingTheSave_ResyncsTotalsInsteadOfLeavingThemStale()
        {
            Initialized(points: 500, upgradePoints: 9);

            _save.ResetToNewGame();

            Assert.AreEqual(0, _currency.Points, "a deleted save must not leave currency behind");
            Assert.AreEqual(0, _currency.UpgradePoints);
        }

        [Test]
        public void Disposing_StopsListeningToTheSave()
        {
            Initialized(points: 500);

            _currency.DisposeService();
            _save.ResetToNewGame();

            Assert.AreEqual(500, _currency.Points, "a disposed service must not still react");
        }
    }
}
