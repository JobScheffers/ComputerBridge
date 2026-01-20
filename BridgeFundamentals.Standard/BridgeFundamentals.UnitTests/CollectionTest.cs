using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bridge.Test
{
	[TestClass]
	public class CollectionTest
	{

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Deal_IncompletePBN()
        {
            var pbn = "N:K95.QJT3.AKJ.AQJ JT42.87..K98765 AQ86.K652.86432. 73.A94.QT97.T432";
            var deal = new Deal(pbn);
            Assert.AreEqual(pbn, deal.ToPBN());

            int cards = 0;
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
            {
                for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
                {
                    for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
                    {
                        if (deal[seat, suit, r])
                        {
                            cards++;
                        }
                    }
                }
            }

            Assert.AreEqual(51, cards);
        }


        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Deal_Random()
        {
            var deal = new Deal();
            deal[Seats.North, Suits.Spades, Ranks.Ace] = true;
            deal[Seats.North, Suits.Spades, Ranks.Jack] = true;
            deal[Seats.North, Suits.Hearts, Ranks.King] = true;

            for (int attempt = 0; attempt < 10; attempt++)
            {
                // produce a BigInteger suitable for a full deal
                BigInteger seed = RandomGenerator.Instance.NextDealBigInteger();

                // or for remaining n cards
                //BigInteger seed = RandomGenerator.Instance.NextPermutationBigInteger(51);

                var completedDeal = deal.CompletedFromSeed(seed);
                Debug.WriteLine(completedDeal.ToPBN());
            }
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Deal_Size()
        {
            Assert.AreEqual(20, Unsafe.SizeOf<Deal>());
            var x = new Deal();
            Assert.IsFalse(x[Seats.East, Suits.Hearts, Ranks.King]);
            x[Seats.East, Suits.Hearts, Ranks.King] = true;
            Assert.IsTrue(x[Seats.East, Suits.Hearts, Ranks.King]);
            x[Seats.East, Suits.Hearts, Ranks.King] = false;
            Assert.IsFalse(x[Seats.East, Suits.Hearts, Ranks.King]);
            x[Seats.East, Suits.Hearts, Ranks.King] = true;
            x[Seats.East, Suits.Hearts, Ranks.Ace] = true;
            x[Seats.North, Suits.Clubs, Ranks.Two] = true;
            Assert.IsTrue(x[Seats.East, Suits.Hearts, Ranks.King]);
            Assert.IsTrue(x[Seats.East, Suits.Hearts, Ranks.Ace]);
            Assert.IsTrue(x[Seats.North, Suits.Clubs, Ranks.Two]);
            Assert.IsFalse(x[Seats.East, Suits.Hearts, Ranks.Queen]);
            Assert.IsFalse(x[Seats.East, Suits.Spades, Ranks.Two]);
            Assert.IsFalse(x[Seats.West, Suits.Spades, Ranks.Ace]);
            Assert.IsFalse(x[Seats.North, Suits.Clubs, Ranks.Three]);
        }

        [TestMethod]
        public void PBN2Deal2PBN()
        {
            string deal = "N:954.QJT3.AJT.QJ6 KJT2.87.5.AK9875 AQ86.K652.86432. 73.A94.KQ97.T432";
            var dealBinary = new Deal(in deal);
            var dealPBN = dealBinary.ToPBN();

            Assert.AreEqual(deal, dealPBN);
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
                SuitHelper.ForEachTrump(suit =>
                {
                    x[seat, suit] = (byte)(4 * (int)suit + (int)seat);
                });
            });

            SeatsExtensions.ForEachSeat(seat =>
            {
                SuitHelper.ForEachTrump(suit =>
                {
                    Assert.AreEqual((byte)(4 * (int)suit + (int)seat), x[seat, suit]);
                });
            });

            var y = x.ToString();
            Assert.IsFalse(string.IsNullOrWhiteSpace(y));
            Assert.AreEqual("North: 12 8 4 0 East: 13 9 5 1 South: 14 10 6 2 West: 15 11 7 3", y);
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void SeatsTrumpsArrayOfByte_Test1()
        {
            var x = new SeatsTrumpsArrayOfByte();
            SeatsExtensions.ForEachSeat(seat =>
            {
                SuitHelper.ForEachTrump(suit =>
                {
                    x[seat, suit] = (byte)(4 * (int)suit + (int)seat);
                });
            });

            SeatsExtensions.ForEachSeat(seat =>
            {
                SuitHelper.ForEachTrump(suit =>
                {
                    Assert.AreEqual((byte)(4 * (int)suit + (int)seat), x[seat, suit]);
                });
            });

            var y = x.ToString();
            Assert.IsFalse(string.IsNullOrWhiteSpace(y));
            Assert.AreEqual("North: 12 8 4 0 16 East: 13 9 5 1 17 South: 14 10 6 2 18 West: 15 11 7 3 19", y);
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
