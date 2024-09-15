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
        public void ProtocolHelper_Parse_Cards()
        {
            ProtocolHelper.Parse("East's cards : S A 9 6 3. H -. D A Q T 9 8 7 3. C K 5.", out var owner, out var cards);
            Assert.AreEqual(Seats.East, owner);
            Assert.AreEqual(13, cards.Count);
            Assert.IsTrue(cards.Contains(new SimpleMove(Suits.Spades, Ranks.Ace)));
            Assert.IsFalse(cards.Contains(new SimpleMove(Suits.Spades, Ranks.King)));
        }
    }
}
