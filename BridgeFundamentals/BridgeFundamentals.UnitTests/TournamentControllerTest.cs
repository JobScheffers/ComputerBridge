using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sodes.Bridge.Base;
using Sodes.Bridge.Base.Test.Helpers;
using System;
using System.Threading;

namespace BridgeFundamentals.UnitTests
{
    [TestClass]
    public class TournamentControllerTest : BridgeTestBase
    {
        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            BridgeTestBase.Initialize(testContext);
        }

        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public void TournamentController_Run()
        {
            var t = TournamentTest.TournamentLoad("WC2005final01.pbn");
            var c = new TournamentController(t, new ParticipantInfo() { PlayerNames = new Participant("North", "East", "South", "West"), ConventionCardNS = "RoboBridge", ConventionCardWE = "RoboBridge", UserId = Guid.NewGuid() });
            var r = new SeatCollection<BridgeRobot>(new BridgeRobot[] { new TestRobot(Seats.North), new TestRobot(Seats.East), new TestRobot(Seats.South), new TestRobot(Seats.West) });
            var loop = true;
            c.StartTournament(() => { loop = false; }).Wait();
            while (loop) Thread.Sleep(1000);
        }
}
}
