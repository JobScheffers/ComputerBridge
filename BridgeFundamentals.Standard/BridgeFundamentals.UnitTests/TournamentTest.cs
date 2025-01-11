using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System;
using Bridge.Test.Helpers;
using System.Threading.Tasks;
using System.Net.Http;

namespace Bridge.Test
{
    [TestClass]
    public class TournamentTest : TestBase
    {
        [TestMethod, TestCategory("CI"), TestCategory("Other")]
        [DeploymentItem("TestData\\Contract c404.pbn")]
        [DeploymentItem("TestData\\Round1.2024-12-21-09-49.RoboBridge-RB2017.pbn")]
        [DeploymentItem("TestData\\Thorvald 2.pbn")]
        [DeploymentItem("TestData\\21211444342275260735140.pbn")]
        [DeploymentItem("TestData\\Bjorn Hjalmarsson Board 49-64.pbn")]
        [DeploymentItem("TestData\\TDJ240516.01 3NT.pbn")]
        [DeploymentItem("TestData\\WC2005final01.pbn")]
        [DeploymentItem("TestData\\eb42.pbn")]
        [DeploymentItem("TestData\\Cap98bu1.pbn")]
        [DeploymentItem("TestData\\Contract Hoofdstuk 2.pbn")]
        [DeploymentItem("TestData\\Contract1H12.pbn")]
        [DeploymentItem("TestData\\EasyBridge.6610slam.pbn")]
        [DeploymentItem("TestData\\NBC.20090109.b06.pbn")]
        [DeploymentItem("TestData\\Samtronix.pbn")]
        [DeploymentItem("TestData\\SingleBoard.pbn")]
        [DeploymentItem("TestData\\WC2007RR1a.pbn")]
        public async Task Tournament_Load_All()
        {
            var pbnList = Directory.EnumerateFiles(".");
            foreach (var pbnFileName in pbnList)
            {
                // can the pbn be loaded? and can scores be calculated?
                var tournament = await PbnHelper.LoadFile(pbnFileName);

                Assert.AreEqual(!pbnFileName.ToLower().EndsWith("ubidparscore.pbn"), tournament.AllowOvercalls, $"OvercallsAllowed in {pbnFileName}");
                switch (Path.GetFileNameWithoutExtension(pbnFileName))
                {
                    case "SingleBoard":
                        Assert.AreEqual(0, tournament.Boards[0].Results.Count);
                        break;
                    case "WC2007RR1a":
                        Assert.AreEqual(2, tournament.Boards.Count);
                        Assert.AreEqual(0, tournament.Boards[0].Results.Count);
                        Assert.AreEqual(0, tournament.Boards[1].Results.Count);
                        break;
                }

                // can the tournament be saved?
                using (var stream = File.Create("Saved.pbn"))
                {
                    PbnHelper.Save(tournament, stream);
                }

                // can the saved pbn be loaded?
                var tournament2 = await PbnHelper.LoadFile("Saved.pbn");
            }
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other"), DeploymentItem("TestData\\Round1.2024-12-21-09-49.RoboBridge-RB2017.pbn")]
        public async Task Tournament_Load_Round1()
        {
            var tournament = await PbnHelper.LoadFile("Round1.2024-12-21-09-49.RoboBridge-RB2017.pbn");
            Assert.IsNotNull(tournament.MatchInProgress);
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other")]
        public void PbnHelper_Load_4hands()
        {
            var t4 = PbnHelper.Load(@"
[Dealer ""E""]
[Vulnerable ""None""]
[Auction ""E""]
");
            Assert.IsTrue(t4.Boards.Count > 0);
            Assert.IsTrue(t4.Boards[0].Results.Count > 0);

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

            var t3 = PbnHelper.Load(@"
[Dealer ""E""]
[Vulnerable ""None""]
[Auction ""E""]
1C Pass
");
            Assert.IsTrue(t3.Boards.Count > 0);
            Assert.IsTrue(t3.Boards[0].Results.Count > 0);
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
            partialPlay.HandleBidDone(Seats.North, Bid.C("1S!S5"));
            partialPlay.HandleBidDone(Seats.East, Bid.C("p?(!(ps0911*pa0012*D8*o1D*!H4*!S4*r23D5))*(!(ps0911*pa0012*C8*o1C*!H4*!S4*r23C5))*(!(pS1019*pa0612*S7*o7S3*r23S4))*(!(pH1019*pa0612*H7*o7H3*r23H4))*(!(pD1019*pa0712*D7*o7D1*!H4*!S4*r23D4))*(!(pC1019*pa0712*C7*o7C1*!H4*!S4*r23C4))*(!(pS0814*pa0510*(pa0509+th3)*S7*o7S1*!H4*r23S3))*(!(pH0814*pa0512*(pa0509+th3)*H7*o7H0*!S4*r23H3))*(!(pD0714*pa0011*D6*(D7+o7D6*(!S3+!H3))*o7D1*!H4*!S4*r23D3))*(!(pC0714*pa0011*C6*(C7+o7C6*(!S3+!H3))*o7C1*!H4*!S4*r23C3))*(!(S5*!S8*!>HS*o7S1*(!u+S6*(o7S3+th3+pS1011+pa0009))*r23S2*pa0011*pS0413*(pa0010+th3)*!(pS1213*pa1021*(H5+D5+C5+S7))*!(pa1115*=S5*H3*(D4+C4))*!(pa1019*S6*(H4+D4+C4))*!(vS6*pa1240)))*(!(H5*!H8*>HS*o7H1*(!u+H6*(o7H3+th3+pH1011+pa0009))*r23H2*pa0011*pH0413*(pa0010+th3)*!(pH1213*pa1021*(S5+D5+C5+H7))*!(pa1019*H6*(S4+D4+C4))*!(vH6*pa1240)))*(!(D5*!S5*!H5*(!u+D6*o7D1*(o7D3+th3))*r23D2*pa0311*pD0413*(pa0010+th3)))*(!(pN2022*S2*!S6*H2*!H6*D1*(D2+o7D3*C3*!C6*H3*!H5*S3*!S5)*!D7*C1*(C2+o7C3*D3*!D6*H3*!H5*S3*!S5)*!C7*!(S5*(H4*!t07+D5+C5))*!(H5*(S4*!t07+D5+C5))*!(pa2240*(S5+H5))))*(!(pN1517*pa1418*t07*(t01+th4+th3)))*(!(S5*!>HS*!(pa1640*(>DS+>CS))*ps1021*(pg1350*(pg1450+pa1021)*(H5+D5+C5+!H1+!D1+!C1)+pa1010*S5*(S6+th4)*(H4+D4+C4+S7)+pa1111*(C4+D4+H4+S6+o7S9)+pa1221)*!H7*!D8*!C8*!vS9*!vH9*!vD9*!vC9*!(vS8*pa1840)*!(pa2121*pg2250*(H5+D5+C5))))*(!(H5*!(pa1640*(>DH+>CH))*>HS*ps1021*(pg1350*(pg1550+pa1021)*(S5+D5+C5+!S1+!D1+!C1)+pa1010*(H6*(D4+C4+S4+H7*vH6)*pH1150+D5+C5)+pa1111*(C4+D4+S4+H6+th4)+pa1221)*!S7*!D8*!C8*!(vH9*pa1540)*!vD9*!vC9*!(vH8*pa1840)*!(pa2121*pg2250*(H5+D5+C5))))*(!(D4*!(pa1621*(>CD))*ps1021*(pa1021*D6*(D7+H4+S4)+pa1121*t04*(D4*H4*S4*th3+D5*!(C6*pa1622)*(D6+H4+S4+C4))+pa1221*D4*!C5*(!C4+=C4*=D4*=H4)*(D5+H4+S4+pa1321+!u+th4))*!S6*(=D4*!>SD+>DS)*!H6*(=D4*!>HD+>DH)*!C7*!(vD9*o7D7*pa1840)*!(pa2121*pg2250*(H5+S5+C5))))*(!(C2*ps1021*(pa1010*C6*(H4+S4+C7)+pa1111*C5*!>DC*((H4+S4+D4)*pg1150+C6)+pa1212*C4*!>DC*(D4+H4+S4+C5)+pa1212*!C4*=H4*=S4*!D4+pa1212*C3*(=H4+=S4+C4)*!D4*!H5*!S5+pa1321*(=C4*!>DC*!>HC*!>SC+C5*>CD*>CH*>CS+!H5*!S5))*!S6*!H6*!D7*!(vC9*o7C7*pa1840)*!(D4*>DC)*!(pa2121*pg2250*(H5+D5+S5))))*(!(pa2040*human[?]))*(!(ps2037*(pa2037*!(pa2020*t01)+pa1337*(vS8*S6+vH8+vD8*o7D7+vC8*o7C7)+pa1037*(vS9*S6+vH9*H6+vD9*D6*o7D5+vC9*C6*o7C5))))*(pa0012)"));
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
            Assert.IsTrue(copy.Boards[0].Results[2].Auction.Bids[0].Alert, "alert");
            Assert.AreEqual(true, copy.Boards[0].Results[2].Auction.Bids[0].Alert, "alert");
            Assert.AreEqual("S5", copy.Boards[0].Results[2].Auction.Bids[0].Explanation, "alert");
            Assert.AreEqual(false, copy.Boards[0].Results[2].Auction.Bids[2].Alert, "alert");
            Assert.AreEqual("S5", copy.Boards[0].Results[2].Auction.Bids[0].Explanation, "alert");
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
    }
}
