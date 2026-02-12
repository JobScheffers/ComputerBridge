using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Bridge.Test
#pragma warning restore IDE0130 // Namespace does not match folder structure
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
            foreach (var seat in SeatsExtensions.SeatsAscending)
            {
                foreach (Suits suit in SuitHelper.StandardSuitsAscending)
                {
                    foreach (Ranks r in RankHelper.RanksAscending)
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
        public void Deal_Incomplete()
        {
            var deal = new Deal();
            Assert.AreEqual(0, deal.EnumerateCards(0).Count());

            deal[Seats.North, Suits.Spades, Ranks.King] = true;
            Assert.AreEqual(1, deal.EnumerateCards(0).Count());
            Assert.AreEqual(0, deal.EnumerateCards(1).Count());
            Assert.AreEqual(0, deal.EnumerateCards(2).Count());
            Assert.AreEqual(0, deal.EnumerateCards(3).Count());
            Assert.AreEqual("N:K... ... ... ...", deal.ToPBN());
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
                //Trace.WriteLine(completedDeal);
                Trace.WriteLine(completedDeal.ToPBN());
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
        public void SeatsSuitsRanksArray_Fill()
        {
            var x = new SeatsSuitsRanksArray<sbyte>();
            x.Fill(-1);
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
            {
                for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
                {
                    for (Ranks rank = Ranks.Two; rank <= Ranks.Ace; rank++)
                    {
                        Assert.AreEqual(-1, x[seat, suit, rank]);
                        x[seat, suit, rank] = (sbyte)(sbyte.MinValue + (sbyte)seat + 4 * (sbyte)suit + 16 * (sbyte)rank);
                    }
                }
            }
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
            {
                for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
                {
                    for (Ranks rank = Ranks.Two; rank <= Ranks.Ace; rank++)
                    {
                        Assert.AreEqual((sbyte)(sbyte.MinValue + (sbyte)seat + 4 * (sbyte)suit + 16 * (sbyte)rank), x[seat, suit, rank]);
                    }
                }
            }

            x.Fill(0);
            x[Seats.East, Suits.Hearts, Ranks.King] = 14;
            x[Seats.East, Suits.Hearts, Ranks.Jack] = 14;
            x[Seats.East, Suits.Hearts, Ranks.Five] = 14;
            Assert.AreEqual(14, x[Seats.East, Suits.Hearts, Ranks.King]);
            Debug.WriteLine(x.ToString());
            Assert.AreEqual("North: C: 0,0,0,0,0,0,0,0,0,0,0,0,0 D: 0,0,0,0,0,0,0,0,0,0,0,0,0 H: 0,0,0,0,0,0,0,0,0,0,0,0,0 S: 0,0,0,0,0,0,0,0,0,0,0,0,0 East: C: 0,0,0,0,0,0,0,0,0,0,0,0,0 D: 0,0,0,0,0,0,0,0,0,0,0,0,0 H: 0,0,0,14,0,0,0,0,0,14,0,14,0 S: 0,0,0,0,0,0,0,0,0,0,0,0,0 South: C: 0,0,0,0,0,0,0,0,0,0,0,0,0 D: 0,0,0,0,0,0,0,0,0,0,0,0,0 H: 0,0,0,0,0,0,0,0,0,0,0,0,0 S: 0,0,0,0,0,0,0,0,0,0,0,0,0 West: C: 0,0,0,0,0,0,0,0,0,0,0,0,0 D: 0,0,0,0,0,0,0,0,0,0,0,0,0 H: 0,0,0,0,0,0,0,0,0,0,0,0,0 S: 0,0,0,0,0,0,0,0,0,0,0,0,0", x.ToString());
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void SeatsSuitsArray_Fill()
        {
            var x = new SeatsSuitsArray<Ranks>();
            x.Fill(Ranks.Null);
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
            {
                for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
                {
                    Assert.AreEqual(Ranks.Null, x[seat, suit]);
                    x[seat, suit] = (Ranks)((int)seat + 4 * (int)suit);     // check if each elemet can be set to a different value
                }
            }
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
            {
                for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
                {
                    Assert.AreEqual((Ranks)((int)seat + 4 * (int)suit), x[seat, suit]);
                }
            }
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void SeatsSuitsArrayOfByte_Test1()
        {
            var x = new SeatsSuitsArray<Ranks>();
            SeatsExtensions.ForEachSeat(seat =>
            {
                SuitHelper.ForEachSuit(suit =>
                {
                    x[seat, suit] = (Ranks)((((int)seat + 1) * ((int)suit + 1)) % 13);
                });
            });

            SeatsExtensions.ForEachSeat(seat =>
            {
                SuitHelper.ForEachSuit(suit =>
                {
                    Assert.AreEqual((Ranks)((((int)seat + 1) * ((int)suit + 1)) % 13), x[seat, suit]);
                });
            });

            var y = x.ToString();
            Assert.IsFalse(string.IsNullOrWhiteSpace(y));
            Assert.AreEqual("North: Six Five Four Three East: Ten Eight Six Four South: Ace Jack Eight Five West: Five Ace Ten Six", y);
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void SeatsTrumpsArray_Test1()
        {
            var x = new SeatsTrumpsArray<Ranks>();
            SeatsExtensions.ForEachSeat(seat =>
            {
                SuitHelper.ForEachTrump(suit =>
                {
                    x[seat, suit] = (Ranks)(4 * (int)suit + (int)seat);
                });
            });

            SeatsExtensions.ForEachSeat(seat =>
            {
                SuitHelper.ForEachTrump(suit =>
                {
                    Assert.AreEqual((Ranks)(4 * (int)suit + (int)seat), x[seat, suit]);
                });
            });

            var y = x.ToString();
            Assert.IsFalse(string.IsNullOrWhiteSpace(y));
            //Assert.AreEqual("North: 12 8 4 0 16 East: 13 9 5 1 17 South: 14 10 6 2 18 West: 15 11 7 3 19", y);
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void TrickArray_Test1()
        {
            TrickArray<Seats> newArray;
            for (int trick = 1; trick <= 13; trick++)
                for (int man = 1; man <= 4; man++)
                {
                    newArray[trick, man] = Seats.East;
                }
            Seats result = 0;
            for (int i = 0; i < 9; i++)
            {
                result ^= newArray[2, 3];
            }
            Assert.AreEqual(Seats.East, result);
            newArray[3, 1] = Seats.South;
            Assert.AreEqual(Seats.South, newArray[3, 1]);
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void TrickArray_Test2()
        {
            TrickArray<Seats> newArray;
            for (int trick = 1; trick <= 13; trick++)
                for (int man = 1; man <= 4; man++)
                {
                    newArray[trick, man] = Seats.East;
                }
            Seats result = 0;
            for (int i = 0; i < 9; i++)
            {
                result ^= newArray[2, 3];
            }
            Assert.AreEqual(Seats.East, result);
            newArray[3, 1] = Seats.South;
            Assert.AreEqual(Seats.South, newArray[3, 1]);
        }

        [TestMethod]
        public void SuitsRanksArray_Fill()
        {
            var target = new SuitsRanksArray<Ranks>();
            target.Fill(Ranks.Ten);
            for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
            {
                for (Ranks rank = Ranks.Two; rank <= Ranks.Ace; rank++)
                {
                    Assert.AreEqual(Ranks.Ten, target[suit, rank]);
                    target[suit, rank] = (Ranks)((int)suit + 4 * (int)rank);        // check if each elemet can be set to a different value
                }
            }
            for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
            {
                for (Ranks rank = Ranks.Two; rank <= Ranks.Ace; rank++)
                {
                    Assert.AreEqual((Ranks)((int)suit + 4 * (int)rank), target[suit, rank]);
                }
            }

            Debug.WriteLine(target.ToString());

            var copy = target;
            for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
            {
                for (Ranks rank = Ranks.Two; rank <= Ranks.Ace; rank++)
                {
                    Assert.AreEqual((Ranks)((int)suit + 4 * (int)rank), copy[suit, rank]);
                }
            }
        }
    }
}
