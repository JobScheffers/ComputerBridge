using System;
using System.Diagnostics;
using System.Text;
using Bridge.NonBridgeHelpers;

namespace Bridge
{
    public class SuitCollection<T>
    {
        private T[] x = new T[5];

        public SuitCollection()
        {
        }

        public SuitCollection(T initialValue)
        {
            this.Set(initialValue);
        }

        public SuitCollection(T[] initialValues)
        {
            for (Suits s = Suits.Clubs; s <= Suits.NoTrump; s++)
                this[s] = initialValues[(int)s];
        }

        public T this[Suits index]
        {
            [DebuggerStepThrough]
            get
            {
                return x[(int)index];
            }
            [DebuggerStepThrough]
            set
            {
                x[(int)index] = value;
            }
        }

        public T this[int index]
        {
            [DebuggerStepThrough]
            get
            {
                return x[index];
            }
            [DebuggerStepThrough]
            set
            {
                x[index] = value;
            }
        }

        public void Set(T value)
        {
            for (Suits s = Suits.Clubs; s <= Suits.NoTrump; s++)
                this[s] = value;
        }

        //public SuitCollection<T> Clone()
        //{
        //  SuitCollection<T> result = new SuitCollection<T>();
        //  for (Suits s = Suits.Clubs; s <= Suits.NoTrump; s++)
        //  {
        //    result[s] = this[s];
        //  }

        //  return result;
        //}
    }

    /*
    Benchmark results (release build)
    SuitRankCollection<byte>: read/write[suit,rank] : 2,0856187E-07
    SuitRankCollection<byte>: read/write[int ,int ] : 1,6549976E-07
    SuitRankCollection<byte>: Clone                 : 1,2989967E-07
    SuitRankCollectionInt   : read/write[suit,rank] : 1,6015011E-07
    SuitRankCollectionInt   : read/write[int ,int ] : 1,6185007E-07
    SuitRankCollectionInt   : Clone                 : 8,090013E-08
    SuitRankCollection<int> : read/write[suit,rank] : 1,7444964E-07
    SuitRankCollection<int> : read/write[int ,int ] : 1,4554973E-07
    SuitRankCollection<int> : read/write[int      ] : 1,437499E-07
    SuitRankCollection<int> : Clone                 : 1,6225647E-07

    */

    public unsafe struct Deal
    {
        private fixed ushort data[13];

        public bool this[Seats seat, Suits suit, Ranks rank]
        {
            get
            {
                return this[(int)seat, (int)suit, (int)rank];
            }
            set
            {
                this[(int)seat, (int)suit, (int)rank] = value;
            }
        }

        private unsafe bool this[int seat, int suit, int rank]
        {
            get
            {
                //Debug.WriteLine($"{((Seats)seat).ToXML()}{((Suits)suit).ToXML()}{((Ranks)rank).ToXML()}? {Convert.ToString(data[rank], 2)} {Convert.ToString((1 << (4 * seat + suit)), 2)}");
                return (data[rank] & (1 << (4 * seat + suit))) > 0;
            }
            set
            {
                //Debug.WriteLine($"{((Seats)seat).ToXML()}{((Suits)suit).ToXML()}{((Ranks)rank).ToXML()}={value} {Convert.ToString(data[rank], 2)} {Convert.ToString((1 << (4 * seat + suit)), 2)}");
                if (value)
                {
                    data[rank] |= (ushort)(1 << (4 * seat + suit));
                }
                else
                {
                    data[rank] &= (ushort)(ushort.MaxValue - (1 << (4 * seat + suit)));
                }
                //Debug.WriteLine($"{Convert.ToString(data[rank], 2)} {Convert.ToString((1 << (4 * seat + suit)), 2)}");
            }
        }

        [DebuggerStepThrough]
#if NET6_0_OR_GREATER
        public Deal(ref readonly string pbnDeal)
#else
        public Deal(ref string pbnDeal)
#endif
        {
            var firstHand = pbnDeal[0];
#if NET6_0_OR_GREATER
            var hands = pbnDeal[2..].Split2(' ');
#else
            var hands = pbnDeal.Substring(2).Split(' ');
#endif
            var hand = HandFromPbn(in firstHand);
            foreach (var handHolding in hands)
            {
#if NET6_0_OR_GREATER
                var suits = handHolding.Line.Split2('.');
#else
                var suits = handHolding.Split('.');
#endif
                int pbnSuit = 1;
                foreach (var suitHolding in suits)
                {
#if NET6_0_OR_GREATER
                    var suitCards = suitHolding.Line;
#else
                    var suitCards = suitHolding;
#endif
                    var suitLength = suitCards.Length;
                    var suit = SuitFromPbn(pbnSuit);
                    for (int r = 0; r < suitLength; r++)
                    {
#if NET6_0_OR_GREATER
                        var rank = RankFromPbn(in suitCards[r]);
#else
                        var x = suitCards[r];
                        var rank = RankFromPbn(in x);
#endif
                        this[hand, suit, rank] = true;
                    }
                    pbnSuit++;
                }

                hand = NextHandPbn(hand);
            }

            [DebuggerStepThrough]
            static Seats HandFromPbn(ref readonly Char hand)
            {
                switch (hand)
                {
                    case 'n':
                    case 'N': return Seats.North;
                    case 'e':
                    case 'E': return Seats.East;
                    case 's':
                    case 'S': return Seats.South;
                    case 'w':
                    case 'W': return Seats.West;
                    default: throw new ArgumentOutOfRangeException(nameof(hand), $"unknown {hand}");
                }
            }

            [DebuggerStepThrough]
            static Seats NextHandPbn(Seats hand)
            {
                switch (hand)
                {
                    case Seats.North: return Seats.East;
                    case Seats.East: return Seats.South;
                    case Seats.South: return Seats.West;
                    case Seats.West: return Seats.North;
                    default: throw new ArgumentOutOfRangeException(nameof(hand), $"unknown {hand}");
                }
            }

            [DebuggerStepThrough]
            static Suits SuitFromPbn(int relativeSuit)
            {
                switch (relativeSuit)
                {
                    case 1: return Suits.Spades;
                    case 2: return Suits.Hearts;
                    case 3: return Suits.Diamonds;
                    case 4: return Suits.Clubs;
                    default: throw new ArgumentOutOfRangeException(nameof(relativeSuit), $"unknown {relativeSuit}");
                }
            }

            [DebuggerStepThrough]
            static Ranks RankFromPbn(ref readonly Char rank)
            {
                switch (rank)
                {
                    case 'a':
                    case 'A': return Ranks.Ace;
                    case 'k':
                    case 'h':
                    case 'H':
                    case 'K': return Ranks.King;
                    case 'q':
                    case 'Q': return Ranks.Queen;
                    case 'j':
                    case 'b':
                    case 'B':
                    case 'J': return Ranks.Jack;
                    case 't':
                    case 'T': return Ranks.Ten;
                    case '9': return Ranks.Nine;
                    case '8': return Ranks.Eight;
                    case '7': return Ranks.Seven;
                    case '6': return Ranks.Six;
                    case '5': return Ranks.Five;
                    case '4': return Ranks.Four;
                    case '3': return Ranks.Three;
                    case '2': return Ranks.Two;
                    default: throw new ArgumentOutOfRangeException(nameof(rank), $"unknown {rank}");
                }
            }
        }

        public void Clear()
        {
            for (int i = 0; i <= 12; i++) this.data[i] = 0;
        }

        public string ToPBN()
        {
            var result = new StringBuilder(70);
            result.Append("N:");
            for (Seats hand = Seats.North; hand <= Seats.West; hand++)
            {
                for (Suits suit = Suits.Spades; suit >= Suits.Clubs; suit--)
                {
                    for (Ranks rank = Ranks.Ace; rank >= Ranks.Two; rank--)
                    {
                        if (this[hand, suit, rank])
                        {
                            result.Append(RankToPbn(rank));
                        }
                    };

                    if (suit != Suits.Clubs) result.Append(".");
                };

                if (hand != Seats.West) result.Append(" ");
            };

            return result.ToString();

            static string RankToPbn(Ranks rank)
            {
                switch (rank)
                {
                    case Ranks.Ace: return "A";
                    case Ranks.King: return "K";
                    case Ranks.Queen: return "Q";
                    case Ranks.Jack: return "J";
                    case Ranks.Ten: return "T";
                    case Ranks.Nine: return "9";
                    case Ranks.Eight: return "8";
                    case Ranks.Seven: return "7";
                    case Ranks.Six: return "6";
                    case Ranks.Five: return "5";
                    case Ranks.Four: return "4";
                    case Ranks.Three: return "3";
                    case Ranks.Two: return "2";
                    default: throw new ArgumentOutOfRangeException(nameof(rank), $"unknown {rank}");
                }
            }
        }
    }

    /// <summary>
    /// This specific version of a SuitRankCollection is a fraction faster in cloning, uses bytes to store data while allowing int in the interface
    /// </summary>
    [Obsolete]
    public class SuitRankCollectionInt
    {
        private SuitsRanksArrayOfInt x;

        public SuitRankCollectionInt()
        {
        }

        public SuitRankCollectionInt(int initialValue)
            : this()
        {
            for (Suits s = Suits.Clubs; s <= Suits.Spades; s++)
            {
                this.Init(s, initialValue);
            }
        }

        public int this[Suits suit, Ranks rank]
        {
            get
            {
                return x[suit, rank];
            }
            set
            {
                x[suit, rank] = value;
            }
        }

        public int this[int suit, int rank]
        {
            get
            {
                return x[(Suits)suit, (Ranks)rank];
            }
            set
            {
                x[(Suits)suit, (Ranks)rank] = value;
            }
        }

        public void Init(Suits suit, int value)
        {
            for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
            {
                this.x[suit, r] = value;
            }
        }

        public SuitRankCollectionInt Clone()
        {
            var result = new SuitRankCollectionInt();
            result.x = this.x;
            return result;
        }
    }

    public class SuitRankCollection<T>
    {
        private T[] x = new T[52];
        private int typeSize = -1;

        public SuitRankCollection()
        {
            var typeName = typeof(T).Name;
            if (typeName == "Int32") typeSize = 4 * 52;
            else if (typeName == "Int16") typeSize = 2 * 52;
            else if (typeName == "Byte") typeSize = 1 * 52;
        }

        private SuitRankCollection(int size)
        {
            typeSize = size;
        }

        public SuitRankCollection(T initialValue)
            : this()
        {
            for (Suits s = Suits.Clubs; s <= Suits.Spades; s++)
            {
                this.Init(s, initialValue);
            }
        }

        public T this[Suits suit, Ranks rank]
        {
            get
            {
                return x[13 * (int)suit + (int)rank];
            }
            set
            {
                x[13 * (int)suit + (int)rank] = value;
            }
        }

        public T this[int suit, int rank]
        {
            get
            {
                return x[13 * suit + rank];
            }
            set
            {
                x[13 * suit + rank] = value;
            }
        }

        public T this[int suitRank]
        {
            get
            {
                return x[suitRank];
            }
            set
            {
                x[suitRank] = value;
            }
        }

        private void Init(Suits suit, T value)
        {
            int _s = 13 * (int)suit;
            for (int r = RankHelper.Two; r <= RankHelper.Ace; r++)
            {
                this.x[_s + r] = value;
            }
        }

        public SuitRankCollection<T> Clone()
        {
            SuitRankCollection<T> result = new SuitRankCollection<T>(this.typeSize);

            if (this.typeSize > 0)
            {
                System.Buffer.BlockCopy(this.x, 0, result.x, 0, typeSize);
            }
            else
            {
                //this.x.CopyTo(result.x, 0);
                //Array.Copy(this.x, result.x, 52);
                for (int s = 0; s <= 3; s++)
                {
                    int _s = 13 * s;
                    for (int r = 0; r <= 12; r++)
                    {
                        int i = _s + r;
                        result.x[i] = this.x[i];
                    }
                }
            }

            return result;
        }
    }

    [DebuggerDisplay("{DisplayValue}")]
    public unsafe struct SuitsRanksArrayOfRanks
    {
        private fixed byte data[52];

        public unsafe Ranks this[Suits suit, Ranks rank]
        {
            get
            {
                var rawValue = this.data[(int)suit | ((int)rank << 2)];
                // trick for coping with negatives in byte (-127..128)
                var value = (Ranks)((int)rawValue - 128);
                return value;
            }
            set
            {
                this.data[(int)suit | ((int)rank << 2)] = (byte)(value + 128);
            }
        }

        public unsafe void Fill(Ranks value)
        {
            var v = (byte)(value + 128);
            for (int i = 0; i < 52; i++)
            {
                data[i] = v;
            }
        }

        /// <summary>
        /// replace a value in the array and return the old value
        /// </summary>
        /// <returns>old value</returns>
        public Ranks Replace(Suits suit, Ranks rank, Ranks newValue)
        {
            var oldValue = this[suit, rank];
            this[suit, rank] = newValue;
            return oldValue;
        }

        public Ranks[,] Data
        {
            get
            {
                var result = new Ranks[4, 13];
                for (Suits s = Suits.Clubs; s <= Suits.Spades; s++)
                {
                    for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
                    {
                        result[(int)s, (int)r] = this[s, r];
                    }
                }
                return result;
            }
        }

        public string DisplayValue
        {
            get
            {
                var result = new StringBuilder(512);
                for (Suits s = Suits.Clubs; s <= Suits.Spades; s++)
                {
                    result.Append(s.ToXML());
                    result.Append(": ");
                    for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
                    {
                        var v = this[s, r];
                        result.Append(v < 0 ? "-" : this[s, r].ToXML());
                        if (r < Ranks.Ace) result.Append(" ");
                    }
                    if (s < Suits.Spades) result.Append(" ");
                }
                return result.ToString();
            }
        }
    }

    [DebuggerDisplay("{DisplayValue}")]
    public unsafe struct SuitsRanksArrayOfSeats
    {
        private fixed byte data[52];

        public unsafe Seats this[Suits suit, Ranks rank]
        {
            get => (Seats)this.data[(int)suit | ((int)rank << 2)];
            set => this.data[(int)suit | ((int)rank << 2)] = (byte)value;
        }

        public string DisplayValue
        {
            get
            {
                var result = new StringBuilder(512);
                for (Suits s = Suits.Clubs; s <= Suits.Spades; s++)
                {
                    result.Append(s.ToXML());
                    result.Append(": ");
                    for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
                    {
                        result.Append(this[s, r].ToXML());
                        if (r < Ranks.Ace) result.Append(",");
                    }
                    if (s < Suits.Spades) result.Append(" ");
                }
                return result.ToString();
            }
        }
    }

    [DebuggerDisplay("{DisplayValue}")]
    public unsafe struct SuitsRanksArrayOfByte
    {
        private fixed byte data[52];

        public unsafe byte this[Suits suit, Ranks rank]
        {
            get
            {
                return this.data[(int)suit | ((int)rank << 2)];
            }
            set
            {
                this.data[(int)suit | ((int)rank << 2)] = value;
            }
        }

        public unsafe void Fill(byte value)
        {
            for (int i = 0; i < 52; i++)
            {
                data[i] = value;
            }
        }

        public unsafe void Fill(Suits suit, byte value)
        {
            int index = (int)suit;
            for (int i = 1; i <= 13; i++)
            {
                data[index] = value;
                index += 4;
            }
        }

        public string DisplayValue
        {
            get
            {
                var result = new StringBuilder(512);
                for (Suits s = Suits.Clubs; s <= Suits.Spades; s++)
                {
                    result.Append(s.ToXML());
                    result.Append(": ");
                    for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
                    {
                        result.Append(this[s, r]);
                        if (r < Ranks.Ace) result.Append(",");
                    }
                    if (s < Suits.Spades) result.Append(" ");
                }
                return result.ToString();
            }
        }
    }

    [DebuggerDisplay("{DisplayValue}")]
    public unsafe struct SuitsRanksArrayOfInt
    {
        private fixed int data[52];


        public unsafe int this[Suits suit, Ranks rank]
        {
            get
            {
                return this.data[(int)suit | ((int)rank << 2)];
            }
            set
            {
                this.data[(int)suit | ((int)rank << 2)] = value;
            }
        }

        public unsafe void Fill(int value)
        {
            for (int i = 0; i < 52; i++)
            {
                data[i] = value;
            }
        }

        public unsafe void Fill(Suits suit, int value)
        {
            int index = (int)suit;
            for (int i = 1; i <= 13; i++)
            {
                data[index] = value;
                index += 4;
            }
        }

        public string DisplayValue
        {
            get
            {
                var result = new StringBuilder(512);
                for (Suits s = Suits.Clubs; s <= Suits.Spades; s++)
                {
                    result.Append(s.ToXML());
                    result.Append(": ");
                    for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
                    {
                        result.Append(this[s, r]);
                        if (r < Ranks.Ace) result.Append(",");
                    }
                    if (s < Suits.Spades) result.Append(" ");
                }
                return result.ToString();
            }
        }
    }

    /// <summary>
    /// =14 = still have the card
    /// >=1 = played the card in trick ...
    /// 
    /// </summary>
    [DebuggerDisplay("{DisplayValue}")]
    public unsafe struct SeatsSuitsRanksArrayOfByte
    {
        private fixed byte data[256];
        public const int NotPlayed = 14;

        public unsafe byte this[Seats seat, Suits suit, Ranks rank]
        {
            get
            {
                return this.data[(int)rank | ((int)suit << 4) | ((int)seat << 6)];
            }
            set
            {
                this.data[(int)rank | ((int)suit << 4) | ((int)seat << 6)] = value;
            }
        }

        public unsafe Ranks Lowest(Seats seat, Suits suit, int skip)
        {
            int index = 0 | ((int)suit << 4) | ((int)seat << 6);
            int last = index + 13;
            do
            {
                if (data[index] == NotPlayed)
                {
                    if (skip == 0)
                    {
                        return (Ranks)(index - last + 13);
                    }
                    else
                    {
                        skip--;
                    }
                }
                index++;
            } while (index <= last);

            return (Ranks)(-21);
        }

        public unsafe Ranks Highest(Seats seat, Suits suit, int skip)
        {
            int index = 12 | ((int)suit << 4) | ((int)seat << 6);
            int last = index - 13;
            do
            {
                if (data[index] == NotPlayed)
                {
                    if (skip == 0)
                    {
                        return (Ranks)(index - last - 1);
                    }
                    else
                    {
                        skip--;
                    }
                }
                index--;
            } while (index >= last);

            return (Ranks)(-21);
        }

        public unsafe void X(Seats seat, Suits suit, ref Ranks r)
        {
            var higher = r + 1;
            int index = (int)higher | ((int)suit << 4) | ((int)seat << 6);
            while (higher <= Ranks.Ace && data[index++] == 14) higher++;
            higher++; index++;
            while (higher <= Ranks.Ace)
            {
                if (data[index++] == NotPlayed)
                {
                    r = higher;
                }
                higher++;
            }
        }

        public unsafe byte[,,] Data
        {
            get
            {
                var result = new byte[4, 4, 13];
                for (Seats seat = Seats.North; seat <= Seats.West; seat++)
                {
                    for (Suits s = Suits.Clubs; s <= Suits.Spades; s++)
                    {
                        for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
                        {
                            result[(int)seat, (int)s, (int)r] = this[seat, s, r];
                        }
                    }
                }
                return result;
            }
        }

        public unsafe string DisplayValue
        {
            get
            {
                var result = new StringBuilder(512);
                for (Seats p = Seats.North; p <= Seats.West; p++)
                {
                    result.Append(p.ToString2());
                    result.Append(": ");
                    for (Suits s = Suits.Clubs; s <= Suits.Spades; s++)
                    {
                        result.Append(s.ToXML());
                        result.Append(": ");
                        for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
                        {
                            result.Append(this[p, s, r]);
                            if (r < Ranks.Ace) result.Append(",");
                        }
                        if (s < Suits.Spades) result.Append(" ");
                    }
                    if (p < Seats.West) result.Append(" ");
                }

                return result.ToString();
            }
        }
    }

    /// <summary>
    /// only for clubs..spades (4 suits)
    /// </summary>
    [DebuggerDisplay("{DisplayValue}")]
    public unsafe struct SeatsSuitsArrayOfByte
    {
        private fixed byte data[16];

        public byte this[Seats seat, Suits suit]
        {
            get => this[(int)seat, (int)suit];
            set => this[(int)seat, (int)suit] = value;
        }

        public unsafe byte this[int seat, int suit]
        {
            get => data[(suit << 2) | seat];
            set => data[(suit << 2) | seat] = value;
        }

        private string DisplayValue
        {
            get
            {
                unsafe
                {
                    return $"North: {this[Seats.North, Suits.Spades]} {this[Seats.North, Suits.Hearts]} {this[Seats.North, Suits.Diamonds]} {this[Seats.North, Suits.Clubs]} East: {this[Seats.East, Suits.Spades]} {this[Seats.East, Suits.Hearts]} {this[Seats.East, Suits.Diamonds]} {this[Seats.East, Suits.Clubs]} South: {this[Seats.South, Suits.Spades]} {this[Seats.South, Suits.Hearts]} {this[Seats.South, Suits.Diamonds]} {this[Seats.South, Suits.Clubs]} West: {this[Seats.West, Suits.Spades]} {this[Seats.West, Suits.Hearts]} {this[Seats.West, Suits.Diamonds]} {this[Seats.West, Suits.Clubs]}";
                }
            }
        }

        public override string ToString()
        {
            return DisplayValue;
        }
    }

    /// <summary>
    /// for clubs..notrump (5 suits)
    /// </summary>
    [DebuggerDisplay("{DisplayValue}")]
    public unsafe struct SeatsTrumpsArrayOfByte
    {
        private fixed byte data[20];

        public byte this[Seats seat, Suits suit]
        {
            get => this[(int)seat, (int)suit];
            set => this[(int)seat, (int)suit] = value;
        }

        public unsafe byte this[int seat, int suit]
        {
            get => data[(suit << 2) | seat];
            set => data[(suit << 2) | seat] = value;
        }

        private string DisplayValue
        {
            get
            {
                return $"North: {this[Seats.North, Suits.Spades]} {this[Seats.North, Suits.Hearts]} {this[Seats.North, Suits.Diamonds]} {this[Seats.North, Suits.Clubs]} {this[Seats.North, Suits.NoTrump]} East: {this[Seats.East, Suits.Spades]} {this[Seats.East, Suits.Hearts]} {this[Seats.East, Suits.Diamonds]} {this[Seats.East, Suits.Clubs]} {this[Seats.East, Suits.NoTrump]} South: {this[Seats.South, Suits.Spades]} {this[Seats.South, Suits.Hearts]} {this[Seats.South, Suits.Diamonds]} {this[Seats.South, Suits.Clubs]} {this[Seats.South, Suits.NoTrump]} West: {this[Seats.West, Suits.Spades]} {this[Seats.West, Suits.Hearts]} {this[Seats.West, Suits.Diamonds]} {this[Seats.West, Suits.Clubs]} {this[Seats.West, Suits.NoTrump]}";
            }
        }

        public override string ToString()
        {
            return DisplayValue;
        }
    }

    [DebuggerDisplay("{DisplayValue}")]
    public unsafe struct TrickArrayOfSeats
    {
        private fixed byte seat[52];

        public unsafe Seats this[int trick, int man]
        {   // data must be in order: trick, man
            get => (Seats)this.seat[4 * trick + man - 5];
            set => this.seat[4 * trick + man - 5] = (byte)value;
        }

        public Seats this[int lastCard]
        {
            get => (Seats)this.seat[lastCard];
            set => this.seat[lastCard] = (byte)value;
        }

        public string DisplayValue
        {
            get
            {
                var result = new StringBuilder(512);
                for (int trick = 1; trick <= 13; trick++)
                {
                    result.Append(trick.ToString());
                    result.Append(": ");
                    for (int man = 1; man <= 4; man++)
                    {
                        result.Append(this[trick, man].ToXML());
                        if (man < 4) result.Append(",");
                    }
                    if (trick < 13) result.Append(" ");
                }
                return result.ToString();
            }
        }
    }

    [DebuggerDisplay("{DisplayValue}")]
    public unsafe struct TrickArrayOfSuits
    {
        private fixed byte suit[52];

        public unsafe Suits this[int trick, int man]
        {   // data must be in order: trick, man
            get => (Suits)this.suit[4 * trick + man - 5];
            set => this.suit[4 * trick + man - 5] = (byte)value;
        }

        public unsafe Suits this[int lastCard]
        {
            get => (Suits)this.suit[lastCard];
            set => this.suit[lastCard] = (byte)value;
        }

        public string DisplayValue
        {
            get
            {
                var result = new StringBuilder(512);
                for (int trick = 1; trick <= 13; trick++)
                {
                    result.Append(trick.ToString());
                    result.Append(": ");
                    for (int man = 1; man <= 4; man++)
                    {
                        result.Append(this[trick, man].ToXML());
                        if (man < 4) result.Append(",");
                    }
                    if (trick < 13) result.Append(" ");
                }
                return result.ToString();
            }
        }
    }

    [DebuggerDisplay("{DisplayValue}")]
    public unsafe struct TrickArrayOfRanks
    {
        private fixed byte rank[52];

        public unsafe Ranks this[int trick, int man]
        {   // data must be in order: trick, man
            get => (Ranks)this.rank[4 * trick + man - 5];
            set => this.rank[4 * trick + man - 5] = (byte)value;
        }

        public unsafe Ranks this[int lastCard]
        {
            get => (Ranks)this.rank[lastCard];
            set => this.rank[lastCard] = (byte)value;
        }

        public string DisplayValue
        {
            get
            {
                var result = new StringBuilder(512);
                for (int trick = 1; trick <= 13; trick++)
                {
                    result.Append(trick.ToString());
                    result.Append(": ");
                    for (int man = 1; man <= 4; man++)
                    {
                        result.Append(this[trick, man].ToXML());
                        if (man < 4) result.Append(",");
                    }
                    if (trick < 13) result.Append(" ");
                }
                return result.ToString();
            }
        }
    }
}
