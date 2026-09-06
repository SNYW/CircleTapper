using System.Threading;
using Economy;
using NUnit.Framework;
using Persistence;
using Progression;

namespace CircleTapper.Tests
{
    public class ObjectiveServiceTests
    {
        private FakeDataService _data;
        private SaveService _save;
        private CurrencyService _currency;
        private ObjectiveService _objectives;

        [SetUp]
        public void SetUp() => _data = new FakeDataService();

        private ObjectiveService Initialized(int objective = 1, long points = 0)
        {
            GameData save = FakeDataService.ValidSave(points);
            save.currentObjective = objective;
            _data.Primary = save;

            _save = new SaveService(_data);
            _save.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            _currency = new CurrencyService(_save);
            _currency.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            _objectives = new ObjectiveService(_save, _currency);
            _objectives.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            return _objectives;
        }

        // --- the cost curve --------------------------------------------------

        [Test]
        public void CostCurve_SteepensAtTwentyAndAgainAtThirty()
        {
            Assert.AreEqual(10, ObjectiveService.CostOf(1), "1 x 10");
            Assert.AreEqual(190, ObjectiveService.CostOf(19), "still x10 just under the first step");
            Assert.AreEqual(1000, ObjectiveService.CostOf(20), "20 x 50");
            Assert.AreEqual(1450, ObjectiveService.CostOf(29), "still x50 just under the second step");
            Assert.AreEqual(3000, ObjectiveService.CostOf(30), "30 x 100");
        }

        [Test]
        public void CurrentCost_FollowsTheCurrentObjective()
        {
            Initialized(objective: 20);

            Assert.AreEqual(1000, _objectives.CurrentCost);
        }

        // --- loading ----------------------------------------------------------

        [Test]
        public void Initializing_TakesTheObjectiveFromTheSave()
        {
            Initialized(objective: 7);

            Assert.AreEqual(7, _objectives.Current);
        }

        [Test]
        public void AnObjectiveBelowOne_IsClampedToTheFirst()
        {
            Initialized(objective: 0);

            Assert.AreEqual(1, _objectives.Current, "a zeroed save must not produce a free objective");
        }

        // --- claiming ---------------------------------------------------------

        [Test]
        public void CannotClaim_WhileShortOfTheCost()
        {
            Initialized(objective: 1, points: 9);

            Assert.IsFalse(_objectives.CanClaim);
        }

        [Test]
        public void CanClaim_OnExactlyTheCost()
        {
            Initialized(objective: 1, points: 10);

            Assert.IsTrue(_objectives.CanClaim);
        }

        [Test]
        public void Claiming_SpendsTheCostAndAdvances()
        {
            Initialized(objective: 1, points: 25);

            bool claimed = _objectives.TryClaim();

            Assert.IsTrue(claimed);
            Assert.AreEqual(2, _objectives.Current);
            Assert.AreEqual(15, _currency.Points, "25 minus the objective's cost of 10");
        }

        [Test]
        public void Claiming_AwardsAnUpgradePoint()
        {
            Initialized(objective: 1, points: 10);

            _objectives.TryClaim();

            Assert.AreEqual(ObjectiveService.UpgradePointsPerClaim, _currency.UpgradePoints);
        }

        [Test]
        public void ClaimingWhileShort_ChangesNothingAtAll()
        {
            Initialized(objective: 1, points: 9);

            bool claimed = _objectives.TryClaim();

            Assert.IsFalse(claimed);
            Assert.AreEqual(1, _objectives.Current);
            Assert.AreEqual(9, _currency.Points, "a refused claim must not deduct");
            Assert.AreEqual(0, _currency.UpgradePoints, "nor award");
        }

        [Test]
        public void Claiming_RaisesTheChangeEvent()
        {
            Initialized(objective: 3, points: 100);
            int raisedWith = 0;
            _objectives.CurrentChanged += value => raisedWith = value;

            _objectives.TryClaim();

            Assert.AreEqual(4, raisedWith);
        }

        [Test]
        public void Claiming_WritesImmediately()
        {
            Initialized(objective: 1, points: 10);
            int before = _data.SaveCount;

            _objectives.TryClaim();

            Assert.AreEqual(before + 1, _data.SaveCount, "the player just paid for this");
        }

        [Test]
        public void Claiming_PersistsTheNewObjective()
        {
            Initialized(objective: 1, points: 10);

            _objectives.TryClaim();

            Assert.AreEqual(2, _save.Data.currentObjective);
        }

        [Test]
        public void ClaimingRepeatedly_WalksUpTheCurve()
        {
            Initialized(objective: 1, points: 1000);

            Assert.IsTrue(_objectives.TryClaim(), "objective 1 costs 10");
            Assert.IsTrue(_objectives.TryClaim(), "objective 2 costs 20");
            Assert.IsTrue(_objectives.TryClaim(), "objective 3 costs 30");

            Assert.AreEqual(4, _objectives.Current);
            Assert.AreEqual(1000 - 10 - 20 - 30, _currency.Points);
        }

        // --- lifecycle --------------------------------------------------------

        [Test]
        public void ResettingTheSave_ReturnsToTheFirstObjective()
        {
            Initialized(objective: 12, points: 5000);

            _save.ResetToNewGame();

            Assert.AreEqual(1, _objectives.Current);
        }

        [Test]
        public void Disposing_StopsListeningToTheSave()
        {
            Initialized(objective: 12);

            _objectives.DisposeService();
            _save.ResetToNewGame();

            Assert.AreEqual(12, _objectives.Current, "a disposed service must not still react");
        }
    }
}
