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
        }

        [TestMethod, DeploymentItem("TestData\\uBidParscore.pbn")]
        public async Task TournamentController_BidContest()
        {
            Log.Level = 4;
            var t = await TournamentLoader.LoadAsync(File.OpenRead("uBidParscore.pbn"));
            var c = new TournamentController(t, new ParticipantInfo() { PlayerNames = new Participant("North", "East", "South", "West"), ConventionCardNS = "RoboBridge", ConventionCardWE = "RoboBridge", UserId = Guid.NewGuid() }, BridgeEventBus.MainEventBus);
            var r = new SeatCollection<BridgeRobot>(new BridgeRobot[] { new TestRobot(Seats.North, BridgeEventBus.MainEventBus), new TestRobot(Seats.East, BridgeEventBus.MainEventBus), new TestRobot(Seats.South, BridgeEventBus.MainEventBus), new TestRobot(Seats.West, BridgeEventBus.MainEventBus) });
            await c.StartTournamentAsync();
            Assert.AreEqual<int>(1, t.Boards[0].Results.Count);
            Assert.AreEqual<int>(2, t.Boards[0].Results[0].Contract.Bid.Hoogte);
            Assert.IsFalse(t.Boards[0].Results[0].Play.PlayEnded);
        }
    }
}
