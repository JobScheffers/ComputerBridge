using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;

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
            for (int r = Rank.Two; r <= Rank.Ace; r++)
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

        public Ranks this[Suits suit, Ranks rank]
        {
            get
            {
                var rawValue = this.data[13 * (int)suit + (int)rank];
                // trick for coping with negatives in byte (-127..128)
                var value = (Ranks)((int)rawValue - 128);
                return value;
            }
            set
            {
                this.data[13 * (int)suit + (int)rank] = (byte)(value + 128);
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
    public unsafe struct SuitsRanksArrayOfSeats
    {
        private fixed byte data[52];

        public Seats this[Suits suit, Ranks rank]
        {
            get => (Seats)this.data[13 * (int)suit + (int)rank];
            set => this.data[13 * (int)suit + (int)rank] = (byte)value;
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

        public byte this[Suits suit, Ranks rank]
        {
            get => this.data[13 * (int)suit + (int)rank];
            set => this.data[13 * (int)suit + (int)rank] = value;
        }

        public void Fill(byte value)
        {
            for (int i = 0; i < 52; i++)
            {
                data[i] = value;
            }
        }

        public void Fill(Suits suit, byte value)
        {
            int index = 13 * (int)suit;
            for (int i = index; i < index + 13; i++)
            {
                data[i] = value;
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

        public int this[Suits suit, Ranks rank]
        {
            get => this.data[13 * (int)suit + (int)rank];
            set => this.data[13 * (int)suit + (int)rank] = value;
        }

        public void Fill(int value)
        {
            for (int i = 0; i < 52; i++)
            {
                data[i] = value;
            }
        }

        public void Fill(Suits suit, int value)
        {
            int index = 13 * (int)suit;
            for (int i = index; i < index + 13; i++)
            {
                data[i] = value;
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
    public unsafe struct SeatsSuitsRanksArrayOfByte
    {
        private fixed byte data[208];

        public byte this[Seats seat, Suits suit, Ranks rank]
        {
            get => this.data[52 * (int)seat + 13 * (int)suit + (int)rank];
            set => this.data[52 * (int)seat + 13 * (int)suit + (int)rank] = value;
        }

        public Ranks Lowest(Seats seat, Suits suit, int skip)
        {
            int index = 52 * (int)seat + 13 * (int)suit;
            for (Ranks rank = Ranks.Two; rank <= Ranks.Ace; rank++)
            {
                if (data[index++] == 14)
                {
                    if (skip == 0)
                    {
                        return rank;
                    }
                    else
                    {
                        skip--;
                    }
                }
            }

            return (Ranks)(-21);
        }

        public Ranks Highest(Seats seat, Suits suit, int skip)
        {
            int index = 52 * (int)seat + 13 * (int)suit + 12;
            for (Ranks rank = Ranks.Ace; rank >= Ranks.Two; rank--)
            {
                if (data[index--] == 14)
                {
                    if (skip == 0)
                    {
                        return rank;
                    }
                    else
                    {
                        skip--;
                    }
                }
            }

            return (Ranks)(-21);
        }

        public string DisplayValue
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

    [DebuggerDisplay("{DisplayValue}")]
    public unsafe struct SeatsSuitsArrayOfByte
    {
        private fixed byte data[16];

        public byte this[Seats seat, Suits suit]
        {
            get => this.data[4 * (int)seat + (int)suit];
            set => this.data[4 * (int)seat + (int)suit] = value;
        }

        public string DisplayValue
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
                        result.Append(this[p, s]);
                        if (s < Suits.Spades) result.Append(",");
                    }
                    if (p < Seats.West) result.Append(" ");
                }

                return result.ToString();
            }
        }
    }

    [DebuggerDisplay("{DisplayValue}")]
    public unsafe struct TrickArrayOfSeats
    {
        private fixed byte seat[52];

        public Seats this[int trick, int man]
        {
            get => (Seats) this.seat[4 * trick + man - 5];
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

        public Suits this[int trick, int man]
        {
            get => (Suits)this.suit[4 * trick + man - 5];
            set => this.suit[4 * trick + man - 5] = (byte)value;
        }

        public Suits this[int lastCard]
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

        public Ranks this[int trick, int man]
        {
            get => (Ranks)this.rank[4 * trick + man - 5];
            set => this.rank[4 * trick + man - 5] = (byte)value;
        }

        public Ranks this[int lastCard]
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

    //public unsafe struct SuitsRanksArray<T> where T : unmanaged
    //{
    //    private fixed T data[52];

    //    public T this[Suits index1, Ranks index2]
    //    {
    //        get => this.data[13 * (int)index1 + (int)index2];
    //        set => this.data[13 * (int)index1 + (int)index2] = value;
    //    }

    //}
}
