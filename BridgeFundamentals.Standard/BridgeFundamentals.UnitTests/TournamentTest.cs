using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System;
using System.Net;
using Bridge.Test.Helpers;
using System.Threading.Tasks;

namespace Bridge.Test
{
    [TestClass]
    public class TournamentTest : TestBase
    {
        [TestMethod, TestCategory("CI"), TestCategory("Other")]
        public void Tournament_Load_FromString()
        {
            var t = Pbn2Tournament.Load(@"
[Event ""0x9999990""] 
[Board ""1""]   
[Dealer ""S""]
[Vulnerable ""None""]
[Deal ""N:643.KQ75.73.K973 J752.J6.QT862.52 AK8.T8.KJ954.JT4 QT9.A9432.A.AQ86""]
[Auction ""S""]
1D X 1NT Pass Pass 3H X Pass Pass Pass
[Contract ""3HX""]
[Play ""N""]
D7 D8 D9 DA S3 S2 SK S9 S6 S5 SA SQ HQ H6 HT H2 D3 D6 DJ H3 S4 S7 S8 ST HK HJ H8 H4 H7 C2 D4 HA
");
            Assert.AreEqual<int>(1, t.Boards[0].Results.Count);
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\uBidParscore.pbn")]
        public async Task Tournament_Load_uBid()
        {
            var originalTournament = await TournamentLoader.LoadAsync(File.OpenRead("uBidParscore.pbn"));
            Assert.IsFalse(originalTournament.AllowOvercalls, "OvercallsAllowed");
            Pbn2Tournament.Save(originalTournament, File.Create("t1.pbn"));
            var newFile = await File.OpenText("t1.pbn").ReadToEndAsync();
            Assert.IsTrue(newFile.Contains("DoubleDummyTricks"), "DoubleDummyTricks");
            //Assert.IsTrue(newFile.Contains("OptimumResultTable"), "OptimumResultTable");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\TDJ240516.01 3NT.pbn")]
        public void Tournament_Load_BridgEZ()
        {
            // should not crash
            var target = TournamentLoad("TDJ240516.01 3NT.pbn");
            Assert.IsTrue(target.AllowOvercalls, "OvercallsAllowed");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\WC2005final01.pbn")]
        public void Tournament_SavePbn()
        {
            var original = TournamentLoad("WC2005final01.pbn");
            var allPass = new BoardResult("", original.Boards[0], new Participant("test1", "test1", "test1", "test1"));
            allPass.Auction.Record(Bid.C("p"));
            allPass.Auction.Record(Bid.C("p"));
            allPass.Auction.Record(Bid.C("p"));
            allPass.Auction.Record(Bid.C("p"));
            original.Boards[0].Results.Add(allPass);
            var partialPlay = new BoardResult("", original.Boards[0], new Participant("test2", "test2", "test2", "test2"));
            partialPlay.HandleBidDone(Seats.North, Bid.C("1S"));
            partialPlay.HandleBidDone(Seats.East, Bid.C("p"));
            partialPlay.HandleBidDone(Seats.South, Bid.C("p"));
            partialPlay.HandleBidDone(Seats.West, Bid.C("p"));
            partialPlay.HandleCardPlayed(Seats.East, Suits.Hearts, Ranks.King);
            partialPlay.HandleCardPlayed(Seats.South, Suits.Hearts, Ranks.Two);
            partialPlay.HandleCardPlayed(Seats.West, Suits.Hearts, Ranks.Three);
            partialPlay.HandleCardPlayed(Seats.North, Suits.Hearts, Ranks.Ace);
            partialPlay.HandleCardPlayed(Seats.North, Suits.Spades, Ranks.Ace);
            original.Boards[0].Results.Add(partialPlay);
            var partialAuction = new BoardResult("", original.Boards[0], new Participant("test3", "test3", "test3", "test3"));
            partialAuction.Auction.Record(Bid.C("1S"));
            partialAuction.Auction.Record(Bid.C("p"));
            partialAuction.Auction.Record(Bid.C("p"));
            original.Boards[0].Results.Add(partialAuction);
            Pbn2Tournament.Save(original, File.Create("t2.pbn"));
            var copy = TournamentLoad("t2.pbn");
            Assert.AreEqual(original.EventName, copy.EventName, "EventName");
            Assert.AreEqual<DateTime>(original.Created, copy.Created, "Created");
            Assert.AreEqual<int>(original.Boards.Count, copy.Boards.Count, "Boards.Count");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\WC2005final01.pbn")]
        public void Tournament_Load_WC2005final01pbn()
        {
            Tournament target = TournamentLoad("WC2005final01.pbn");
            Assert.IsTrue(target.GetNextBoard(1, Guid.Empty).Results[0].Play.AllCards.Count > 0, "pbn: No played cards");
            //TournamentLoader.Save("WC2005final01.trn", target);
            //target = TournamentLoad("WC2005final01.trn");
            //Assert.IsTrue(target.Boards.Count == 16, "No 16 boards");
            //Assert.IsTrue(target.GetBoard(1, false).Distribution.Owns(Seats.North, Suits.Spades, Ranks.Ace));
            //Assert.IsTrue(target.GetBoard(1, false).Results.Count == 2, "Board 1 does not have 2 results");
            ////Assert.IsTrue(target.GetBoard(1, false).Results[0].Participants.Count == 4, "No 4 participants");
            //Assert.IsTrue(target.GetBoard(1, false).Results[0].Auction != null, "No auction");
            //Assert.IsTrue(target.GetBoard(1, false).Results[0].Play != null, "No play");
            //Assert.IsTrue(target.GetBoard(1, false).Results[0].Play.AllCards.Count > 0, "No played cards");
        }

        [TestMethod]
        public void Tournament_Load_Http()
        {
            Tournament target = TournamentLoad("http://bridge.nl/groepen/Wedstrijdzaken/1011/Ruitenboer/RB11_maandag.pbn");
        }

        public static Tournament TournamentLoad(string fileName)
        {
            if (fileName.StartsWith("http://"))
            {
                var url = new Uri(fileName);
                var req = WebRequest.Create(url);
                var resp = req.GetResponse();
                var stream = resp.GetResponseStream();
                return TournamentLoader.LoadAsync(stream).Result;
            }
            else
            {
                return TournamentLoader.LoadAsync(File.OpenRead(fileName)).Result;
            }
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\BC Ruit.pbn")]
        public void Tournament_Load_BCRuit()
        {
            var target = TournamentLoad("BC Ruit.pbn");
            Assert.AreEqual<int>(24, target.Boards.Count, "boards");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\PBN00201- Baron25 v RoboBridge.pbn"), DeploymentItem("TestData\\PBN00201- MicroBridge v WBridg5.pbn")]
        public void Tournament_Merge()
        {
            var t1 = TournamentLoad("PBN00201- Baron25 v RoboBridge.pbn");
            var t2 = TournamentLoad("PBN00201- MicroBridge v WBridg5.pbn");
            t1.AddResults(t2);
            Assert.AreEqual<int>(16, t1.Boards.Count, "boards");
            Assert.AreEqual<int>(4, t1.Boards[0].Results.Count, "results");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\PBN00201- Baron25 v RoboBridge.pbn")]
        public void Tournament_Load_BridgeMoniteur2014()
        {
            var target = TournamentLoad("PBN00201- Baron25 v RoboBridge.pbn");
            Assert.AreEqual<int>(16, target.Boards.Count, "boards");
            Assert.AreEqual<int>(2, target.Boards[0].Results.Count, "results");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\academy_20140812_2.pbn")]
        public void Tournament_Load_Acadamy()
        {
            var target = TournamentLoad("academy_20140812_2.pbn");
            Assert.AreEqual<int>(24, target.Boards.Count, "boards");
            Assert.AreEqual<int>(8, target.Boards[0].Results.Count, "results");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\DealSet01203.pbn")]
        public void Tournament_Load_LevendaalDealSet()
        {
            var target = TournamentLoad("DealSet01203.pbn");
            Assert.AreEqual<int>(37, target.Boards.Count, "boards");
            Assert.AreEqual<int>(0, target.Boards[0].Results.Count, "results");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\Ruitenboer2014FinaleZitting1.pbn")]
        public void Tournament_Load_Ruitenboer2014()
        {
            var target = TournamentLoad("Ruitenboer2014FinaleZitting1.pbn");
            Assert.AreEqual<int>(20, target.Boards.Count, "boards");
            Assert.AreEqual<int>(11, target.Boards[0].Results.Count, "results");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\ledbury_20131120_1.pbn")]
        public void Tournament_Load_BridgeWebsPbn()
        {
            var target = TournamentLoad("ledbury_20131120_1.pbn");
            Assert.AreEqual<int>(26, target.Boards.Count, "boards");
            Assert.AreEqual<int>(10, target.Boards[0].Results.Count, "results");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\eb42.pbn")]
        public void Tournament_Load_EasyBridge402pbn()
        {
            Tournament target = TournamentLoad("eb42.pbn");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\Cap98bu1.pbn")]
        public void Tournament_Load_Cap98bu1pbn()
        {
            var target = TournamentLoad("Cap98bu1.pbn");
            //TournamentLoader.Save("Cap98bu1.trn", target);
            //target = TournamentLoad("Cap98bu1.trn");
            //Assert.IsTrue(target.Boards.Count == 16, "No 16 boards");
            //Assert.IsTrue(target.GetBoard(1).Distribution.Owns(Seats.North, Suits.Spades, Ranks.Ace));
            //Assert.IsTrue(target.GetBoard(1).Results.Count == 2, "Board 1 does not have 2 results");
            //Assert.IsTrue(target.GetBoard(1).Results[0].Participants.Count == 4, "No 4 participants");
            //Assert.IsTrue(target.GetBoard(1).Results[0].Auction != null, "No auction");
            //Assert.IsTrue(target.GetBoard(1).Results[0].Play != null, "No play");
            //Assert.IsTrue(target.GetBoard(1).Results[0].Play.AllCards.Count > 0, "No played cards");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\Cap98bu3.pbn")]
        public void Tournament_Load_Cap98bu3pbn()
        {
            var target = TournamentLoad("Cap98bu3.pbn");
            Assert.AreEqual("North's bold (that's the polite description!)", target.GetNextBoard(704, Guid.Empty).Results[0].Auction.Bids[1].HumanExplanation.Substring(0, 45));
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\OKBridge_imp_01.pbn")]
        public void Tournament_Load_Pbn20()
        {
            Tournament target = TournamentLoad("OKBridge_imp_01.pbn");
            Assert.AreEqual<int>(193, target.Boards.Count, "boards");
            Assert.AreEqual<int>(27, target.Boards[0].Results.Count, "results for 1st board");
            Assert.AreEqual<int>(150, target.Boards[0].Results[1].NorthSouthScore, "NS score");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\Contract c404.pbn")]
        public void Tournament_Load_Contract4H04()
        {
            var target = TournamentLoad("Contract c404.pbn");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\Contract Hoofdstuk 2.pbn")]
        public void Tournament_Load_Contract1H02()
        {
            var target = TournamentLoad("Contract Hoofdstuk 2.pbn");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\Contract1H12.pbn")]
        public void Tournament_Load_Contract1H12()
        {
            var target = TournamentLoad("Contract1H12.pbn");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\EasyBridge.6610slam.pbn")]
        public void Tournament_Load_EasyBridgePBN()
        {
            Tournament target = TournamentLoad("EasyBridge.6610slam.pbn");
            //Assert.AreEqual<int>(193, target.Boards.Count, "boards");
            //Assert.AreEqual<int>(112, target.Boards[0].Results.Count, "results for 1st board");
            //Assert.AreEqual<int>(150, target.Boards[0].Results[0].NorthSouthScore, "NS score");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\NBC.20090109.b06.pbn")]
        public void Tournament_Load_NBC_PBN()
        {
            Tournament target = TournamentLoad("NBC.20090109.b06.pbn");
            //Assert.AreEqual<int>(193, target.Boards.Count, "boards");
            //Assert.AreEqual<int>(112, target.Boards[0].Results.Count, "results for 1st board");
            //Assert.AreEqual<int>(150, target.Boards[0].Results[0].NorthSouthScore, "NS score");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\RB11_maandag.pbn")]
        public void Tournament_Load_RB2011()
        {
            Tournament target = TournamentLoad("RB11_maandag.pbn");
            Assert.AreEqual<int>(28, target.Boards.Count, "boards");
            Assert.AreEqual<int>(0, target.Boards[0].Results.Count, "results for 1st board");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\RB12maan.pbn")]
        public void Tournament_Load_RB2012()
        {
            Tournament target = TournamentLoad("RB12maan.pbn");
            Assert.AreEqual<int>(28, target.Boards.Count, "boards");
            Assert.AreEqual<int>(0, target.Boards[0].Results.Count, "results for 1st board");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\Samtronix.pbn")]
        public void Tournament_Load_Samtronix()
        {
            Tournament target = TournamentLoad("Samtronix.pbn");
        }
    }
}
