using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Bridge.Networking.UnitTests
{
    [TestClass]
    public class ProtocolTests
    {
        [TestMethod, DeploymentItem("TestData\\rb12maan.pbn")]
        public async Task ProtocolHelper_Translate_Deal()
        {
            var target = await PbnHelper.LoadFile("rb12maan.pbn");
            var cards = ProtocolHelper.Translate(Seats.West, target.Boards[1].Distribution);
            Assert.AreEqual("'s cards : S A 9 6 3. H -. D A Q T 9 8 7 3. C K 5.", cards, "boards");   // Q-Plus & Meadowlark expect a space between suits
        }

        [TestMethod]
        public void ProtocolHelper_Translate_Bid_Alerted_Manual()
        {
            var bid = ProtocolHelper.Translate(Bid.C("1S!S5"), Seats.West, false, AlertMode.Manual);
            Assert.AreEqual("West bids 1S Alert. S5", bid); 
        }

        [TestMethod]
        public void ProtocolHelper_Translate_Bid_Explained_Manual()
        {
            var bid = ProtocolHelper.Translate(Bid.C("1S?S5"), Seats.West, false, AlertMode.SelfExplaining);
            Assert.AreEqual("West bids 1S Infos. S5", bid);
        }

        [TestMethod]
        public void ProtocolHelper_Translate_Bid_Alerted_None()
        {
            var bid = ProtocolHelper.Translate(Bid.C("1S!S5"), Seats.West, false, AlertMode.None);
            Assert.AreEqual("West bids 1S", bid);
        }

        [TestMethod]
        public void ProtocolHelper_Translate_Bid_Alerted_Partner()
        {
            var bid = ProtocolHelper.Translate(Bid.C("1S!S5"), Seats.West, true, AlertMode.Manual);
            Assert.AreEqual("West bids 1S", bid);
        }
    }
}
