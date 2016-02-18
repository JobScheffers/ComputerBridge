using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Sodes.Bridge.Base
{
    public delegate bool ConcludeFromNewInfo();

    public class SpelersBeeld : SeatCollection<SpelerBeeld>
    {
        public SpelersBeeld() : base(new SpelerBeeld[4] { new SpelerBeeld(), new SpelerBeeld(), new SpelerBeeld(), new SpelerBeeld() }) { }

        /// <summary>
        /// Constructor for unit testing
        /// </summary>
        /// <param name="fromTrace">A copy of a SpelersBeeld from a trace file</param>
        public SpelersBeeld(string fromTrace)
            : this()
        {
            /*
                hcp:   total    s     h     d     c   fitpoints
                  North 11-11 07-07 04-04 00-00 00-00
                  East  18-22 00-03 00-06 04-10 00-05 06-00
                  South 07-12 00-03 00-06 00-06 05-10 05-04
                  West  00-04 00-03 00-04 00-04 00-04
                length:         s     h     d     c 
                  North       06-06 02-02 04-04 01-01
                  East        00-07 00-07 06-09 00-06
                  South       00-07 00-07 00-03 06-12
                  West        00-07 00-11 00-03 00-06
             */
            int startPoints = fromTrace.IndexOf("hcp");
            if (startPoints < 0) throw new FatalBridgeException("SpelersBeeld(fromTrace): 'hcp' not found");
            int startLength = fromTrace.IndexOf("length");
            if (startLength < 0) throw new FatalBridgeException("SpelersBeeld(fromTrace): 'length' not found");

            string pointsFromTrace = fromTrace.Substring(startPoints + 6, startLength - (startPoints + 6));
            string lengthFromTrace = fromTrace.Substring(startLength + 6);

            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
            {
                int startSeatPoints = pointsFromTrace.IndexOf(seat.ToString());
                if (startSeatPoints < 0) throw new FatalBridgeException("SpelersBeeld(fromTrace): seat not found");
                int startSeatLength = lengthFromTrace.IndexOf(seat.ToString());
                if (startSeatLength < 0) throw new FatalBridgeException("SpelersBeeld(fromTrace): seat not found");

                this[seat].P.Min = int.Parse(pointsFromTrace.Substring(startSeatPoints + 6, 2));
                this[seat].P.Max = int.Parse(pointsFromTrace.Substring(startSeatPoints + 9, 2));
                if (pointsFromTrace[startSeatPoints + 35] == ' ')
                {
                    this[seat].Verdeling.Min = int.Parse(pointsFromTrace.Substring(startSeatPoints + 36, 2));
                    this[seat].Verdeling.Max = int.Parse(pointsFromTrace.Substring(startSeatPoints + 39, 2));
                }

                for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
                {
                    this[seat].PK[suit].Min = int.Parse(pointsFromTrace.Substring(startSeatPoints + 6 + 6 + 18 - 6 * (int)suit, 2));
                    this[seat].PK[suit].Max = int.Parse(pointsFromTrace.Substring(startSeatPoints + 6 + 9 + 18 - 6 * (int)suit, 2));
                    this[seat].L[suit].Min = int.Parse(lengthFromTrace.Substring(startSeatLength + 6 + 6 + 18 - 6 * (int)suit, 2));
                    this[seat].L[suit].Max = int.Parse(lengthFromTrace.Substring(startSeatLength + 6 + 9 + 18 - 6 * (int)suit, 2));
                }
            }
        }

        public void Leeg()
        {
            for (Seats s = Seats.North; s <= Seats.West; s++)
                this[s].Leeg();
        }

        public string Show(string after)
        {
            string info = "";
            info += "Kaartbeeld (na " + after + ")\r\n";
            info += "  hcp:   total    s     h     d     c   fitpoints\r\n";
            for (Seats sp = Seats.North; sp <= Seats.West; sp++)
            {
                info += "    ";
                info += sp.ToString().PadRight(5) + " " + this[sp].P.Min.ToString("00") + "-" + this[sp].P.Max.ToString("00");
                for (Suits cl = Suits.Spades; cl >= Suits.Clubs; cl--)
                {
                    info += " " + this[sp].PK[cl].Min.ToString("00") + "-" + this[sp].PK[cl].Max.ToString("00");
                }

                if (this[sp].Verdeling.Min > int.MinValue)
                    info += " " + this[sp].Verdeling.Min.ToString("00") + "-" + this[sp].Verdeling.Max.ToString("00");
                info += "\r\n";
            }

            info += "  length:         s     h     d     c \r\n";
            for (Seats sp = Seats.North; sp <= Seats.West; sp++)
            {
                info += "    ";
                info += sp.ToString().PadRight(11);
                for (Suits cl = Suits.Spades; cl >= Suits.Clubs; cl--)
                {
                    info += " " + this[sp].L[cl].Min.ToString("00") + "-" + this[sp].L[cl].Max.ToString("00");
                }
                info += "\r\n";
            }
            return info;
        }

        public void BaseUpon(SeatCollection<SpelerBeeld> baseInfo)
        {
            Leeg();
            for (Seats s = Seats.North; s <= Seats.West; s++)
                this[s].KGV(baseInfo[s]);
        }

        public SpelersBeeld Clone()
        {
            SpelersBeeld copy = new SpelersBeeld();
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
                copy[seat] = this[seat].Clone();
            return copy;
        }

        public double ProbableLength(Seats seat, Suits suit, Seats me)
        {
            if (seat == me) throw new ArgumentException("seat==me");
            return BridgeProbabilities.AverageLength(seat, suit, this, me);
        }

        /// <summary>
        /// Berekent het vermoedelijke aantal punten van [player] op een manier die
        /// NIET bij het interpreteren van een bod herhaalbaar is
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public double ProbablePoints2(Seats player, Seats me)
        {
            int unknownPoints = 40 - this[me].P.Min;
            double midPoint = unknownPoints / 3.0;
            double lowestMidPoint = double.MaxValue;
            int totalRange = 0;
            SeatCollection<double> dmp = new SeatCollection<double>();
            for (Seats p = Seats.North; p <= Seats.West; p++)
            {
                if (p != me)
                {
                    unknownPoints -= this[p].P.Min;
                    totalRange += this[p].P.Max - this[p].P.Min;
                    dmp[p] = midPoint - this[p].P.Min;
                    if (dmp[p] < lowestMidPoint) lowestMidPoint = dmp[p];
                }
            }

            var points = this[player].P;
            if (points.Min == 14 && (points.Max == 17 || points.Max == 18) && this[player].FitPoints.Min == 15) points.Min++;		// 1NT opening: almost always 15-17 hcp
            else if (points.Min == 19 && (points.Max == 22 || points.Max == 23) && this[player].FitPoints.Min == 20) points.Min++;		// 2NT opening: almost always 20-22 hcp

            if (totalRange == 0) return points.Min;

            double totalDmp = 0;
            for (Seats p = Seats.North; p <= Seats.West; p++)
            {
                dmp[p] += (2 - lowestMidPoint);
                if (this[p].P.Max > this[p].P.Min) totalDmp += dmp[p];
            }

            int range = points.Max - points.Min;
            //int othersRange = totalRange - range;

            double averagePart = 1.0 * (dmp[player] / totalDmp) * unknownPoints;
            if (averagePart > range) averagePart = range;
            //if (player == Partner && range >= 2 && 1.0 * averagePart / range > 0.6) averagePart *= 0.75;
            double p1 = points.Min + averagePart;
            return p1;
        }

        public double pHasPointsAndLength(Seats seat, Suits suit, int pointsNeeded, int lengthNeeded, Seats me)
        {
            if (seat == me) throw new ArgumentException("seat==me");
            //Debug.WriteLine("pHasPointsAndLength: seat={0} suit={1} pointsNeeded={2}", seat.ToString().PadRight(5), suit, pointsNeeded);
            double totalSpace = 0;
            var ruimte = new SeatCollection<double>();
            for (Seats p = Seats.North; p <= Seats.West; p++)
            {
                if (p != me)
                {
                    double pointSpace = 0;
                    if (pointsNeeded >= 1)
                    {
                        if (this[p].PK[suit].Max >= pointsNeeded)
                        {
                            pointSpace = Math.Pow(0.1 * (2 + this[p].PK[suit].Min) + 0.4 * this[p].PK[suit].Max + 0.35 * (0 + this[p].P.Min) + 0.15 * this[p].P.Max, 0.4 + 0.3 * pointsNeeded);
#if DEBUG
                            if (pointSpace < 0.0) throw new ArgumentOutOfRangeException("pointSpace", pointSpace.ToString());
#endif
                        }
                    }
                    else
                    {
                        pointSpace = 1;
                    }

                    double lengthSpace = 0;
                    if (this[p].L[suit].Max >= lengthNeeded)
                    {
                        lengthSpace = 1.0 * this[p].L[suit].Max + 0.9 * this[p].L[suit].Min;
#if DEBUG
                        if (lengthSpace < 0.0) throw new ArgumentOutOfRangeException("lengthSpace", lengthSpace.ToString());
#endif
                    }

                    ruimte[p] += pointSpace * lengthSpace;
                    //Debug.WriteLine("{0}: room={1:F1}*{2:F1}={3:F1}", p.ToString().PadRight(5), pointSpace, lengthSpace, ruimte[p]);
                }

                totalSpace += ruimte[p];
            }

            if (totalSpace == 0) return 0.5;		// no one has space for this card?????
            double kans = 1.0 * ruimte[seat] / totalSpace;
            //Debug.WriteLine("p={0:F3}", kans);
#if DEBUG
            if (kans < 0.0) throw new ArgumentOutOfRangeException("kans", kans.ToString());
#endif
            return kans;
        }
    }

    public class SpelerBeeld
    {
        public MinMaxSuits L = new MinMaxSuits(0, 13);
        public MinMaxSuits PK = new MinMaxSuits(0, 10);
        public MinMax P = new MinMax(0, 40);
        public MinMax Verdeling = new MinMax(int.MinValue, 0);
        public MinMaxSuits Losers = new MinMaxSuits(0, 13);
        public bool?[] KeyCards = new bool?[6];
        public Suits Trump;

        public SpelerBeeld()
        {
            this.Trump = Suits.NoTrump;
        }

        public void KGV(SpelerBeeld newBidInfo)
        {
            this.KGV(newBidInfo, null);
        }

        public string KGV(SpelerBeeld newBidInfo, ConcludeFromNewInfo concluder)
        {
            string error = "";
            string myError = "";

            for (Suits Kleur = Suits.Clubs; Kleur <= Suits.Spades; Kleur++)
            {
                error = L[Kleur].KGV(newBidInfo.L[Kleur], concluder);
                if (error.Length > 0) 
                    myError += "L[" + SuitHelper.ToXML(Kleur) + "]." + error;

                error = PK[Kleur].KGV(newBidInfo.PK[Kleur], concluder);
                if (error.Length > 0) myError += "PK[" + SuitHelper.ToXML(Kleur) + "]." + error;

                error = Losers[Kleur].KGV(newBidInfo.Losers[Kleur], concluder);
                if (error.Length > 0) myError += "Losers[" + SuitHelper.ToXML(Kleur) + "]." + error;
            }

            if (newBidInfo.Verdeling.Min > int.MinValue)
            {
#if DEBUG
                //if (newBidInfo.Verdeling.Min < -1
                //    || newBidInfo.Verdeling.Min > 6
                //    || newBidInfo.Verdeling.Max < -1
                //    || newBidInfo.Verdeling.Max > 6
                //   ) System.Diagnostics.Debugger.Break();
#endif
                MinMax fitPoints = new MinMax(newBidInfo.FitPoints.Min, newBidInfo.FitPoints.Max);
                if (this.Verdeling.Min > int.MinValue && this.Trump != Suits.NoTrump)
                {
                    if (this.FitPoints.Min > fitPoints.Min) fitPoints.Min = this.FitPoints.Min;
                    if (this.FitPoints.Max < fitPoints.Max) fitPoints.Max = this.FitPoints.Max;
                }
                
                if (newBidInfo.Trump != this.Trump)
                {
                    if (this.Trump == Suits.NoTrump && this.Verdeling.Min > int.MinValue) this.Verdeling.Min += 5;
                    this.Trump = newBidInfo.Trump;
                    this.Verdeling.Min = Math.Max(this.Verdeling.Min, newBidInfo.Verdeling.Min);
                    this.Verdeling.Max = Math.Max(this.Verdeling.Max, newBidInfo.Verdeling.Max);
                }
                else
                {
                    if (this.Verdeling.Min > int.MinValue) fitPoints.KGV(this.FitPoints);
                }

                error = this.P.KGV(newBidInfo.P, concluder);		// intersection of hcp's
                if (error.Length > 0) myError += "P." + error;

                // how to make hcp's and fitpoints match again?
                if (fitPoints.Min != this.P.Min || fitPoints.Max != this.P.Max)
                {
                    this.Verdeling.Min = fitPoints.Max - this.P.Max;
                    if (this.P.Min < fitPoints.Min)
                    {
                        this.Verdeling.Max = fitPoints.Min - this.P.Min;
                    }
                    else
                    {		// never decrease already showed hcp's (1H x xx(=9+) 2D 2S 3D 3H(8-12fp))
                        //this.Verdeling.Max = 0;
                    }

                    if (this.Verdeling.Min > 6) this.Verdeling.Min = 6;
#if DEBUG
                    //if (this.Verdeling.Min < -1
                    //    || this.Verdeling.Min > 6
                    //    || this.Verdeling.Max < -1
                    //    || this.Verdeling.Max > 6
                    //   ) System.Diagnostics.Debugger.Break();
#endif
                }
                else
                {
                    Verdeling.Min = int.MinValue;
                }
            }
            else
            {
                if (this.Verdeling.Min == int.MinValue 
                    //|| (newBidInfo.Trump == Suits.NoTrump && newBidInfo.Verdeling.Min == int.MinValue)
                    )
                { // no need to cope with fitpoints
                    error = this.P.KGV(newBidInfo.P, concluder);		// intersection of hcp's
                    if (error.Length > 0) myError += "P." + error;
                }
                else
                {
                    MinMax fitPoints = new MinMax(this.FitPoints.Min, this.FitPoints.Max);
                    error = this.P.KGV(newBidInfo.P, concluder);		// intersection of hcp's
                    if (error.Length > 0) myError += "P." + error;

                    // how to make hcp's and fitpoints match again?
                    if (fitPoints.Min != this.P.Min || fitPoints.Max != this.P.Max)
                    {
                        this.Verdeling.Min = fitPoints.Max - this.P.Max;
                        if (this.Verdeling.Min > 5) this.Verdeling.Min = 5;
                        if (this.Trump != Suits.NoTrump) this.Verdeling.Max = fitPoints.Min - this.P.Min;
                    }
                }
            }

            // shift hcp's to fitpoints in case of extreme distributions
            if (this.Verdeling.Min > int.MinValue && this.P.Min < this.P.Max && this.Verdeling.Max == 3)
            {
                int distributionality = 0;
                for (Suits Kleur = Suits.Clubs; Kleur <= Suits.Spades; Kleur++)
                {
                    if (this.L[Kleur].Min >= 5)
                    {
                        distributionality += (this.L[Kleur].Min - 2);
                    }
                    else
                    {
                        if (this.L[Kleur].Min >= 2)
                        {
                            if (this.L[Kleur].Max <= 4) distributionality -= 2;
                        }
                        else
                        {
                            if (this.L[Kleur].Min == 0 && this.L[Kleur].Max <= 8) distributionality += 1;		// potential void
                        }
                    }

                    if (this.L[Kleur].Max <= 2) distributionality += (3 - this.L[Kleur].Max);
                }

                // higher distributionality means more extreme distribution and thus more fitpoints
                // 2..13 2..13 5.. 5 2.. 2 =   0
                // 2..13 2..13 2..13 2..13 =  -4
                // 0..13 7..13 0..13 0..13 =   7
                // 0..13 5..13 5..13 0..13 =   4
                if (distributionality >= 7)
                {
                    switch (distributionality)
                    {
                        case 7:
                            if (this.Verdeling.Max <= 4 && this.P.Min >= 1)
                            {
                                this.Verdeling.Max += 1;
                                this.P.Min -= 1;
#if DEBUG
                                if (this.P.Min < 0) throw new InvalidOperationException();
#endif
                            }
                            break;
                        default:
                            if (this.Verdeling.Max <= 3 && this.P.Min >= 2)
                            {
                                this.Verdeling.Max += 2;
                                this.P.Min -= 2;
#if DEBUG
                                if (this.P.Min < 0) throw new InvalidOperationException();
#endif
                            }
                            break;
                    }
                }
                else
                {
                    if (distributionality < 0)
                    {		// very even distribution, not too many fp's
                        if (distributionality < -3)
                        {		// very even distribution, not too many fp's
                            if (this.Verdeling.Max >= 2 && this.P.Min + 2 <= this.P.Max)
                            {
                                this.Verdeling.Max -= 1;
                                this.P.Min += 1;
                            }
                        }
                        else
                        {
                            if (distributionality <= -3)
                            {		// very even distribution, not too many fp's
                                if (this.Verdeling.Max >= 1 && this.P.Min + 1 <= this.P.Max)
                                {
                                    this.Verdeling.Max -= 1;
                                    this.P.Min += 1;
                                }
                            }
                        }
                    }
                }
            }
            else if (this.Verdeling.Min == 3
                        && this.P.Max - P.Min <= 2
                        && this.L[Suits.Clubs].Min >= 2
                        && this.L[Suits.Diamonds].Min >= 2
                        && this.L[Suits.Hearts].Min >= 2
                        && this.L[Suits.Spades].Min >= 2
                    )   // partner bid NT; can't have much distribution points
            {
                this.Verdeling.Min = 0;
            }

            return myError;
        }

        //----------------------------------------------------------------------------------
        public void Invert(SpelerBeeld pGrens)
        {
            int AantalGevuld = P.Invert(pGrens.P);
            for (Suits Kleur = Suits.Clubs; Kleur <= Suits.Spades; Kleur++)
            {
                AantalGevuld += L[Kleur].Invert(pGrens.L[Kleur]);
                AantalGevuld += PK[Kleur].Invert(pGrens.PK[Kleur]);
            }

            if (AantalGevuld > 1) Leeg();
        }

        private byte MaxPuntenInHand = 40;
        public void Leeg()
        {
            P.Min = 0;
            P.Max = MaxPuntenInHand;
            Verdeling.Min = int.MinValue;
            Verdeling.Max = 0;
            for (Suits Kleur = Suits.Clubs; Kleur <= Suits.Spades; Kleur++)
            {
                L[Kleur].Min = 0;
                L[Kleur].Max = 13;
                PK[Kleur].Min = 0;
                PK[Kleur].Max = 10;
                Losers[Kleur].Min = 0;
                Losers[Kleur].Max = 10;
            }
            L[Suits.NoTrump].Min = 0;
            L[Suits.NoTrump].Max = 0;
            PK[Suits.NoTrump].Min = 0;
            PK[Suits.NoTrump].Max = 0;
            Losers[Suits.NoTrump].Min = 0;
            Losers[Suits.NoTrump].Max = 0;
            for (int i = 0; i < 6; i++)
            {
                this.KeyCards[i] = null;
            }
        }
        //----------------------------------------------------------------------------------
        public MinMax FitPoints
        {
            get
            {
                MinMax result = new MinMax(this.P.Min, this.P.Max);
                if (this.Verdeling.Min > int.MinValue)
                {
                    result.Min += this.Verdeling.Max;
                    result.Max += this.Verdeling.Min;
                    result.Sort();
                }

                return result;
            }
        }

        public void ToonVerdeling(SpelerBeeld bidInfo, Suits _trump, bool overcall)
        {
            this.Trump = _trump;
            if (this.P.Min == 0 && bidInfo.P.Min == 12) this.P.Min = 12;		// weakest fit bid after an opening: pb0019
            this.Verdeling.Min = -1;
            this.Verdeling.Max = overcall ? 1 : _trump == Suits.NoTrump ? 1 : bidInfo.P.Min >= 12 || this.P.Min >= 11 ? 4 : 3;
            this.P.Min -= this.Verdeling.Max;
            this.P.Max -= this.Verdeling.Min;
            this.P.Sort();

            while (this.P.Min > bidInfo.P.Max)
            {
                this.P.Min--;
                this.Verdeling.Min++;
                this.Verdeling.Max++;
            }

            while (this.P.Max < bidInfo.P.Min)
            {
                this.P.Max++;
                this.Verdeling.Min--;
                this.Verdeling.Max--;
            }

            while (bidInfo.P.Min > this.P.Min)
            {
                this.P.Min++;
                this.Verdeling.Max--;
                //this.Verdeling.Min--;
                //if (this.Verdeling.Max - (bidInfo.P.Min - this.P.Min) >= 0)
                //  this.Verdeling.Max -= bidInfo.P.Min - this.P.Min;
                //else
                //  this.Verdeling.Max = 0;
            }

            while (bidInfo.P.Max < this.P.Max && this.P.Max < 30 && bidInfo.P.Max < 30)
            {
                this.P.Max--;
                this.Verdeling.Min++;
                //this.Verdeling.Max++;
                //if (this.Verdeling.Min - (bidInfo.P.Max - this.P.Max) <= 0)
                //  this.Verdeling.Min -= bidInfo.P.Max - this.P.Max;
                //else
                //  this.Verdeling.Min = 0;
            }

            //this.Verdeling.Sort();
            if (this.Verdeling.Min < -1) this.Verdeling.Min = -1;
            if (this.Verdeling.Max < -1) this.Verdeling.Max = -1;
            if (this.Verdeling.Min > 6) this.Verdeling.Min = 6;
            if (this.Verdeling.Max > 6) this.Verdeling.Max = 6;
            // pb0015 after 1D p 1S p 2S
            // sS2550 after 2NT p 3H p 3S p 4S
        }

        public SpelerBeeld Clone()
        {
            SpelerBeeld copy = new SpelerBeeld();
            copy.L = this.L.Clone();
            copy.PK = this.PK.Clone();
            copy.Losers = this.Losers.Clone();
            copy.P = this.P.Clone();
            copy.Verdeling = this.Verdeling.Clone();
            for (int i = 0; i < 6; i++)
            {
                copy.KeyCards[i] = this.KeyCards[i];
            }
            return copy;
        }
        public override string ToString()
        {
            return string.Format(@"L:  {0}
PK: {1}
P:  {2}", L, PK, P);
        }

        public string ToHuman()
        {
            string result = string.Empty;
            MinMax p = this.P;
            string points = "points";
            if (Verdeling.Min > int.MinValue)
            {
                p = this.FitPoints;
                if (this.Trump < Suits.NoTrump)
                {
                    points = "fit-points";
                }
            }

            if (p.Min > 0 && p.Max < 25)
            {
                if (p.Min == p.Max)
                {
                    result += "Excatly " + p.Max + " " + points + ". ";
                }
                else
                {
                    result += p.Min + "-" + p.Max + " " + points + ". ";
                }
            }
            else
            {
                if (p.Min > 0) result += "At least " + p.Min + " " + points + ". ";
                if (p.Max < 25) result += "At most " + p.Max + " " + points + ". ";
            }

            for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
            {
                if (this.L[suit].Min > 0 && this.L[suit].Max < 6)
                {
                    if (this.L[suit].Min == this.L[suit].Max)
                    {
                        result += "Exactly " + this.L[suit].Max + SuitHelper.ToXML(suit) + ". ";
                    }
                    else
                    {
                        result += this.L[suit].Min + "-" + this.L[suit].Max + SuitHelper.ToXML(suit) + ". ";
                    }
                }
                else
                {
                    if (this.L[suit].Min > 0)
                        result += "At least " + this.L[suit].Min + SuitHelper.ToXML(suit) + ". ";
                    if (this.L[suit].Max < 6)
                        result += "At most " + this.L[suit].Max + SuitHelper.ToXML(suit) + ". ";
                }
            }

            return result.Trim();
        }
    }
    //================================================================================
    public class MinMaxSuits : SuitCollection<MinMax>
    {
        public MinMaxSuits(int _min, int _max) : base(new MinMax[] { new MinMax(_min, _max), new MinMax(_min, _max), new MinMax(_min, _max), new MinMax(_min, _max), new MinMax(_min, _max) }) { }

        public MinMaxSuits Clone()
        {
            MinMaxSuits copy = new MinMaxSuits(0, 100);
            for (Suits s = Suits.Clubs; s <= Suits.NoTrump; s++)
                copy[s] = this[s].Clone();
            return copy;
        }

        public override string ToString()
        {
            string r = "";
            for (Suits s = Suits.Clubs; s <= Suits.NoTrump; s++)
                r += string.Format("{0} ", this[s]);
            return r;
        }

    }
    //================================================================================
    public class MinMax
    {
        public int Min;
        public int Max;
        //public MinMax()
        //{
        //  Min = 0;
        //  Max = int.MaxValue;
        //}
        public MinMax(int _min, int _max)
        {
            Min = _min;
            Max = _max;
        }

        public void KGV(MinMax newInfo)
        {
            this.KGV(newInfo, null);
        }

        internal string KGV(MinMax newInfo, ConcludeFromNewInfo errorInConcluder)
        {
            /*  Deze proc gaat er van uit dat het aangeven van punten en lengte
                    alleen maar nauwkeuriger wordt. Als er een Min of Max
                    wordt aangegeven dat buiten een eerder bereik ligt, dan ga ik
                    er van uit dat er gelogen wordt.
                    Dus eerst 12..19 en daarna 20..20 kan niet; dat zijn verdelings-
                    punten, en daarin ben ik niet zo geinteresseerd    */

            string error = "";
            int newMin = newInfo.Min;
            int newMax = newInfo.Max;

            if (newMin > this.Max)
            {
                newMin = this.Max;
                error += "Min=" + newMin + ";";
            }
            int oldMin = this.Min;
            while (newMin > this.Min)
            {
                this.Min = newMin;
                if (errorInConcluder != null && errorInConcluder())
                {
                    this.Min = oldMin;
                    newMin--;
                    error += "Min=" + newMin + ";";
                }
                else
                    break;
            }

            if (newMax < this.Min)
            {
                newMax = this.Min;
                error += "Max=" + newMax + ";";
            }
            int oldMax = this.Max;
            while (newMax < this.Max)
            {
                this.Max = newMax;
                if (errorInConcluder != null && errorInConcluder())
                {
                    this.Max = oldMax;
                    newMax++;
                    error += "Max=" + newMax + ";";
                }
                else
                    break;
            }

            return error;
        }

        public void Vereniging(MinMax P)
        {
            this.Vereniging(P.Min, P.Max);
        }

        //  Deze proc gaat er van uit dat het aangeven van verdelingspunten alleen maar breder wordt.
        public void Vereniging(int newMin, int newMax)
        {
            if (newMax < this.Min || newMin > this.Max || this.Min == int.MinValue)
            {		// when can this occur? When verdeling is set the first time
                // Verdeling: oud:0-0 nieuw -1--1
                this.Min = newMin;
                this.Max = newMax;
            }
            else
            {
                if (newMin < this.Min) this.Min = newMin;
                if (newMax > this.Max) this.Max = newMax;
            }
        }

        /// <summary>
        /// Swaps Min and Max when Min > Max
        /// </summary>
        public void Sort()
        {
            if (this.Min > this.Max) this.Swap();
        }

        private void Swap()
        {
            int t = this.Min;
            this.Min = this.Max;
            this.Max = t;
        }

        internal int Invert(MinMax pGrens)
        {
            int AantalGevuld = 0;
            if (Min > pGrens.Min && Max >= pGrens.Max)
            {
                Max = Min - 1;
                Min = pGrens.Min;
                AantalGevuld++;
            }
            else if (Min <= pGrens.Min && Max < pGrens.Max)
            {
                Min = Max + 1;
                Max = pGrens.Max;
                AantalGevuld++;
            }
            else if (Min > pGrens.Min && Max < pGrens.Max)
            {
                AantalGevuld += 2;
            }

            return AantalGevuld;
        }

        public MinMax Clone() { return new MinMax(Min, Max); }

        public override string ToString()
        {
            return string.Format("{0}-{1}", this.Min, this.Max);
        }

        /// <summary>Operator</summary>
        /// <param name="b1">First bid to compare</param>
        /// <param name="b2">Second bid to compare</param>
        /// <returns>boolean</returns>
        public static bool operator ==(MinMax b1, MinMax b2)
        {
            //return (Object.Equals(b1, null) && Object.Equals(b2, null))
            //  || (!Object.Equals(b2, null) && !Object.Equals(b1, null) && b1.Level == b2.Level && b1.Suit == b2.Suit);
            return b1.Min == b2.Min && b1.Max == b2.Max;
        }

        /// <summary>Operator</summary>
        /// <param name="b1">First bid to compare</param>
        /// <param name="b2">Second bid to compare</param>
        /// <returns>boolean</returns>
        public static bool operator !=(MinMax b1, MinMax b2)
        {
            return !(b1 == b2);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /// <summary>Dedicated exception class for KaartBeeld</summary>
    public class KaartBeeldException : FatalBridgeException
    {
        /// <summary>Constructor</summary>
        public KaartBeeldException(string format, params object[] args)
            : base(format, args)
        {
        }
    }
}
