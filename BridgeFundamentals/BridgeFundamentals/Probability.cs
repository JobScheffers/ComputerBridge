//#define new
using System;
using System.Collections.Generic;
using System.Text;

namespace Sodes.Bridge.Base
{
    public static class BridgeProbabilities
    {
        /*
         http://www.bridgehands.com/P/Posteriori_Probabilities.htm
        Trumps
        Suit		          Second Suit 
                        a priori		 5-4		 6-3		 7-2		 8-1		 9-0 
        4-0				 4.78%	 2.94%	 1.47%	 0.63%	 0.21%	 0.04% 
        3-1				24.87%	21.17%	14.71%	 9.24%	 5.04%	 2.19% 
        2-2				40.70%	42.35%	39.71%	34.66%	27.73%  19.66% 
        1-3				24.87%	28.24%	35.29%	41.60%	46.22%  48.07% 
        0-4				 4.78%	 5.29%	 8.82%	13.87%	20.80%  30.04% 
         */

        private static double[] pHcp = new double[44]
      {
                0.3639,	//  0
                0.7884,
                1.3561,
                2.4624,
                3.8454,
                5.1862,
                6.5541,
                8.0281,
                8.8922,
                9.3562,
                9.4051,	// 10
                8.9447,
                8.0269,
                6.9143,
                5.6933,
                4.4237,
                3.3109,
                2.3617,
                1.6051,
                1.0362,
                0.6435,	// 20
                0.3779,
                0.2100,
                0.1119,
                0.0559,
                0.0264,
                0.0117,
                0.0049,
                0.0019,
                0.0007,
                0.0002,	// 30
                0.00001,
                0.00001,
                0.00001,
                0.00001,
                0.00001,
                0.00001,
                0.00001,	// 37
                0,
                0,
                0,
                0,
                0,
                0
      };

        public static double Hcp(int count, int min, int max)
        {
            if (count < min) return 0;
            if (count > max) return 0;
            if (count > 38) return 0;
            if (min >= 38) return 0;		// prevent a 'divide by zero'

            if (min < 0) min = 0;
            if (max > 37) max = 37;

            double total = 0;
            for (int i = min; i <= max; i++)
            {
                total += pHcp[i];
            }

            var result = pHcp[count] / total;
            return result;
        }

        public static double Hcp(int count, MinMax range)
        {
            return Hcp(count, range.Min, range.Max);
        }

        public static double HcpAtLeast(int count, int min, int max)
        {
            if (count <= min) return 1;
            if (count > max) return 0;
            if (count > 38) return 0;
            if (min >= 38) return 0;		// prevent a 'divide by zero'

            if (min < 0) min = 0;
            if (max > 37) max = 37;

            double total = 0;
            double valid = 0;
            for (int i = min; i <= max; i++)
            {
                total += pHcp[i];
                if (i >= count) valid += pHcp[i];
            }

            var result = valid / total;
            return result;
        }

        public static double HcpAtLeast(int count, MinMax range)
        {
            return HcpAtLeast(count, range.Min, range.Max);
        }

        public static double HcpAtMost(int count, int min, int max)
        {
            return 1.0 - HcpAtLeast(count + 1, min, max);
        }

        public static double HcpAtMost(int count, MinMax range)
        {
            return HcpAtMost(count, range.Min, range.Max);
        }

        public static double ProbabilityAtLeast(Seats p, int count, Suits s, SpelersBeeld view, Seats Plaats)
        {
            if (s == Suits.NoTrump) return 0;
            if (count <= view[p].L[s].Min) return 1;
            if (count > view[p].L[s].Max) return 0;

            double result = 0;
            var remaining = Aposteriori(s, view, Plaats);
            if (remaining.Count == 0) return 0;

            double remainingProbability = 0;
            foreach (var dp in remaining)
            {
                remainingProbability += dp.probability;
                if (dp.suitLength[0] >= count)
                {   // at least the longest complies with what we're looking for
                    int freeSlots = 0;
                    int resultSlots = 0;
                    for (int l = 0; l <= 2; l++)
                    {
                        if (dp.takenMin[l] == p && dp.takenMax[l] == p && dp.suitLength[l] < count)
                        {
                            resultSlots = 0;
                            freeSlots = 0;
                            break;
                        }

                        if (dp.takenMin[l] == p && dp.suitLength[l] >= count)
                        {   // 1 length has been reserved for Sp; so 100% of this probability counts
                            freeSlots = 3;
                            if (dp.suitLength[0] <= view[p].L[s].Max) resultSlots = 3;
                            else if (dp.suitLength[1] <= view[p].L[s].Max) resultSlots = 2;
                            else resultSlots = 1;
                            break;
                        }

                        if (   (dp.takenMin[l] == Plaats 
                                )
                            || (dp.takenMax[l] == Plaats
                                && (dp.takenMin[l] == Plaats
                                    || (l < 2 && dp.takenMin[l + 1] == Plaats && view[dp.takenMin[l]].L[s].Min <= dp.suitLength[l + 1])
                                    )
                                )
                            )
                        {   // this length has not been taken by someone else
                            freeSlots++;
                            if (dp.suitLength[l] >= count)
                            {
                                if (dp.suitLength[l] <= view[p].L[s].Max
                                    )
                                {
                                    resultSlots++;
                                    //if (l == 2 && dp.takenMin[l] == Plaats && dp.takenMax[l] == Plaats) break;
                                }
                            }
                            else
                            {
                                //if (dp.takenMin[l] != Plaats && dp.takenMin[l] != p)
                                //{		// this slot is partially taken by someone else (maybe because he bid NT)
                                //    freeSlots++;		// chances that p has the previous slot gets less
                                //}
                            }
                        }
                    }

                    if (freeSlots > 0 && resultSlots > 0)
                    {
                        result += dp.probability * resultSlots / freeSlots;
                    }
                }
            }

            result = result / remainingProbability;

            // compensate for known length in other suits
            //for (Suits other = Suits.Clubs; other <= Suits.Spades; other++)
            //{
            //  if (other != s)
            //  {
            //    for (int length = 4; length <= view[p].L[other].Min; length++)
            //    {
            //      result *= 0.93;
            //    }
            //  }
            //}

            return result;
        }

        /// <summary>
        /// Estimate of the most probable length a player will have in a suit
        /// </summary>
        /// <param name="p"></param>
        /// <param name="s"></param>
        /// <remarks>
        /// Based on The Official Encyclopedia of Bridge
        /// Mathematical Tables
        /// Table 3 Probability of Distribution of Cards in Three Hidden Hands
        /// </remarks>
        /// <returns></returns>
        public static double ProbableLength(Seats p, Suits s, SpelersBeeld view, Seats Plaats)
        {
            var distributions = BridgeProbabilities.Aposteriori(s, view, Plaats);   // pick the most probable distribution
            if (distributions.Count == 0)
            {
#if DEBUG
                //System.Diagnostics.Debugger.Break();
#endif
                return view[p].L[s].Min;   // noodmaatregel
            }

            DistributionProbability dp = distributions[0];   // pick the most probable distribution

            if (dp.takenMin[0] == p && dp.takenMax[0] == p)
            {
                return dp.suitLength[0];
            }

            if (dp.takenMin[1] == p && dp.takenMax[1] == p)
            {
                return dp.suitLength[1];
            }

            if (dp.takenMin[2] == p && dp.takenMax[2] == p)
            {
                if (distributions.Count >= 2
                        && distributions[1].takenMin[2] == p
                        && (distributions[1].probability / dp.probability
                                > 0.15 * (13.0 - (view[p].L[Suits.Clubs].Min
                                + view[p].L[Suits.Diamonds].Min
                                + view[p].L[Suits.Hearts].Min
                                + view[p].L[Suits.Spades].Min))
                                )
                     )
                {
                    return distributions[1].suitLength[2];
                }

                return dp.suitLength[2];
            }

            // calculate the room within each hand for this suit length
            SeatCollection<int> room = new SeatCollection<int>();
            Seats[] most = new Seats[4];
            for (Seats pl = Seats.North; pl <= Seats.West; pl++)
            {
                most[(int)pl] = pl;
                if (pl == Plaats
                        || (pl == dp.takenMin[0] && pl == dp.takenMax[0]) || (pl == dp.takenMin[1] && pl == dp.takenMax[1]) || (pl == dp.takenMin[2] && pl == dp.takenMax[2])
                    )
                {
                    room[pl] = 0;
                }
                else
                {
                    room[pl] = 13 + 5 * view[pl].L[s].Min + 2 * view[pl].L[s].Max;		// if pl has already shown length in this suit, it is more likely that he has the longest slot
                    for (Suits s1 = Suits.Clubs; s1 <= Suits.Spades; s1++)
                    {
                        if (s1 != s)
                        {
                            room[pl] -= view[pl].L[s1].Min;
                        }
                    }
                }
            }

            // sort: who has most room for this suit?
            for (int i = 0; i <= 2; i++)
            {
                int k = i;
                for (int j = i + 1; j <= 3; j++)
                {
                    if (room[most[j]] > room[most[k]])
                    {
                        k = j;
                    }
                }

                if (k != i)
                {
                    Seats x = most[i];
                    most[i] = most[k];
                    most[k] = x;
                }
            }

            int totalRoom = room[most[0]] + room[most[1]] + room[most[2]];
            // if no specific length is taken by the player, return the first untaken spot
            // and average over the other untaken spots, based on room 
            if ((dp.takenMin[0] == Plaats || dp.takenMin[0] != dp.takenMax[0])
                    && most[0] == p
                    && view[p].L[s].Max >= dp.suitLength[0]
                 )
            {		// slot 0 is still free and p has most room
                double l1 = dp.suitLength[0];
#if new
                l1 += -1.0 * (dp.suitLength[0] - dp.suitLength[1]) * room[most[1]] / totalRoom - 1.0 * (dp.suitLength[0] - dp.suitLength[2]) * room[most[2]] / totalRoom;
#endif
                return l1;
            }

            if ((dp.takenMin[1] == Plaats || dp.takenMin[1] != dp.takenMax[1])
                    && (most[0] == p || ((dp.takenMin[0] == Plaats || dp.takenMin[0] != dp.takenMax[0]) && most[1] == p))
                    && view[p].L[s].Max >= dp.suitLength[1]
                 )
            {		// slot 1 is still free and p has most room or slot 0 was still free and p has 2nd most room
                double l2 = dp.suitLength[1];
#if new
                l2 += 1.0 * (dp.suitLength[0] - dp.suitLength[1]) * room[most[1]] / totalRoom - 1.0 * (dp.suitLength[1] - dp.suitLength[2]) * room[most[2]] / totalRoom;
#endif
                return l2;
            }

            double l3 = dp.suitLength[2];
#if new
            l3 += 1.0 * (dp.suitLength[0] - dp.suitLength[2]) * room[most[2]] / totalRoom + 1.0 * (dp.suitLength[1] - dp.suitLength[2]) * room[most[2]] / totalRoom;
#endif
            return l3;
        }

        /// <summary>
        /// Estimate of the most probable length a player will have in a suit
        /// </summary>
        /// <param name="p"></param>
        /// <param name="s"></param>
        /// <remarks>
        /// Based on The Official Encyclopedia of Bridge
        /// Mathematical Tables
        /// Table 3 Probability of Distribution of Cards in Three Hidden Hands
        /// </remarks>
        /// <returns></returns>
        public static double AverageLength(Seats p, Suits s, SpelersBeeld view, Seats Plaats)
        {
            var distributions = BridgeProbabilities.Aposteriori(s, view, Plaats);   // pick the most probable distribution
            if (distributions.Count == 0)
            {
#if DEBUG
                //System.Diagnostics.Debugger.Break();
#endif
                return view[p].L[s].Min;   // noodmaatregel
            }

            DistributionProbability dp = distributions[0];   // pick the most probable distribution

            if (dp.takenMin[0] == p && dp.takenMax[0] == p)
            {
                return dp.suitLength[0];
            }

            if (dp.takenMin[1] == p && dp.takenMax[1] == p)
            {
                return dp.suitLength[1];
            }

            if (dp.takenMin[2] == p && dp.takenMax[2] == p)
            {
                if (distributions.Count >= 2
                        && distributions[1].takenMin[2] == p
                        && (distributions[1].probability / dp.probability
                                > 0.15 * (13.0 - (view[p].L[Suits.Clubs].Min
                                + view[p].L[Suits.Diamonds].Min
                                + view[p].L[Suits.Hearts].Min
                                + view[p].L[Suits.Spades].Min))
                                )
                     )
                {
                    return distributions[1].suitLength[2];
                }

                return dp.suitLength[2];
            }

            // calculate the room within each hand for this suit length
            SeatCollection<int> room = new SeatCollection<int>();
            Seats[] most = new Seats[4];
            for (Seats pl = Seats.North; pl <= Seats.West; pl++)
            {
                most[(int)pl] = pl;
                if (pl == Plaats
                        || (pl == dp.takenMin[0] && pl == dp.takenMax[0]) || (pl == dp.takenMin[1] && pl == dp.takenMax[1]) || (pl == dp.takenMin[2] && pl == dp.takenMax[2])
                    )
                {
                    room[pl] = 0;
                }
                else
                {
                    room[pl] = 13 + 5 * view[pl].L[s].Min + 2 * view[pl].L[s].Max;		// if pl has already shown length in this suit, it is more likely that he has the longest slot
                    for (Suits s1 = Suits.Clubs; s1 <= Suits.Spades; s1++)
                    {
                        if (s1 != s)
                        {
                            room[pl] -= view[pl].L[s1].Min;
                        }
                    }
                }
            }

            // sort: who has most room for this suit?
            for (int i = 0; i <= 2; i++)
            {
                int k = i;
                for (int j = i + 1; j <= 3; j++)
                {
                    if (room[most[j]] > room[most[k]])
                    {
                        k = j;
                    }
                }

                if (k != i)
                {
                    Seats x = most[i];
                    most[i] = most[k];
                    most[k] = x;
                }
            }

            int totalRoom = room[most[0]] + room[most[1]] + room[most[2]];
            // if no specific length is taken by the player, return the first untaken spot
            // and average over the other untaken spots, based on room 
            if ((dp.takenMin[0] == Plaats || dp.takenMin[0] != dp.takenMax[0])
                    && most[0] == p
                    && view[p].L[s].Max >= dp.suitLength[0]
                 )
            {		// slot 0 is still free and p has most room
                double l1 = dp.suitLength[0];
                l1 += -1.0 * (dp.suitLength[0] - dp.suitLength[1]) * room[most[1]] / totalRoom - 1.0 * (dp.suitLength[0] - dp.suitLength[2]) * room[most[2]] / totalRoom;
                return l1;
            }

            if ((dp.takenMin[1] == Plaats || dp.takenMin[1] != dp.takenMax[1])
                    && (most[0] == p || ((dp.takenMin[0] == Plaats || dp.takenMin[0] != dp.takenMax[0]) && most[1] == p))
                    && view[p].L[s].Max >= dp.suitLength[1]
                 )
            {		// slot 1 is still free and p has most room or slot 0 was still free and p has 2nd most room
                double l2 = dp.suitLength[1];
                l2 += 1.0 * (dp.suitLength[0] - dp.suitLength[1]) * room[most[1]] / totalRoom - 1.0 * (dp.suitLength[1] - dp.suitLength[2]) * room[most[1]] / totalRoom;
                return l2;
            }

            double l3 = dp.suitLength[2];
            l3 += 1.0 * (dp.suitLength[0] - dp.suitLength[2]) * room[most[0]] / totalRoom + 1.0 * (dp.suitLength[1] - dp.suitLength[2]) * room[most[2]] / totalRoom;
            return l3;
        }

        public static double ProbabilityOneOpponentHasAtLeast(int Aantal, Suits Kleur, SpelersBeeld view, Seats Plaats)
        {
            var remaining = Aposteriori(Kleur, view, Plaats);
            if (remaining.Count == 0)
            {
#if DEBUG
                //System.Diagnostics.Debugger.Break();
#endif
                return 0.5;
            }

            double rKans = 0;
            double remainingProbability = 0;
            Seats partner = Plaats.Partner();
            foreach (DistributionProbability dp in remaining)
            {
                remainingProbability += dp.probability;
                if (dp.suitLength[1] >= Aantal || (dp.suitLength[0] >= Aantal && (dp.takenMax[1] == partner || dp.takenMax[2] == partner)))
                {		// one of the shortest slots is large enough or partner has taken a short slot
                    rKans += dp.probability;
                }
                else
                {		// only the longest slot is long enough; what is the chance that the longest slot is taken by one of the opponents?
                    if (dp.takenMin[0] == partner || dp.suitLength[0] < Aantal)
                    {		// opponents have the shortest slot or the middle slot is too short
                    }
                    else
                    {   // the longest slot is big enough and not taken by partner
                        rKans += 0.6667 * dp.probability;    // 2/3 chance that opponents have both longest slots
                    }
                }
            }

            rKans = rKans / remainingProbability;
            return rKans;
        }

        public static double ProbabilityOneOpponentHasAtMost(int Aantal, Suits Kleur, SpelersBeeld view, Seats Plaats)
        {
            var remaining = Aposteriori(Kleur, view, Plaats);
            if (remaining.Count == 0)
            {
#if DEBUG
                //System.Diagnostics.Debugger.Break();
#endif
                return 0.5;
            }

            double rKans = 0;
            double remainingProbability = 0;
            Seats Partner = Plaats.Partner();
            foreach (DistributionProbability dp in remaining)
            {
                remainingProbability += dp.probability;
                if (dp.suitLength[1] <= Aantal || (dp.suitLength[2] <= Aantal && (dp.takenMax[1] == Partner || dp.takenMax[0] == Partner)))
                {		// even the middle hand has less than required, so at least one opponent has Aantal or less
                    rKans += dp.probability;
                }
                else
                {		// first two slots are longer; what is the chance that one of the opponents has the remaining slot?
                    if (dp.takenMin[2] == Partner || dp.suitLength[2] > Aantal)
                    {		// no chance that opponents have last slot or that slot is too long
                    }
                    else
                    {   // the last slot is small enough and not taken by partner
                        rKans += 0.6667 * dp.probability;    // 2/3 chance that opponents have last slot
                    }
                }
            }

            rKans = rKans / remainingProbability;
            return rKans;
        }

        public static double ProbabilityBothOpponentHaveAtLeast(int Aantal, Suits Kleur, SpelersBeeld view, Seats Plaats)
        {
            var remaining = Aposteriori(Kleur, view, Plaats);
            if (remaining.Count == 0)
            {
#if DEBUG
                //System.Diagnostics.Debugger.Break();
#endif
                return 0.5;
            }

            double rKans = 0;
            double remainingProbability = 0;
            Seats Partner = Plaats.Partner();
            foreach (DistributionProbability dp in remaining)
            {
                remainingProbability += dp.probability;
                if (dp.suitLength[2] >= Aantal)
                {		// even the shortest hand is long enough
                    rKans += dp.probability;
                }
                else
                {		// shortest hand is too short; what is the chance that both opponents have both longest slots?
                    if (dp.takenMin[0] == Partner || dp.takenMin[1] == Partner || dp.suitLength[1] < Aantal)
                    {		// no chance that opponents have both longest slots or that slot is too short
                    }
                    else
                    {   // the longest slots are large enough and not taken by partner
                        rKans += 0.3333 * dp.probability;    // 1/3 chance that opponents have the longest slots
                    }
                }
            }

            rKans = rKans / remainingProbability;
            return rKans;
        }

        /// <summary>
        /// Filter the distribution probabilities that can no longer exist given the current Beeld
        /// </summary>
        /// <param name="s">suit</param>
        /// <returns>A list of distribution probabilities that are still valid</returns>
        private static List<DistributionProbability> Aposteriori(Suits s, SpelersBeeld view, Seats Plaats)
        {
            int ownLength = view[Plaats].L[s].Min;
            var result = new List<DistributionProbability>();
            var dp = (DistributionProbability[])threeHiddenHands[ownLength].Clone();

            int firstEntry = 0;
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
            {
                if (seat != Plaats)
                {
                    int sumSideSuits = 0;
                    for (Suits sideSuit = Suits.Clubs; sideSuit <= Suits.Spades; sideSuit++)
                    {
                        if (sideSuit != s && view[seat].L[sideSuit].Min >= 4)
                        {
                            sumSideSuits += view[seat].L[sideSuit].Min;
                            //if (view[seat].L[sideSuit].Min >= 8) sumSideSuits++;
                        }
                    }

                    if (sumSideSuits >= 10 && dp.Length > 1)
                    {
                        //firstEntry = 1;
                        dp[0].probability /= 2;		// very unlikely that owner of 5-5 has a doubleton in this suit
                    }
                }
            }

            for (int entry = firstEntry; entry < dp.Length; entry++)
            {
                // find entries that match Beeld
                bool match = false;
                for (int l = 0; l < 3; l++)
                {
                    dp[entry].takenMin[l] = Plaats;
                    dp[entry].takenMax[l] = Plaats;
                }

                match = true;
                for (Seats seat = Seats.North; seat <= Seats.West; seat++)
                {
                    if (seat != Plaats)
                    {
                        if (view[seat].L[s].Min > dp[entry].suitLength[0])
                        {
                            match = false;
                            break;
                        }
                        else if (view[seat].L[s].Min > dp[entry].suitLength[1])
                        {
                            if (dp[entry].takenMin[0] != Plaats)
                            {
                                match = false;
                                break;
                            }
                            else
                            {
                                dp[entry].takenMin[0] = seat;
                                dp[entry].takenMax[0] = seat;
                                if (dp[entry].takenMin[1] != Plaats && dp[entry].takenMax[1] == Plaats && view[dp[entry].takenMin[1]].L[s].Min > dp[entry].suitLength[2]) dp[entry].takenMax[1] = dp[entry].takenMin[1];
                            }
                        }
                        else if (view[seat].L[s].Min > dp[entry].suitLength[2])
                        {
                            //if (dp[entry].takenMin[1] != Plaats && dp[entry].takenMin[0] != Plaats)
                            //{
                            //    match = false;
                            //    break;
                            //}
                            //else
                            //{   // 0 and 1 
                            //    dp[entry].takenMin[dp[entry].takenMin[1] == Plaats && dp[entry].takenMin[0] != seat ? 1 : 0] = seat;
                            //}

                            if (dp[entry].takenMin[1] == Plaats)
                            {
                                dp[entry].takenMin[1] = seat;
                                if (dp[entry].takenMin[0] != Plaats) dp[entry].takenMax[1] = seat;
                            }
                            else if (dp[entry].takenMin[0] == Plaats)
                            {
                                if (dp[entry].takenMax[1] == Plaats && view[seat].L[s].Min < dp[entry].suitLength[0])
                                {
                                    if (view[seat].L[s].Min >= view[dp[entry].takenMin[1]].L[s].Min && view[seat].L[s].Max > dp[entry].suitLength[1])
                                    {
                                        dp[entry].takenMin[0] = seat;
                                        //dp[entry].takenMax[0] = seat;
                                        //dp[entry].takenMax[1] = dp[entry].takenMin[1];
                                    }
                                    else
                                    {
                                        dp[entry].takenMin[0] = dp[entry].takenMin[1];
                                        //dp[entry].takenMax[0] = dp[entry].takenMin[1];
                                        dp[entry].takenMin[1] = seat;
                                        //dp[entry].takenMax[1] = seat;
                                    }
                                }
                                else
                                {
                                    dp[entry].takenMin[0] = seat;
                                    dp[entry].takenMax[0] = seat;
                                }
                            }
                            else
                            {
                                match = false;
                                break;
                            }
                        }
                        else if (view[seat].L[s].Min == view[seat].L[s].Max)
                        {
                            for (int i = 0; i <= 2; i++)
                            {
                                if (dp[entry].suitLength[i] == view[seat].L[s].Min && dp[entry].takenMin[i] == Plaats
                                        && dp[entry].takenMax[i] == Plaats
                                        && !(i < 2
                                                    && dp[entry].suitLength[i] == dp[entry].suitLength[i + 1]
                                                    && dp[entry].takenMin[i + 1] == Plaats
                                                    && dp[entry].takenMax[i + 1] == Plaats
                                                )
                                        )
                                {
                                    dp[entry].takenMin[i] = seat;
                                    dp[entry].takenMax[i] = seat;
                                    break;
                                }
                            }
                        }
                        else if (view[seat].L[s].Min == dp[entry].suitLength[2] && dp[entry].suitLength[2] > 0)
                        {
                            if (dp[entry].takenMin[1] == Plaats)
                            {
                                dp[entry].takenMin[1] = seat;
                            }
                            else
                            {
                                dp[entry].takenMin[2] = seat;
                                dp[entry].takenMax[2] = seat;
                            }
                        }
                        else
                        {
                            // this BiedInfo poses no claim on one of the lengths in this distribution
                        }

                        int probableMax = view[seat].L[s].Max;
                        if (probableMax <= 5 && view[seat].L[s].Min < probableMax)
                        {
                            int sumSideSuits = 0;
                            for (Suits sideSuit = Suits.Clubs; sideSuit <= Suits.Spades; sideSuit++)
                            {
                                if (sideSuit != s && view[seat].L[sideSuit].Min >= 4)
                                {
                                    sumSideSuits += view[seat].L[sideSuit].Min;
                                    //if (view[seat].L[sideSuit].Min >= 8) sumSideSuits++;
                                }
                            }
                            //if (sumSideSuits >= 8 && probableMax > 4 && view[seat].L[s].Min < probableMax) probableMax = 4;
                            if (sumSideSuits >= 9 && probableMax > 3 && view[seat].L[s].Min < probableMax) probableMax = 3;
                            if (sumSideSuits >= 10 && probableMax > 2 && view[seat].L[s].Min < probableMax) probableMax = 2;
                            if (sumSideSuits >= 11 && probableMax > 1 && view[seat].L[s].Min < probableMax) probableMax = 1;
                        }

                        if (probableMax < dp[entry].suitLength[2])
                        {
                            match = false;
                            break;
                        }
                        else
                        {
                            if (probableMax < dp[entry].suitLength[1])
                            {
                                if ((dp[entry].takenMax[2] != Plaats && dp[entry].takenMax[2] != seat && dp[entry].takenMax[1] != Plaats)
                                    || dp[entry].takenMin[0] == seat
                                    || dp[entry].takenMin[1] == seat
                                   )
                                {
                                    match = false;
                                    break;
                                }
                                else
                                {
                                    if (dp[entry].takenMax[1] == Plaats && dp[entry].takenMax[2] != seat)
                                    {
                                        dp[entry].takenMax[1] = dp[entry].takenMax[2];
                                    }
                                    dp[entry].takenMax[2] = seat;
                                    dp[entry].takenMin[2] = seat;
                                }
                            }
                            else
                            {
                                if (probableMax < dp[entry].suitLength[0])
                                {
                                    if ((dp[entry].takenMax[1] != Plaats && dp[entry].takenMax[2] != Plaats && dp[entry].takenMax[1] != seat && dp[entry].takenMax[2] != seat)
                                        || dp[entry].takenMax[0] == seat
                                       )
                                    {
                                        match = false;
                                        break;
                                    }
                                    else
                                    {
                                        if (dp[entry].takenMax[1] != Plaats && dp[entry].takenMax[2] != seat)
                                        {
                                            // first check if the current owner has an absolute preference for this seat
                                            if (dp[entry].takenMin[1] == dp[entry].takenMax[1])
                                            {
                                                dp[entry].takenMax[2] = seat;
                                            }
                                            else
                                            {   // there was no absolute preference
                                                // is there a difference between spot 1 and 2
                                                if (dp[entry].suitLength[1] == dp[entry].suitLength[2])
                                                {   // no difference so no preference
                                                    dp[entry].takenMax[1] = seat;
                                                }
                                                else
                                                {   // spot 1 and 2 mark different lengths, who should get which length?
                                                    if (probableMax > view[dp[entry].takenMax[2]].L[s].Max || view[seat].L[s].Min > dp[entry].suitLength[2])
                                                    {
                                                        dp[entry].takenMax[1] = seat;
                                                        if (dp[entry].takenMin[0] == seat)
                                                        {
                                                            dp[entry].takenMin[0] = dp[entry].takenMin[1];
                                                            dp[entry].takenMin[1] = seat;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        dp[entry].takenMax[1] = dp[entry].takenMax[2];
                                                        dp[entry].takenMax[2] = seat;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {		// 1 is free or 2 is already taken by seat
                                            if (dp[entry].takenMax[1] == Plaats && (dp[entry].takenMin[1] == seat || (dp[entry].takenMax[2] != Plaats && dp[entry].takenMax[2] != seat)))
                                            {
                                                dp[entry].takenMax[1] = seat;
                                            }
                                            else
                                            {
                                                dp[entry].takenMax[2] = seat;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // this BiedInfo poses no claim on one of the lengths in this distribution
                                }
                            }
                        }
                    }
                }

                if (match)
                {
                    //for (int l = 0; l < 3; l++)
                    //{
                    //  if (dp[entry].takenMin[l] != dp[entry].takenMax[l])
                    //  {
                    //    dp[entry].takenMin[l] = Plaats;
                    //    dp[entry].takenMax[l] = Plaats; // release this slot
                    //  }
                    //}
                    result.Add(dp[entry]);
                }
            }

            return result;
        }

        /// <summary>
        /// Distributions that are missing from this table have a probability of less than 0.5%
        /// </summary>
        private static DistributionProbability[][] threeHiddenHands = new DistributionProbability[][]
      {
        // self 0 cards
          new DistributionProbability[]
        {
            new DistributionProbability(6, 4, 3, 25.921)
          , new DistributionProbability(5, 4, 4, 24.301)
          , new DistributionProbability(5, 5, 3, 17.497)
          , new DistributionProbability(6, 5, 2, 12.725)
          , new DistributionProbability(7, 4, 2,  7.069)
          , new DistributionProbability(7, 3, 3,  5.184)
          , new DistributionProbability(8, 3, 2,  2.121)
          , new DistributionProbability(7, 5, 1,  2.121)
          , new DistributionProbability(6, 6, 1,  1.414)
          , new DistributionProbability(8, 4, 1,  0.884)		// 0.763 remaining; last entry from encyclopedia
          , new DistributionProbability(7, 6, 0,  0.432)		// 0.331 remaining; my estimation
          , new DistributionProbability(8, 5, 0,  0.243)		// 0.088 remaining; my estimation
          , new DistributionProbability(9, 4, 0,  0.075)		// 0.013 remaining; my estimation
          , new DistributionProbability(10, 3, 0,  0.012)		// 0.001 remaining; my estimation
          , new DistributionProbability(11, 2, 0,  0.001)		// 0.000 remaining; my estimation
        }
        // self 1 card
        , new DistributionProbability[]
        {
            new DistributionProbability(5, 4, 3, 40.377)
          , new DistributionProbability(6, 4, 2, 14.683)
          , new DistributionProbability(6, 3, 3, 10.767)
          , new DistributionProbability(5, 5, 2,  9.911)
          , new DistributionProbability(4, 4, 4,  9.347)
          , new DistributionProbability(7, 3, 2,  5.873)
          , new DistributionProbability(6, 5, 1,  4.405)
          , new DistributionProbability(7, 4, 1,  2.447)
          , new DistributionProbability(8, 3, 1,  0.734)
          , new DistributionProbability(8, 2, 2,  0.601)		// 0.855 remaining; last entry from encyclopedia
          , new DistributionProbability(7, 5, 0,  0.391)		// 0.464 remaining; my estimation
          , new DistributionProbability(6, 6, 0,  0.261)		// 0.203 remaining; my estimation
          , new DistributionProbability(8, 4, 0,  0.163)		// 0.040 remaining; my estimation
          , new DistributionProbability(9, 3, 0,  0.036)		// 0.004 remaining; my estimation
          , new DistributionProbability(10, 2, 0,  0.004)		// 0.000 remaining; my estimation
        }
        // self 2 card
        , new DistributionProbability[]
        {
            new DistributionProbability(4, 4, 3, 26.170)
          , new DistributionProbability(5, 4, 2, 25.695)
          , new DistributionProbability(5, 3, 3, 18.843)
          , new DistributionProbability(6, 3, 2, 13.704)
          , new DistributionProbability(6, 4, 1,  5.710)
          , new DistributionProbability(5, 5, 1,  3.854)
          , new DistributionProbability(7, 3, 1,  2.284)
          , new DistributionProbability(7, 2, 2,  1.869)
          , new DistributionProbability(6, 5, 0,  0.791)		// 1.080 remaining; last entry from encyclopedia
          , new DistributionProbability(8, 2, 1,  0.467)		// 0.613 remaining; my estimation
          , new DistributionProbability(7, 4, 0,  0.439)		// 0.174 remaining; my estimation
          , new DistributionProbability(8, 3, 0,  0.132)		// 0.042 remaining; my estimation
          , new DistributionProbability(9, 1, 1,  0.028)		// 0.014 remaining; my estimation
          , new DistributionProbability(9, 2, 0,  0.013)		// 0.001 remaining; my estimation
          , new DistributionProbability(10, 1, 0,  0.001)		// 0.000 remaining; my estimation
        }
        // self 3 card
        , new DistributionProbability[]
        {
            new DistributionProbability(4, 3, 3, 27.598)
          , new DistributionProbability(5, 3, 2, 27.096)
          , new DistributionProbability(4, 4, 2, 18.817)
          , new DistributionProbability(5, 4, 1, 11.290)
          , new DistributionProbability(6, 3, 1,  6.021)
          , new DistributionProbability(6, 2, 2,  4.927)
          , new DistributionProbability(7, 2, 1,  1.642)
          , new DistributionProbability(6, 4, 0,  1.158)
          , new DistributionProbability(5, 5, 0,  0.782)		// 0.669 remaining; last entry from encyclopedia
          , new DistributionProbability(7, 3, 0,  0.463)		// 0.203 remaining; my estimation
          , new DistributionProbability(8, 1, 1,  0.102)		// 0.101 remaining; my estimation
          , new DistributionProbability(8, 2, 0,  0.095)		// 0.006 remaining; my estimation
          , new DistributionProbability(9, 1, 0,  0.005)		// 0.001 remaining; my estimation
          , new DistributionProbability(10, 0, 0,  0.001)		// 0.000 remaining; my estimation
        }
        // self 4 card
        , new DistributionProbability[]
        {
            new DistributionProbability(4, 3, 2, 45.160)
          , new DistributionProbability(5, 3, 1, 13.548)
          , new DistributionProbability(5, 2, 2, 11.085)
          , new DistributionProbability(3, 3, 3, 11.039)
          , new DistributionProbability(4, 4, 1,  9.408)
          , new DistributionProbability(6, 2, 1,  4.927)
          , new DistributionProbability(5, 4, 0,  2.605)
          , new DistributionProbability(6, 3, 0,  1.390)		// 0.855 remaining; last entry from encyclopedia
          , new DistributionProbability(7, 1, 1,  0.411)		// 0.444 remaining; my estimation
          , new DistributionProbability(7, 2, 0,  0.379)		// 0.065 remaining; my estimation
          , new DistributionProbability(8, 1, 0,  0.047)		// 0.018 remaining; my estimation
          , new DistributionProbability(9, 0, 0,  0.018)		// 0.000 remaining; my estimation
        }
        // self 5 card
        , new DistributionProbability[]
        {
            new DistributionProbability(3, 3, 2, 31.110)
          , new DistributionProbability(4, 3, 1, 25.925)
          , new DistributionProbability(4, 2, 2, 21.212)
          , new DistributionProbability(5, 2, 1, 12.727)
          , new DistributionProbability(5, 3, 0,  3.590)
          , new DistributionProbability(4, 4, 0,  2.493)
          , new DistributionProbability(6, 1, 1,  1.414)
          , new DistributionProbability(6, 2, 0,  1.305)		// 0.224 remaining; last entry from encyclopedia
          , new DistributionProbability(7, 1, 0,  0.218)		// 0.006 remaining; my estimation
          , new DistributionProbability(8, 0, 0,  0.006)		// 0.000 remaining; my estimation
        }
        // self 6 card
        , new DistributionProbability[]
        {
            new DistributionProbability(3, 2, 2, 33.939)
          , new DistributionProbability(4, 2, 1, 28.282)
          , new DistributionProbability(3, 3, 1, 20.740)
          , new DistributionProbability(4, 3, 0,  7.977)
          , new DistributionProbability(5, 1, 1,  4.242)
          , new DistributionProbability(5, 2, 0,  3.916)
          , new DistributionProbability(6, 1, 0,  0.870)		// 0.034 remaining; last entry from encyclopedia
          , new DistributionProbability(7, 0, 0,  0.034)		// 0.000 remaining; my estimation
        }
        // self 7 card
        , new DistributionProbability[]
        {
            new DistributionProbability(3, 2, 1, 53.333)
          , new DistributionProbability(2, 2, 2, 14.545)
          , new DistributionProbability(4, 1, 1, 11.111)
          , new DistributionProbability(4, 2, 0, 10.256)
          , new DistributionProbability(3, 3, 0,  7.521)
          , new DistributionProbability(5, 1, 0,  3.077)		// 0.157 remaining; last entry from encyclopedia
          , new DistributionProbability(6, 0, 0,  0.157)		// 0.000 remaining; my estimation
        }
        // self 8 card
        , new DistributionProbability[]
        {
            new DistributionProbability(2, 2, 1, 41.211)
          , new DistributionProbability(3, 1, 1, 25.185)
          , new DistributionProbability(3, 2, 0, 23.247)
          , new DistributionProbability(4, 1, 0,  9.686)
          , new DistributionProbability(5, 0, 0,  0.671)
        }
        // self 9 card
        , new DistributionProbability[]
        {
            new DistributionProbability(2, 1, 1, 48.080)
          , new DistributionProbability(3, 1, 0, 27.122)
          , new DistributionProbability(2, 2, 0, 22.191)
          , new DistributionProbability(4, 0, 0,  2.607)
        }
        // self 10 card
        , new DistributionProbability[]
        {
            new DistributionProbability(2, 1, 0, 66.572)
          , new DistributionProbability(1, 1, 1, 24.040)
          , new DistributionProbability(3, 0, 0,  9.388)
        }
        // self 11 card
        , new DistributionProbability[]
        {
            new DistributionProbability(1, 1, 0, 68.421)
          , new DistributionProbability(2, 0, 0, 31.579)
        }
        // self 12 card
        , new DistributionProbability[]
        {
            new DistributionProbability(1, 0, 0, 100.00)
        }
        // self 13 card
        , new DistributionProbability[]
        {
            new DistributionProbability(0, 0, 0, 100.00)
        }
      };
    }

    public struct DistributionProbability
    {
        public int[] suitLength;
        public Seats[] takenMin;
        public Seats[] takenMax;
        public double probability;
        public DistributionProbability(int l1, int l2, int l3, double p)
        {
            suitLength = new int[3];
            suitLength[0] = l1;
            suitLength[1] = l2;
            suitLength[2] = l3;
            probability = p;
            takenMin = new Seats[3];
            takenMax = new Seats[3];
        }

        public override string ToString()
        {
            return string.Format("{0}-{1}-{2}: {3}", suitLength[0], suitLength[1], suitLength[2], probability);
        }
    }

    /// <summary>
    /// Static class for making several probability calculations
    /// All parameters and return values are based on doubles with values between 0 and 1
    /// </summary>
    public static class Probability
    {
        public static double Not(double p1)
        {
            if (p1 < 0.0 || p1 > 1.0) throw new ArgumentOutOfRangeException("p1", p1.ToString());
            return 1.0 - p1;
        }

        public static double And(double p1, double p2)
        {
            if (p1 < 0.0 || p1 > 1.0) throw new ArgumentOutOfRangeException("p1", p1.ToString());
            if (p2 < 0.0 || p2 > 1.0) throw new ArgumentOutOfRangeException("p2", p2.ToString());
            return p1 * p2;
        }

        public static double ExclusiveOr(double p1, double p2)
        {
            if (p1 < 0.0 || p1 > 1.0) throw new ArgumentOutOfRangeException("p1", p1.ToString());
            if (p2 < 0.0 || p2 > 1.0) throw new ArgumentOutOfRangeException("p2", p2.ToString());
            double result = p1 + p2;
            if (result > 1.0) throw new InvalidOperationException("p1 and p2 are not disjunct");
            return result;
        }

        public static double InclusiveOr(double p1, double p2, double p1and2)
        {
            if (p1 < 0.0 || p1 > 1.0) throw new ArgumentOutOfRangeException("p1", p1.ToString());
            if (p2 < 0.0 || p2 > 1.0) throw new ArgumentOutOfRangeException("p2", p2.ToString());
            double result = p1 + p2 - p1and2;
            if (result < 0.0 || result > 1.0) throw new InvalidOperationException("result==" + result.ToString());
            return result;
        }
    }
}
