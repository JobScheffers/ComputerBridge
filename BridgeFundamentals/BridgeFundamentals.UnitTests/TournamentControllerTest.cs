using Bridge.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Bridge.Test
{
    [TestClass]
    public class TournamentControllerTest : BridgeTestBase
    {
        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            BridgeTestBase.ClassInitialize(testContext);
        }

        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public async Task TournamentController_Run()
        {
            Log.Level = 4;
            var t = await TournamentLoader.LoadAsync(File.OpenRead("WC2005final01.pbn"));
            var c = new TournamentController(t, new ParticipantInfo() { PlayerNames = new Participant("North", "East", "South", "West"), ConventionCardNS = "RoboBridge", ConventionCardWE = "RoboBridge", UserId = Guid.NewGuid() }, BridgeEventBus.MainEventBus);
            var r = new SeatCollection<BridgeRobot>(new BridgeRobot[] { new TestRobot(Seats.North, BridgeEventBus.MainEventBus), new TestRobot(Seats.East, BridgeEventBus.MainEventBus), new TestRobot(Seats.South, BridgeEventBus.MainEventBus), new TestRobot(Seats.West, BridgeEventBus.MainEventBus) });
            await c.StartTournamentAsync();
            Assert.AreEqual<int>(3, t.Boards[0].Results.Count);
            Assert.AreEqual<int>(5, t.Boards[0].Results[0].Contract.Bid.Hoogte);
            Assert.IsTrue(t.Boards[0].Results[0].Play.PlayEnded);
            Assert.IsFalse(t.Boards[0].Results[2].Auction.Bids[0].IsPass);      // opening
            Assert.IsFalse(t.Boards[0].Results[2].Auction.Bids[1].IsPass);      // overcall
        }

        [TestMethod, DeploymentItem("TestData\\uBidParscore.pbn")]
        public async Task TournamentController_BidContest()
        {
            Log.Level = 5;
            var t = await TournamentLoader.LoadAsync(File.OpenRead("uBidParscore.pbn"));
            var c = new TournamentController(t, new ParticipantInfo() { PlayerNames = new Participant("North", "East", "South", "West"), ConventionCardNS = "RoboBridge", ConventionCardWE = "RoboBridge", UserId = Guid.NewGuid() }, BridgeEventBus.MainEventBus);
            var r = new SeatCollection<BridgeRobot>(new BridgeRobot[] { new TestRobot(Seats.North, BridgeEventBus.MainEventBus), new TestRobot(Seats.East, BridgeEventBus.MainEventBus), new TestRobot(Seats.South, BridgeEventBus.MainEventBus), new TestRobot(Seats.West, BridgeEventBus.MainEventBus) });
            await c.StartTournamentAsync();
            Assert.AreEqual<int>(1, t.Boards[0].Results.Count);
            Assert.AreEqual<int>(2, t.Boards[0].Results[0].Contract.Bid.Hoogte);
            Assert.IsFalse(t.Boards[0].Results[0].Play.PlayEnded);
            var whoseTurn = t.Boards[0].Dealer;
            foreach (var bid in t.Boards[0].Results[0].Auction.Bids)
            {
                if (!whoseTurn.IsSameDirection(t.Boards[0].Results[0].Auction.Opener))
                {
                    Assert.IsTrue(bid.IsPass, "no overcalls");
                }

                whoseTurn = whoseTurn.Next();
            }
        }

        [TestMethod]
        public async Task TournamentController_NoBoards()
        {
            Log.Level = 5;
            var t = new NoBoardsTournament();
            var c = new TournamentController(t, new ParticipantInfo() { PlayerNames = new Participant("North", "East", "South", "West"), ConventionCardNS = "RoboBridge", ConventionCardWE = "RoboBridge", UserId = Guid.NewGuid() }, BridgeEventBus.MainEventBus);
            var r = new SeatCollection<BridgeRobot>(new BridgeRobot[] { new TestRobot(Seats.North, BridgeEventBus.MainEventBus), new TestRobot(Seats.East, BridgeEventBus.MainEventBus), new TestRobot(Seats.South, BridgeEventBus.MainEventBus), new TestRobot(Seats.West, BridgeEventBus.MainEventBus) });
            await c.StartTournamentAsync();
            Assert.AreEqual<int>(0, t.Boards.Count);
        }

        private class NoBoardsTournament : Tournament
        {

            public NoBoardsTournament() : base()
            {
                this.ScoringMethod = Scorings.scPairs;
            }

            public override async Task<Board2> GetNextBoardAsync(int relativeBoardNumber, Guid userId)
            {
                Log.Trace(2, "NoBoardsTournament.GetNextBoardAsync: relativeBoardNumber:{0} userId={1}", relativeBoardNumber, userId);
                return null;
            }

            public override Task SaveAsync(BoardResult result)
            {
                throw new NotImplementedException();
            }
        }
    }
}
