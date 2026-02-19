using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

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
        public void Deal_CompletedFromSeed_IsDeterministic_AndPreservesAssignments()
        {
            var partial = new Deal(); // all unassigned initially

            // Preassign a few cards for North (seat 0) just as an example:
            partial[0, /*spades*/0, /*Ace*/12] = true; // suit=0..3, rank=0..12 per your struct
            partial[0, 1, 11] = true; // etc.

            var seed = RandomGenerator.Instance.NextDealBigInteger();
            var a = partial.CompletedFromSeed(seed);
            var b = partial.CompletedFromSeed(seed);

            Assert.AreEqual(a.ToPBN(), b.ToPBN()); // or a bytewise comparison if you expose it

            // Preassigned cards remain with the same seat:
            Assert.IsTrue(a[0, 0, 12] && a[0, 1, 11]);
            Assert.IsTrue(b[0, 0, 12] && b[0, 1, 11]);

            // All 52 assigned, 13 per seat:
            for (int s = 0; s < 4; s++)
                Assert.AreEqual(13, a.EnumerateCards(s).Count());
        }

        [TestMethod]
        public void Deal_CompletedDeal_Has52UniqueCards_AndCounts()
        {
            dynamic d0 = new Deal();
            var d = d0.CompletedFromSeed(TestHelpers.SeedFromCounter(1));

            int total = 0;
            for (int s = 0; s < 4; s++)
                total += ((System.Collections.IEnumerable)d.EnumerateCards(s)).Cast<object>().Count();
            Assert.AreEqual(52, total);

            for (int s = 0; s < 4; s++)
                Assert.AreEqual(13, ((System.Collections.IEnumerable)d.EnumerateCards(s)).Cast<object>().Count());
        }

        [TestMethod]
        public void Deal_ExactUniformity_4Remaining_OneEachSeat()
        {
            dynamic baseDeal = new Deal();

            // Assign first 48 cards in blocks of 12 to seats 0..3
            for (int i = 0; i < 48; i++)
            {
                int suit = i / 13;
                int rank = i % 13;
                int seat = i / 12; // 0..3
                baseDeal.SetOwner(seat, suit, rank);
            }

            int[] remaining = [48, 49, 50, 51];
            int[,] counts = new int[4, 4]; // [whichRemaining, seat]

            for (int seed = 0; seed < 24; seed++)
            {
                var d = baseDeal.CompletedFromSeed(new BigInteger(seed));
                for (int r = 0; r < 4; r++)
                {
                    int ci = remaining[r];
                    int suit = ci / 13;
                    int rank = ci % 13;
                    int owner = (int)d.GetOwner(suit, rank);
                    counts[r, owner]++;
                }
            }

            for (int r = 0; r < 4; r++)
                for (int s = 0; s < 4; s++)
                    Assert.AreEqual(6, counts[r, s], $"Card index {remaining[r]} to seat {s}");
        }

        [TestMethod]
        public void Deal_ExactUniformity_5Remaining_BlockNeeds_2_2_1_0()
        {
            var baseDeal = new Deal();

            // Target: have = [11,11,12,13] ⇒ need = [2,2,1,0]
            int[] targetHave = [11, 11, 12, 13];
            int[] have = new int[4];

            // Pick exactly 5 card indices to remain unassigned (any 5 distinct indices are fine)
            int[] remaining = [0, 7, 19, 28, 51];
            var remainingSet = new System.Collections.Generic.HashSet<int>(remaining);

            // Assign all other 47 cards to reach targetHave exactly
            for (int ci = 0; ci < 52; ci++)
            {
                if (remainingSet.Contains(ci)) continue;

                for (int s = 0; s < 4; s++)
                {
                    if (have[s] < targetHave[s])
                    {
                        baseDeal.SetOwner(s, ci / 13, ci % 13);
                        have[s]++;
                        break;
                    }
                }
            }

            // Verify setup reached the intended counts
            Assert.AreEqual(11, have[0], "Seat 0 preassigned count");
            Assert.AreEqual(11, have[1], "Seat 1 preassigned count");
            Assert.AreEqual(12, have[2], "Seat 2 preassigned count");
            Assert.AreEqual(13, have[3], "Seat 3 preassigned count");

            // Tally where each of the 5 remaining cards goes across the 120 permutations
            int[,] counts = new int[5, 4];

            for (int seed = 0; seed < 120; seed++)
            {
                var d = baseDeal.CompletedFromSeed(new System.Numerics.BigInteger(seed));

                for (int i = 0; i < remaining.Length; i++)
                {
                    int ci = remaining[i];
                    int owner = (int)d.GetOwner(ci / 13, ci % 13).Value;
                    counts[i, owner]++;
                }
            }

            // Expect exact counts: seat0=48, seat1=48, seat2=24, seat3=0 for every remaining card
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(48, counts[i, 0], $"Card {i} seat 0");
                Assert.AreEqual(48, counts[i, 1], $"Card {i} seat 1");
                Assert.AreEqual(24, counts[i, 2], $"Card {i} seat 2");
                Assert.AreEqual(0, counts[i, 3], $"Card {i} seat 3");
            }
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public void Deal_PartialDeal_SingleCardSeat_Probability_Matches_NeedOverN()
        {
            int N = 10;
            dynamic d0 = new Deal();

            // Build a reproducible partial deal:
            // want have ~ [8,10,12,9] => need [5,3,1,4] (sum 13)
            int[] have = new int[4];
            for (int ci = 0; ci < 52; ci++)
            {
                int seat = -1;
                if (have[0] < 8 && ci % 7 == 0) seat = 0;
                else if (have[1] < 10 && ci % 5 == 0) seat = 1;
                else if (have[2] < 12 && ci % 3 == 0) seat = 2;
                else if (have[3] < 9 && ci % 2 == 0) seat = 3;
                if (seat >= 0)
                {
                    d0.SetOwner(seat, ci / 13, ci % 13);
                    have[seat]++;
                }
            }
            int[] need = new int[4];
            for (int s = 0; s < 4; s++) need[s] = 13 - have[s];
            int n = need[0] + need[1] + need[2] + need[3];
            Assert.IsGreaterThan(0, n);

            // choose first unassigned card
            int cardSuit = -1, cardRank = -1;
            for (int ci = 0; ci < 52; ci++)
            {
                int suit = ci / 13, rank = ci % 13;
                var owner = ((object)d0.GetOwner(suit, rank)) as int?;
                if (owner == null) { cardSuit = suit; cardRank = rank; break; }
            }
            Assert.IsGreaterThanOrEqualTo(0, cardSuit);

            long[] seatCount = new long[4];
            for (int i = 0; i < N; i++)
            {
                var d = d0.CompletedFromSeed(TestHelpers.SeedFromCounter(i + 2024));
                int owner = (int)d.GetOwner(cardSuit, cardRank);
                seatCount[owner]++;
            }

            for (int s = 0; s < 4; s++)
            {
                double p = (double)need[s] / n;
                double expected = N * p;
                double sd = Math.Sqrt(N * p * (1 - p));
                TestHelpers.AssertWithinSigmas(seatCount[s], expected, sd, 6, $"Seat {s} need/n");
            }
        }

        [TestMethod]
        [TestCategory("LongRunning")] // set DEAL_TEST_SAMPLES to increase sample size
        public void Deal_Marginal_CardSeat_IsQuarterEach()
        {
            int N = TestHelpers.SampleSize();
            dynamic d0 = new Deal();
            int suit = 0;  // spades
            int rank = 12; // Ace (assumes Two=0..Ace=12)

            long[] seatCount = new long[4];
            for (int i = 0; i < N; i++)
            {
                var d = d0.CompletedFromSeed(TestHelpers.SeedFromCounter(i + 1));
                int owner = (int)d.GetOwner(suit, rank);
                seatCount[owner]++;
            }

            double expected = N / 4.0;
            double sd = Math.Sqrt(N * 0.25 * 0.75);
            for (int s = 0; s < 4; s++)
                TestHelpers.AssertWithinSigmas(seatCount[s], expected, sd, 6, $"Seat {s} quarter-marginal");
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public void Deal_TwoCards_SameSeat_Probability_12over51()
        {
            int N = TestHelpers.SampleSize();
            dynamic d0 = new Deal();
            int s1 = 0, r1 = 12; // ♠A
            int s2 = 1, r2 = 12; // ♥A

            long sameSeat = 0;
            for (int i = 0; i < N; i++)
            {
                var d = d0.CompletedFromSeed(TestHelpers.SeedFromCounter(i + 123_456));
                int o1 = (int)d.GetOwner(s1, r1);
                int o2 = (int)d.GetOwner(s2, r2);
                if (o1 == o2) sameSeat++;
            }

            double p = 12.0 / 51.0; // theoretical probability
            double expected = N * p;
            double sd = Math.Sqrt(N * p * (1 - p));
            TestHelpers.AssertWithinSigmas(sameSeat, expected, sd, 6, "Two-card same-seat probability 12/51");
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public void Deal_HandPatterns_Roughly_Match_Baselines()
        {
            int N = TestHelpers.SampleSize();
            dynamic d0 = new Deal();

            long c4432 = 0, c5332 = 0, c5431 = 0, c5422 = 0, c4333 = 0;

            for (int i = 0; i < N; i++)
            {
                var d = d0.CompletedFromSeed(TestHelpers.SeedFromCounter(i + 987_654));
                // Check only seat 0 to keep samples independent enough
                var pat = TestHelpers.CanonicalPattern(TestHelpers.SuitLengths(d, 0));
                switch (pat)
                {
                    case "4432": c4432++; break;
                    case "5332": c5332++; break;
                    case "5431": c5431++; break;
                    case "5422": c5422++; break;
                    case "4333": c4333++; break;
                }
            }

            // Baseline percentages (Ken Monzingo / BridgeHands)
            double p4432 = 0.2155; // ≈21.55%
            double p5332 = 0.1552; // ≈15.52%
            double p5431 = 0.1293; // ≈12.93%
            double p5422 = 0.1058; // ≈10.58%
            double p4333 = 0.1053; // ≈10.53%

            void Check(long obs, double p, string label)
            {
                double expected = N * p;
                double sd = Math.Sqrt(N * p * (1 - p));
                TestHelpers.AssertWithinSigmas(obs, expected, sd, 6, $"Pattern {label}");
            }

            Check(c4432, p4432, "4432");
            Check(c5332, p5332, "5332");
            Check(c5431, p5431, "5431");
            Check(c5422, p5422, "5422");
            Check(c4333, p4333, "4333");
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public void Deal_Hcp_Distribution_Sane_MeanAround10()
        {
            int N = TestHelpers.SampleSize();
            dynamic d0 = new Deal();

            long sumHcp = 0;
            for (int i = 0; i < N; i++)
            {
                var d = d0.CompletedFromSeed(TestHelpers.SeedFromCounter(i + 246_810));
                sumHcp += TestHelpers.ComputeHcp(d, 0); // seat 0 only
            }

            double mean = sumHcp / (double)N;
            // Pavlicek: mean ≈ 10 HCP per hand
            Assert.IsTrue(mean > 9.6 && mean < 10.4, $"Mean HCP ≈ 10 expected, got {mean:F3}");
        }

        [TestMethod]
        [TestCategory("LongRunning")]
        public void Deal_Hcp_Distribution_WeakHand_MeanAround13()
        {
            int N = TestHelpers.SampleSize();
            dynamic d0 = new Deal("N:954.8763.T98.976");

            long sumHcp = 0;
            for (int i = 0; i < N; i++)
            {
                var d = d0.CompletedFromSeed(TestHelpers.SeedFromCounter(i + 246_811));
                sumHcp += TestHelpers.ComputeHcp(d, 1); // East only
            }

            double mean = sumHcp / (double)N;
            Assert.IsLessThan(0.1, Math.Abs(mean - 13.3), $"Mean HCP ≈ 13.3 expected, got {mean:F3}");
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

    public static class TestHelpers
    {
        /// <summary>
        /// Turn an integer counter into a non-negative BigInteger via SHA256, to spread seeds well.
        /// </summary>
        public static BigInteger SeedFromCounter(long counter)
        {
            Span<byte> bytes = stackalloc byte[8];
            BitConverter.TryWriteBytes(bytes, counter);
            //using var sha = SHA256.Create();
            //var hash = sha.ComputeHash(bytes.ToArray());
            var hash = SHA256.HashData(bytes);
            // BigInteger ctor expects little-endian; append a 0x00 to force non-negative
            var tmp = new byte[hash.Length + 1];
            Array.Copy(hash, 0, tmp, 0, hash.Length);
            return new BigInteger(tmp);
        }

        /// <summary>
        /// Compute High Card Points (A=4,K=3,Q=2,J=1) for a seat.
        /// Assumes rank integers map Two=0 .. Ace=12.
        /// </summary>
        public static int ComputeHcp(Deal deal, int seat)
        {
            int hcp = 0;
            foreach (var (suit, rank) in deal.EnumerateCards(seat))
            {
                if (rank == 12) hcp += 4;       // Ace
                else if (rank == 11) hcp += 3;  // King
                else if (rank == 10) hcp += 2;  // Queen
                else if (rank == 9) hcp += 1;  // Jack
            }
            return hcp;
        }

        /// <summary>
        /// Get a tuple of suit lengths for a seat as (s0, s1, s2, s3), suits assumed 0..3.
        /// </summary>
        public static (int s0, int s1, int s2, int s3) SuitLengths(Deal deal, int seat)
        {
            int[] counts = new int[4];
            foreach (var (suit, _) in deal.EnumerateCards(seat))
                counts[suit]++;
            return (counts[0], counts[1], counts[2], counts[3]);
        }

        /// <summary>
        /// Return a canonical hand pattern string like "4432" sorted descending, for a (s0,s1,s2,s3) tuple.
        /// </summary>
        public static string CanonicalPattern((int s0, int s1, int s2, int s3) t)
        {
            var arr = new[] { t.s0, t.s1, t.s2, t.s3 };
            Array.Sort(arr);
            Array.Reverse(arr);
            return string.Concat(arr.Select(x => x.ToString()));
        }

        /// <summary>
        /// Simple z-test style bound.
        /// </summary>
        public static void AssertWithinSigmas(long observed, double expected, double sigma, int sigmas, string message)
        {
            var diff = Math.Abs(observed - expected);
            if (diff > sigmas * sigma)
                throw new Microsoft.VisualStudio.TestTools.UnitTesting.AssertFailedException(
                    $"{message}. Observed={observed}, Expected≈{expected:F2}, |Δ|/σ={(diff / sigma):F2} > {sigmas}");
        }

        public static int SampleSize()
        {
            return 100_000; // raise to 200_000+ for tighter bounds
        }
    }
}
