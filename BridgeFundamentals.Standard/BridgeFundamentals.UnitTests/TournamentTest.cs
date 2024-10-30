using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System;
using Bridge.Test.Helpers;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;

namespace Bridge.Test
{
    [TestClass]
    public class TournamentTest : TestBase
    {
        [TestMethod, TestCategory("CI"), TestCategory("Other")]
        public void PbnHelper_Load_4hands()
        {
            var t1 = PbnHelper.Load(@"
[Deal ""S:9T4.95Q.QK46.76Q""]
[Deal ""N:AJ25.87K.AJ8.J38""]
[Deal ""E:KQ.AJT.T9753.AKT""]
[Deal ""W:8763.6432.2.9542""]
");
            Assert.IsTrue(t1.Boards.Count > 0);

            var t2 = PbnHelper.Load(@"
[Deal ""S:94.95Q.QK46.7Q""]
[Deal ""N:AJ5.87K.AJ8.J8""]
[Deal ""E:Q.AJT.T9753.KT""]
[Deal ""W:876.6432.2.954""]
");
            Assert.IsTrue(t2.Boards.Count > 0);
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), ExpectedException(typeof(FatalBridgeException))]
        public void Tournament_Load_Redoublet_20240208()
        {
            var t = PbnHelper.Load(@"[Dealer ""West""][Vulnerable ""None""]
[Deal ""W:5.KQT954.AT53.97""]
[Deal ""N:T82.3.KQ987.AKT3""]
[Deal ""E:AK63.AJ87.64.Q84""]
[Deal ""S:QJ974.62.J2.J652""]
[Auction ""W""]
2H Pass 2NT Pass 4H Pass Pass Pass 
[Play ""-""]");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other")]
        public void Tournament_Load_Redoublet_20240101()
        {
            var t = PbnHelper.Load(@"            [Dealer ""South""]
            [Vulnerable ""Both""]
            [Deal ""S:A965.A.AQ9432.J9 J84.K94.J75.K654 KQ732.QT872..AT8 T.J653.KT86.Q732""]
            [Auction ""South""]
            6S Pass          
");
        }

        /*
            [Dealer "South"]
            [Vulnerable "Both"]
            [Deal "S:A965.A.AQ9432.J9 J84.K94.J75.K654 KQ732.QT872..AT8 T.J653.KT86.Q732"]
            [Auction "South"]
            6S Pass          
         
         */
        [TestMethod, TestCategory("CI"), TestCategory("Other")]
        public void Tournament_Load_FromString()
        {
            var t = PbnHelper.Load(@"
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

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\Thorvald 2.pbn")]
        public async Task Tournament_Load_BugReport_Thorvald1()
        {
            using var stream = File.OpenRead("Thorvald 2.pbn");
            var originalTournament = await PbnHelper.Load(stream);
            Assert.AreEqual("Open", originalTournament.Boards[0].Results[0].Room);
            Assert.AreEqual(23, originalTournament.Boards[0].Results[0].Created.Day);
            Assert.AreEqual(27, originalTournament.Boards[0].Results[0].Created.Minute);
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\21211444342275260735140.pbn")]
        public async Task Tournament_Load_BugReport_21211444342275260735140()
        {
            using var stream = File.OpenRead("21211444342275260735140.pbn");
            var originalTournament = await PbnHelper.Load(stream);
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\Bjorn Hjalmarsson Board 49-64.pbn")]
        public async Task Tournament_Load_Bjorn_Hjalmarsson_Board_49_64()
        {
            using var stream = File.OpenRead("Bjorn Hjalmarsson Board 49-64.pbn");
            var originalTournament = await PbnHelper.Load(stream);
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\uBidParscore.pbn")]
        public async Task Tournament_Load_uBid()
        {
            Tournament originalTournament;
            using var stream1 = File.OpenRead("uBidParscore.pbn");
            originalTournament = await PbnHelper.Load(stream1);
            Assert.IsFalse(originalTournament.AllowOvercalls, "OvercallsAllowed");

            using var stream2 = File.Create("t1.pbn");
            PbnHelper.Save(originalTournament, stream2);

            using var stream3 = File.OpenText("t1.pbn");
            var newFile = await stream3.ReadToEndAsync();
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
            using (var stream = File.Create("t2.pbn"))
            {
                PbnHelper.Save(original, stream);
            }
            var copy = TournamentLoad("t2.pbn");
            Assert.AreEqual(original.EventName, copy.EventName, "EventName");
            Assert.AreEqual<DateTime>(original.Created, copy.Created, "Created");
            Assert.AreEqual<int>(original.Boards.Count, copy.Boards.Count, "Boards.Count");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\WC2005final01.pbn")]
        public void Tournament_SaveImpMatch()
        {
            var original = TournamentLoad("WC2005final01.pbn");
            original.ScoringMethod = Scorings.scCross;
            original.CalcTournamentScores();
            using (var stream = File.Create("t2.pbn"))
            {
                PbnHelper.Save(original, stream);
            }
            using (var stream = File.OpenText("t2.pbn"))
            {
                var pbn = stream.ReadToEnd();
                Trace.WriteLine(pbn);
            }
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\PBN00201- Baron25 v RoboBridge.pbn")]
        public void Tournament_SaveImpMatch2()
        {
            var original = TournamentLoad("PBN00201- Baron25 v RoboBridge.pbn");
            original.MatchInProgress = new() { Team1 = new TeamData { Name = "Baron25" }, Team2 = new TeamData { Name = "RoboBridge" },Tables = 1 };
            original.ScoringMethod = Scorings.scIMP;
            original.CalcTournamentScores();
            using (var stream = File.Create("t2.pbn"))
            {
                PbnHelper.Save(original, stream);
            }

            var clone = TournamentLoad("t2.pbn");
            Assert.AreEqual("Baron25", clone.MatchInProgress.Team1.Name);
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\WC2007RR1a.pbn")]
        public void Tournament_Load_WC2007RR1apbn()
        {
            Tournament target = TournamentLoad("WC2007RR1a.pbn");
            Assert.AreEqual(2, target.Boards.Count);
            Assert.AreEqual(0, target.Boards[0].Results.Count);
            Assert.AreEqual(0, target.Boards[1].Results.Count);
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

        public static Tournament TournamentLoad(string fileName)
        {
            Stream responseStream;
            if (fileName.StartsWith("http://"))
            {
                var url = new Uri(fileName);
                var myClient = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true });
                var response = myClient.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                responseStream = response.Content.ReadAsStream();
            }
            else
            {
                responseStream = File.OpenRead(fileName);
            }

            return PbnHelper.Load(responseStream).Result;
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

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\PBN00201- Baron25 v RoboBridge.pbn"), DeploymentItem("TestData\\PBN00201- MicroBridge v WBridg5.pbn")]
        public void Tournament_Merge_ShouldNotIntroduceDuplicates()
        {
            var t1 = TournamentLoad("PBN00201- Baron25 v RoboBridge.pbn");
            var t2 = TournamentLoad("PBN00201- Baron25 v RoboBridge.pbn");
            t1.AddResults(t2);
            Assert.AreEqual<int>(16, t1.Boards.Count, "boards");
            Assert.AreEqual<int>(2, t1.Boards[0].Results.Count, "results");
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
            var board704 = target.GetBoardAsync(704).Result;
            var firstResult = board704.Results[0];
            Assert.AreEqual("North's bold (that's the polite description!)", firstResult.Auction.Bids[1].HumanExplanation.Substring(0, 45));
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
            Assert.AreEqual<int>(-100, target.Boards[0].OptimumScoreNS.Value, "optimum score on board 1");
            Assert.AreEqual<int>(140, target.Boards[2].OptimumScoreNS.Value, "optimum score on board 3");
            Assert.AreEqual<int>(600, target.Boards[25].OptimumScoreNS.Value, "optimum score on board 26");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\Samtronix.pbn")]
        public void Tournament_Load_Samtronix()
        {
            Tournament target = TournamentLoad("Samtronix.pbn");
        }
    }
}
