using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bridge.Test
{
    [TestClass]
    public class SeatsTest
    {
        [TestMethod]
        public void Seats_Next()
        {
            Assert.AreEqual(Seats.East, Seats.North.Next());
            Assert.AreEqual(Seats.South, Seats.East.Next());
            Assert.AreEqual(Seats.West, Seats.South.Next());
            Assert.AreEqual(Seats.North, Seats.West.Next());
        }

        [TestMethod]
        public void Seats_Previous()
        {
            Assert.AreEqual(Seats.West, Seats.North.Previous());
            Assert.AreEqual(Seats.North, Seats.East.Previous());
            Assert.AreEqual(Seats.East, Seats.South.Previous());
            Assert.AreEqual(Seats.South, Seats.West.Previous());
        }

        [TestMethod]
        public void Seats_Partner()
        {
            Assert.AreEqual(Seats.South, Seats.North.Partner());
            Assert.AreEqual(Seats.West, Seats.East.Partner());
            Assert.AreEqual(Seats.North, Seats.South.Partner());
            Assert.AreEqual(Seats.East, Seats.West.Partner());
        }
    }
}
