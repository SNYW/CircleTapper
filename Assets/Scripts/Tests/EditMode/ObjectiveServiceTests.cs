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

        private ObjectiveService Initialized(int objective = 1, long points = 0, ObjectiveSet set = null)
        {
            GameData save = FakeDataService.ValidSave(points);
            save.currentObjective = objective;
            _data.Primary = save;

            _save = new SaveService(_data);
            _save.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            _currency = new CurrencyService(_save);
            _currency.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            // Never null: a null set falls back to the shipped asset, which would make these
            // tests depend on whatever the authored objective list happens to say today.
            _objectives = new ObjectiveService(_save, _currency, set ?? ObjectiveSet.Create());
            _objectives.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            return _objectives;
        }

        private static ObjectiveStep Task(ObjectiveGoal goal, int target = 1,
            BoardObjectType type = BoardObjectType.Circle, string label = "Do the thing")
            => new() { label = label, goal = goal, target = target, objectType = type };

        // --- the open-ended curve, once the authored objectives run out ---------

        [Test]
        public void TheOpenEndedCurve_StartsAtTheBaseAndCompounds()
        {
            Assert.AreEqual(1000, ObjectiveService.CostOf(1), "the first one past the tutorial");
            Assert.AreEqual(1600, ObjectiveService.CostOf(2));
            Assert.AreEqual(2560, ObjectiveService.CostOf(3));
            Assert.Greater(ObjectiveService.CostOf(10), ObjectiveService.CostOf(9), "it keeps climbing");
        }

        [Test]
        public void TheCurveNeverRestartsCheapAfterTheTutorial()
        {
            var set = ObjectiveSet.Create(
                Task(ObjectiveGoal.Tap),
                Task(ObjectiveGoal.Buy, 1, BoardObjectType.Circle));

            // Objective 3 is the first past the authored pair, so it must be the base cost and
            // not a near-free restart the player has already blown past.
            Initialized(objective: 3, set: set);

            Assert.AreEqual(1000, _objectives.Target);
            Assert.AreEqual("Next Upgrade", _objectives.Label);
        }

        [Test]
        public void WithNoAuthoredObjectives_ItIsEarnPointsOnTheCurve()
        {
            Initialized(objective: 1);

            Assert.AreEqual(1000, _objectives.Target);
            Assert.AreEqual("Next Upgrade", _objectives.Label);
        }

        // --- unlocking ----------------------------------------------------------

        [Test]
        public void AnObjectIsLockedUntilTheTutorialIntroducesIt()
        {
            var set = ObjectiveSet.Create(
                Task(ObjectiveGoal.Tap),
                Task(ObjectiveGoal.Buy, 1, BoardObjectType.Circle),
                Task(ObjectiveGoal.Buy, 1, BoardObjectType.Square));

            Initialized(objective: 1, set: set);
            Assert.IsFalse(_objectives.IsUnlocked(BoardObjectType.Circle), "not asked for yet");
            Assert.IsFalse(_objectives.IsUnlocked(BoardObjectType.Square));

            Initialized(objective: 2, set: set);
            Assert.IsTrue(_objectives.IsUnlocked(BoardObjectType.Circle), "unlocked as it is asked for");
            Assert.IsFalse(_objectives.IsUnlocked(BoardObjectType.Square), "still two steps away");

            Initialized(objective: 3, set: set);
            Assert.IsTrue(_objectives.IsUnlocked(BoardObjectType.Square));
        }

        [Test]
        public void SomethingTheTutorialNeverAsksFor_IsNeverLocked()
        {
            Initialized(objective: 1, set: ObjectiveSet.Create(Task(ObjectiveGoal.Tap)));

            Assert.IsTrue(_objectives.IsUnlocked(BoardObjectType.Hex));
        }

        [Test]
        public void PastTheTutorial_EverythingIsUnlocked()
        {
            var set = ObjectiveSet.Create(Task(ObjectiveGoal.Buy, 1, BoardObjectType.Hex));

            Initialized(objective: 5, set: set);

            Assert.IsTrue(_objectives.IsUnlocked(BoardObjectType.Hex));
        }

        [Test]
        public void EarnPoints_ProgressIsTheCurrency()
        {
            Initialized(objective: 1, points: 7);

            Assert.AreEqual(7, _objectives.Progress);
            Assert.IsFalse(_objectives.CanClaim);
        }

        [Test]
        public void ClaimingEarnPoints_SpendsAndAdvances()
        {
            Initialized(objective: 1, points: 2500);

            Assert.IsTrue(_objectives.TryClaim());
            Assert.AreEqual(2, _objectives.Current);
            Assert.AreEqual(1500, _currency.Points, "2500 minus the base cost of 1000");
            Assert.AreEqual(ObjectiveService.UpgradePointsPerClaim, _currency.UpgradePoints);
        }

        [Test]
        public void ClaimingWhileShort_ChangesNothing()
        {
            Initialized(objective: 1, points: 999);

            Assert.IsFalse(_objectives.TryClaim());
            Assert.AreEqual(1, _objectives.Current);
            Assert.AreEqual(999, _currency.Points);
            Assert.AreEqual(0, _currency.UpgradePoints);
        }

        // --- authored task objectives ------------------------------------------

        [Test]
        public void ATaskObjective_ShowsItsOwnLabelAndTarget()
        {
            Initialized(set: ObjectiveSet.Create(Task(ObjectiveGoal.Tap, 3, label: "Tap a circle")));

            Assert.AreEqual("Tap a circle", _objectives.Label);
            Assert.AreEqual(3, _objectives.Target);
            Assert.AreEqual(0, _objectives.Progress);
        }

        [Test]
        public void ReportingTheRightAction_MakesProgress()
        {
            Initialized(set: ObjectiveSet.Create(Task(ObjectiveGoal.Tap, 2)));

            _objectives.Report(ObjectiveGoal.Tap);

            Assert.AreEqual(1, _objectives.Progress);
            Assert.IsFalse(_objectives.CanClaim);

            _objectives.Report(ObjectiveGoal.Tap);

            Assert.IsTrue(_objectives.CanClaim);
        }

        [Test]
        public void ReportingSomethingElse_IsIgnored()
        {
            Initialized(set: ObjectiveSet.Create(Task(ObjectiveGoal.Tap)));

            _objectives.Report(ObjectiveGoal.Buy, BoardObjectType.Circle);
            _objectives.Report(ObjectiveGoal.Merge, BoardObjectType.Circle);

            Assert.AreEqual(0, _objectives.Progress);
        }

        [Test]
        public void BuyingTheWrongKind_DoesNotCount()
        {
            Initialized(set: ObjectiveSet.Create(
                Task(ObjectiveGoal.Buy, 1, BoardObjectType.Square, "Buy a square")));

            _objectives.Report(ObjectiveGoal.Buy, BoardObjectType.Circle);
            Assert.AreEqual(0, _objectives.Progress, "a circle is not a square");

            _objectives.Report(ObjectiveGoal.Buy, BoardObjectType.Square);
            Assert.AreEqual(1, _objectives.Progress);
        }

        [Test]
        public void TappingIgnoresTheObjectKind()
        {
            Initialized(set: ObjectiveSet.Create(Task(ObjectiveGoal.Tap)));

            _objectives.Report(ObjectiveGoal.Tap, BoardObjectType.Hex);

            Assert.AreEqual(1, _objectives.Progress, "tapping is not about a particular object");
        }

        [Test]
        public void ProgressNeverRunsPastTheTarget()
        {
            Initialized(set: ObjectiveSet.Create(Task(ObjectiveGoal.Tap, 2)));

            for (int i = 0; i < 10; i++) _objectives.Report(ObjectiveGoal.Tap);

            Assert.AreEqual(2, _objectives.Progress);
        }

        [Test]
        public void ATaskObjectiveIsFree()
        {
            Initialized(points: 500, set: ObjectiveSet.Create(Task(ObjectiveGoal.Tap)));

            _objectives.Report(ObjectiveGoal.Tap);

            Assert.IsTrue(_objectives.TryClaim());
            Assert.AreEqual(500, _currency.Points, "doing the thing was the price");
            Assert.AreEqual(ObjectiveService.UpgradePointsPerClaim, _currency.UpgradePoints);
        }

        [Test]
        public void AnUnfinishedTask_CannotBeClaimed()
        {
            Initialized(set: ObjectiveSet.Create(Task(ObjectiveGoal.Tap, 2)));
            _objectives.Report(ObjectiveGoal.Tap);

            Assert.IsFalse(_objectives.TryClaim());
            Assert.AreEqual(1, _objectives.Current);
        }

        [Test]
        public void ClaimingResetsProgressForTheNextObjective()
        {
            Initialized(set: ObjectiveSet.Create(
                Task(ObjectiveGoal.Tap),
                Task(ObjectiveGoal.Buy, 2, BoardObjectType.Circle)));

            _objectives.Report(ObjectiveGoal.Tap);
            _objectives.TryClaim();

            Assert.AreEqual(2, _objectives.Current);
            Assert.AreEqual(0, _objectives.Progress, "the next objective starts from nothing");
            Assert.AreEqual(2, _objectives.Target);
        }

        [Test]
        public void RunningOutOfAuthoredObjectives_FallsBackToTheCurve()
        {
            Initialized(points: 5000, set: ObjectiveSet.Create(Task(ObjectiveGoal.Tap)));

            _objectives.Report(ObjectiveGoal.Tap);
            _objectives.TryClaim();

            Assert.AreEqual("Next Upgrade", _objectives.Label);
            Assert.AreEqual(ObjectiveService.CostOf(1), _objectives.Target, "first past the set");
        }

        // --- persistence --------------------------------------------------------

        [Test]
        public void PartialProgress_SurvivesIntoTheSave()
        {
            Initialized(set: ObjectiveSet.Create(Task(ObjectiveGoal.Tap, 3)));

            _objectives.Report(ObjectiveGoal.Tap);
            _objectives.Report(ObjectiveGoal.Tap);

            Assert.AreEqual(2, _save.Data.objectiveProgress, "quitting mid-objective must not lose it");
        }

        [Test]
        public void ProgressIsReadBackFromTheSave()
        {
            GameData existing = FakeDataService.ValidSave();
            existing.currentObjective = 1;
            existing.objectiveProgress = 2;
            _data.Primary = existing;

            _save = new SaveService(_data);
            _save.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            _currency = new CurrencyService(_save);
            _currency.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            _objectives = new ObjectiveService(_save, _currency,
                ObjectiveSet.Create(Task(ObjectiveGoal.Tap, 3)));
            _objectives.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(2, _objectives.Progress);
        }

        [Test]
        public void ResettingTheSave_ReturnsToTheFirstObjectiveWithNoProgress()
        {
            Initialized(objective: 12, points: 5000, set: ObjectiveSet.Create(Task(ObjectiveGoal.Tap, 3)));
            _objectives.Report(ObjectiveGoal.Tap);

            _save.ResetToNewGame();

            Assert.AreEqual(1, _objectives.Current);
            Assert.AreEqual(0, _objectives.Progress);
        }

        [Test]
        public void Disposing_StopsListeningToTheSave()
        {
            Initialized(objective: 12);

            _objectives.DisposeService();
            _save.ResetToNewGame();

            Assert.AreEqual(12, _objectives.Current);
        }
    }
}
