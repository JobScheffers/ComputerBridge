﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.CompilerServices;

namespace Bridge.Test
{
	[TestClass]
	public class CollectionTest
	{
        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Deal_Size()
        {
            Assert.AreEqual(26, Unsafe.SizeOf<Deal>());
            var x = new Deal();
            Assert.IsFalse(x[Seats.East, Suits.Hearts, Ranks.King]);
            x[Seats.East, Suits.Hearts, Ranks.King] = true;
            Assert.IsTrue(x[Seats.East, Suits.Hearts, Ranks.King]);
            x[Seats.East, Suits.Hearts, Ranks.King] = false;
            Assert.IsFalse(x[Seats.East, Suits.Hearts, Ranks.King]);
            x[Seats.East, Suits.Hearts, Ranks.King] = true;
            x[Seats.East, Suits.Hearts, Ranks.Ace] = true;
            Assert.IsTrue(x[Seats.East, Suits.Hearts, Ranks.King]);
            Assert.IsTrue(x[Seats.East, Suits.Hearts, Ranks.Ace]);
            Assert.IsFalse(x[Seats.East, Suits.Hearts, Ranks.Queen]);
            Assert.IsFalse(x[Seats.East, Suits.Spades, Ranks.Two]);
            Assert.IsFalse(x[Seats.West, Suits.Spades, Ranks.Ace]);
            Assert.IsFalse(x[Seats.North, Suits.Clubs, Ranks.Two]);
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void SeatsSuitsRanksArrayOfByte_HighestLowest()
        {
            var x = new SeatsSuitsRanksArrayOfByte();
            x[Seats.East, Suits.Hearts, Ranks.King] = 14;
            x[Seats.East, Suits.Hearts, Ranks.Jack] = 14;
            x[Seats.East, Suits.Hearts, Ranks.Five] = 14;
            Assert.AreEqual(Ranks.King, x.Highest(Seats.East, Suits.Hearts, 0));
            Assert.AreEqual(Ranks.Five, x.Lowest(Seats.East, Suits.Hearts , 0));
            Assert.AreEqual(Ranks.Jack, x.Highest(Seats.East, Suits.Hearts, 1));
            Assert.AreEqual(Ranks.Jack, x.Lowest(Seats.East, Suits.Hearts, 1));
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void SeatsSuitsArrayOfByte_Test1()
        {
            var x = new SeatsSuitsArrayOfByte();
            SeatsExtensions.ForEachSeat(seat =>
            {
                x[seat, Suits.Hearts] = 6;
                x[seat, Suits.Clubs] = 5;
                x[seat, Suits.Diamonds] = (byte)(seat + 1);
                Assert.AreEqual(6, x[seat, Suits.Hearts]);
                Assert.AreEqual(5, x[seat, Suits.Clubs]);
                Assert.AreEqual(0, x[seat, Suits.Spades]);
            });


            var y = x.DisplayValue;
            Assert.IsFalse(string.IsNullOrWhiteSpace(y));
            Assert.AreEqual("North: 0 6 1 5 East: 0 6 2 5 South: 0 6 3 5 West: 0 6 4 5", y);
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void SeatsTrumpsArrayOfByte_Test1()
        {
            var x = new SeatsTrumpsArrayOfByte();
            SeatsExtensions.ForEachSeat(seat =>
            {
                x[seat, Suits.Hearts] = 6;
                x[seat, Suits.Clubs] = 5;
                x[seat, Suits.Diamonds] = (byte)(seat + 1);
                x[seat, Suits.NoTrump] = 8;
                Assert.AreEqual(6, x[seat, Suits.Hearts]);
                Assert.AreEqual(5, x[seat, Suits.Clubs]);
                Assert.AreEqual(0, x[seat, Suits.Spades]);
                Assert.AreEqual(8, x[seat, Suits.NoTrump]);
            });


            var y = x.DisplayValue;
            Assert.IsFalse(string.IsNullOrWhiteSpace(y));
            Assert.AreEqual("North: 0 6 1 5 8 East: 0 6 2 5 8 South: 0 6 3 5 8 West: 0 6 4 5 8", y);
        }

        [TestMethod]
        public void Suits_SuitsRanksArrayOfRanks_Debug()
        {
            var target = new SuitsRanksArrayOfRanks();
            //target[Suits.Clubs, Ranks.Two] = Ranks.Ace;
            target[Suits.Diamonds, Ranks.Three] = Ranks.Three;
        }
    }
}
