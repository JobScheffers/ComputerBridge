using Bridge.NonBridgeHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bridge
{
    // ---- these are all obsolete versions of ....Array<T> for specific types, kept for backward compatibility with existing code ----

    /// <summary>
    /// This specific version of a SuitRankCollection is a fraction faster in cloning, uses bytes to store data while allowing int in the interface
    /// </summary>
    [Obsolete("use the generic SuitRankCollection<int>")]
    public class SuitRankCollectionInt
    {
        private SuitsRanksArrayOfInt x;

        public SuitRankCollectionInt()
        {
        }

        public SuitRankCollectionInt(int initialValue)
            : this()
        {
            foreach (Suits s in SuitHelper.StandardSuitsAscending)
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
            foreach (Ranks r in RankHelper.RanksAscending)
            {
                this.x[suit, r] = value;
            }
        }

        public SuitRankCollectionInt Clone()
        {
            var result = new SuitRankCollectionInt
            {
                x = this.x
            };
            return result;
        }
    }

    [DebuggerDisplay("{DisplayValue}"), Obsolete("SuitsRanksArray<Ranks>")]
    public unsafe struct SuitsRanksArrayOfRanks
    {
        private fixed sbyte data[52];

        public Ranks this[Suits suit, Ranks rank]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetValue(suit, rank);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetValue(suit, rank, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Ranks GetValue(Suits suit, Ranks rank)
        {
            return (Ranks)data[Index(suit, rank)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(Suits suit, Ranks rank, Ranks value)
        {
            data[Index(suit, rank)] = (sbyte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(Suits suit, Ranks rank)
            => ((int)rank << 2) | (int)suit;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(Ranks value)
        {
            byte v = unchecked((byte)(sbyte)value);

            fixed (sbyte* p = data)
            {
                Unsafe.InitBlockUnaligned((void*)p, v, 52);
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
                foreach (Suits s in SuitHelper.StandardSuitsAscending)
                {
                    foreach (Ranks r in RankHelper.RanksAscending)
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
                foreach (Suits s in SuitHelper.StandardSuitsAscending)
                {
                    result.Append(s.ToXML());
                    result.Append(": ");
                    foreach (Ranks r in RankHelper.RanksAscending)
                    {
                        var v = this[s, r];
                        result.Append(v < 0 ? "-" : this[s, r].ToXML());
                        if (r < Ranks.Ace) result.Append(' ');
                    }
                    if (s < Suits.Spades) result.Append(' ');
                }
                return result.ToString();
            }
        }
    }

    [DebuggerDisplay("{DisplayValue}"), Obsolete("SuitsRanksArray<Seats>")]
    public unsafe struct SuitsRanksArrayOfSeats
    {
        private fixed sbyte data[52];

        public Seats this[Suits suit, Ranks rank]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetValue(suit, rank);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetValue(suit, rank, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Seats GetValue(Suits suit, Ranks rank)
        {
            return (Seats)data[Index(suit, rank)];
            //sbyte v = data[Index(suit, rank)];
            //return Unsafe.As<sbyte, Seats>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(Suits suit, Ranks rank, Seats value)
            => data[Index(suit, rank)] = (sbyte)value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(Suits suit, Ranks rank)
            => ((int)rank << 2) | (int)suit;

        public string DisplayValue
        {
            get
            {
                var sb = new StringBuilder(512);

                foreach (Suits s in SuitHelper.StandardSuitsAscending)
                {
                    sb.Append(s.ToXML()).Append(": ");

                    foreach (Ranks r in RankHelper.RanksAscending)
                    {
                        sb.Append(this[s, r].ToXML());
                        if (r < Ranks.Ace) sb.Append(',');
                    }

                    if (s < Suits.Spades) sb.Append(' ');
                }

                return sb.ToString();
            }
        }
    }

    [DebuggerDisplay("{DisplayValue}"), Obsolete("SuitsRanksArray<byte>")]
    public unsafe struct SuitsRanksArrayOfByte
    {
        private fixed byte data[52];

        public byte this[Suits suit, Ranks rank]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetValue(suit, rank);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetValue(suit, rank, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte GetValue(Suits suit, Ranks rank)
        {
            return data[Index(suit, rank)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(Suits suit, Ranks rank, byte value)
        {
            data[Index(suit, rank)] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(Suits suit, Ranks rank)
            => ((int)rank << 2) | (int)suit;

        public void Fill(byte value)
        {
            for (int i = 0; i < 52; i++)
            {
                data[i] = value;
            }
        }

        public unsafe void Fill(Suits suit, byte value)
        {
            foreach (Ranks rank in RankHelper.RanksAscending)
            {
                data[Index(suit, rank)] = value;
            }
        }

        public string DisplayValue
        {
            get
            {
                var result = new StringBuilder(512);
                foreach (Suits s in SuitHelper.StandardSuitsAscending)
                {
                    result.Append(s.ToXML());
                    result.Append(": ");
                    foreach (Ranks r in RankHelper.RanksAscending)
                    {
                        result.Append(this[s, r]);
                        if (r < Ranks.Ace) result.Append(',');
                    }
                    if (s < Suits.Spades) result.Append(' ');
                }
                return result.ToString();
            }
        }
    }

    [DebuggerDisplay("{DisplayValue}"), Obsolete("SuitsRanksArray<int>")]
    public unsafe struct SuitsRanksArrayOfInt
    {
        private fixed int data[52];

        public int this[Suits suit, Ranks rank]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetValue(suit, rank);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetValue(suit, rank, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetValue(Suits suit, Ranks rank)
        {
            return data[Index(suit, rank)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(Suits suit, Ranks rank, int value)
        {
            data[Index(suit, rank)] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(Suits suit, Ranks rank)
            => ((int)rank << 2) | (int)suit;

        public unsafe void Fill(int value)
        {
            for (int i = 0; i < 52; i++)
            {
                data[i] = value;
            }
        }

        public unsafe void Fill(Suits suit, int value)
        {
            foreach (Ranks rank in RankHelper.RanksAscending)
            {
                data[Index(suit, rank)] = value;
            }
        }

        public string DisplayValue
        {
            get
            {
                var result = new StringBuilder(512);
                foreach (Suits s in SuitHelper.StandardSuitsAscending)
                {
                    result.Append(s.ToXML());
                    result.Append(": ");
                    foreach (Ranks r in RankHelper.RanksAscending)
                    {
                        result.Append(this[s, r]);
                        if (r < Ranks.Ace) result.Append(',');
                    }
                    if (s < Suits.Spades) result.Append(' ');
                }
                return result.ToString();
            }
        }
    }

    [DebuggerDisplay("{DisplayValue}"), Obsolete("SeatsSuitsRanksArray<sbyte>")]
    public unsafe struct SeatsSuitsRanksArrayOfByte
    {
        private fixed byte data[256];

        public const byte NotPlayed = 14;

        public byte this[Seats seat, Suits suit, Ranks rank]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetValue((int)seat, (int)suit, (int)rank);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetValue((int)seat, (int)suit, (int)rank, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte GetValue(int seat, int suit, int rank)
        {
            return data[Index(seat, suit, rank)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(int seat, int suit, int rank, byte value)
        {
            data[Index(seat, suit, rank)] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(int seat, int suit, int rank)
            => rank | (suit << 4) | (seat << 6);

        public Ranks Lowest(Seats seat, Suits suit, int skip)
        {
            int index = Index((int)seat, (int)suit, 0);
            int end = index + 13;

            for (; index <= end; index++)
            {
                if (data[index] == NotPlayed)
                {
                    if (skip-- == 0)
                        return (Ranks)(index - end + 13);
                }
            }

            return (Ranks)(-21);
        }
        public Ranks Highest(Seats seat, Suits suit, int skip)
        {
            int index = Index((int)seat, (int)suit, 12);
            int end = index - 13;

            for (; index >= end; index--)
            {
                if (data[index] == NotPlayed)
                {
                    if (skip-- == 0)
                        return (Ranks)(index - end - 1);
                }
            }

            return (Ranks)(-21);
        }
        public void X(Seats seat, Suits suit, ref Ranks r)
        {
            int higher = (int)r + 1;
            int index = Index((int)seat, (int)suit, higher);

            while (higher <= (int)Ranks.Ace && data[index++] == NotPlayed)
                higher++;

            higher++;
            index++;

            while (higher <= (int)Ranks.Ace)
            {
                if (data[index++] == NotPlayed)
                    r = (Ranks)higher;

                higher++;
            }
        }
        public byte[,,] Data
        {
            get
            {
                var result = new byte[4, 4, 13];
                foreach (var seat in SeatsExtensions.SeatsAscending)
                    foreach (var suit in SuitHelper.StandardSuitsAscending)
                        foreach (var rank in RankHelper.RanksAscending)
                            result[(int)seat, (int)suit, (int)rank] = this[seat, suit, rank];
                return result;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder(512);

            foreach (var seat in SeatsExtensions.SeatsAscending)
            {
                sb.Append(seat.ToLocalizedString()).Append(": ");
                foreach (var suit in SuitHelper.StandardSuitsAscending)
                {
                    sb.Append(suit.ToXML()).Append(": ");
                    foreach (var rank in RankHelper.RanksAscending)
                    {
                        sb.Append(this[seat, suit, rank]);
                        if (rank < Ranks.Ace) sb.Append(',');
                    }
                    if (suit < Suits.Spades) sb.Append(' ');
                }
                if (seat < Seats.West) sb.Append(' ');
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// only for clubs..spades (4 suits)
    /// </summary>
    [DebuggerDisplay("{DisplayValue}"), Obsolete("SeatsSuitsArray<byte>")]
    public unsafe struct SeatsSuitsArrayOfByte
    {
        private fixed byte data[16]; // 4 suits × 4 seats

        public byte this[Seats seat, Suits suit]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetValue((int)seat, (int)suit);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetValue((int)seat, (int)suit, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte GetValue(int seat, int suit)
        {
            return data[Index(seat, suit)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(int seat, int suit, byte value)
        {
            data[Index(seat, suit)] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(int seat, int suit)
            => (suit << 2) | seat; // suit * 4 + seat

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

    [DebuggerDisplay("{DisplayValue}"), Obsolete("SeatsTrumpsArray<sbyte>")]
    public unsafe struct SeatsTrumpsArrayOfByte
    {
        private fixed byte data[20];

        public byte this[Seats seat, Suits suit]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetValue((int)seat, (int)suit);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetValue((int)seat, (int)suit, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte GetValue(int seat, int suit)
        {
            return data[Index(seat, suit)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(int seat, int suit, byte value)
        {
            data[Index(seat, suit)] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(int seat, int suit)
            => (suit << 2) | seat; // suit * 4 + seat

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

    [DebuggerDisplay("{DisplayValue}"), Obsolete("TrickArray<Seats>")]
    public unsafe struct TrickArrayOfSeats
    {
        private fixed sbyte data[52];

        public Seats this[int trick, int man]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetValue(trick, man);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetValue(trick, man, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Seats GetValue(int trick, int man)
        {
            return (Seats)data[Index(trick, man)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(int trick, int man, Seats value)
        {
            data[Index(trick, man)] = (sbyte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(int trick, int man)
            => 4 * trick + man - 5;

        public Seats this[int lastCard]
        {
            get => (Seats)this.data[lastCard];
            set => this.data[lastCard] = (sbyte)value;
        }

        public string DisplayValue
        {
            get
            {
                var result = new StringBuilder(512);
                for (int trick = 1; trick <= 13; trick++)
                {
                    result.Append(trick);
                    result.Append(": ");
                    for (int man = 1; man <= 4; man++)
                    {
                        result.Append(this[trick, man].ToXML());
                        if (man < 4) result.Append(',');
                    }
                    if (trick < 13) result.Append(' ');
                }
                return result.ToString();
            }
        }
    }

    [DebuggerDisplay("{DisplayValue}"), Obsolete("TrickArray<Suits>")]
    public unsafe struct TrickArrayOfSuits
    {
        private fixed sbyte data[52];

        public Suits this[int trick, int man]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetValue(trick, man);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetValue(trick, man, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Suits GetValue(int trick, int man)
        {
            return (Suits)data[Index(trick, man)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(int trick, int man, Suits value)
        {
            data[Index(trick, man)] = (sbyte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(int trick, int man)
            => 4 * trick + man - 5;

        public Suits this[int lastCard]
        {
            get => (Suits)this.data[lastCard];
            set => this.data[lastCard] = (sbyte)value;
        }

        public string DisplayValue
        {
            get
            {
                var result = new StringBuilder(512);
                for (int trick = 1; trick <= 13; trick++)
                {
                    result.Append(trick);
                    result.Append(": ");
                    for (int man = 1; man <= 4; man++)
                    {
                        result.Append(this[trick, man].ToXML());
                        if (man < 4) result.Append(',');
                    }
                    if (trick < 13) result.Append(' ');
                }
                return result.ToString();
            }
        }
    }

    [DebuggerDisplay("{DisplayValue}"), Obsolete("TrickArray<Ranks>")]
    public unsafe struct TrickArrayOfRanks
    {
        private fixed sbyte data[52];

        public Ranks this[int trick, int man]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetValue(trick, man);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetValue(trick, man, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Ranks GetValue(int trick, int man)
        {
            return (Ranks)data[Index(trick, man)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(int trick, int man, Ranks value)
        {
            data[Index(trick, man)] = (sbyte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(int trick, int man)
            => 4 * trick + man - 5;

        public unsafe Ranks this[int lastCard]
        {
            get => (Ranks)this.data[lastCard];
            set => this.data[lastCard] = (sbyte)value;
        }

        public string DisplayValue
        {
            get
            {
                var result = new StringBuilder(512);
                for (int trick = 1; trick <= 13; trick++)
                {
                    result.Append(trick);
                    result.Append(": ");
                    for (int man = 1; man <= 4; man++)
                    {
                        result.Append(this[trick, man].ToXML());
                        if (man < 4) result.Append(',');
                    }
                    if (trick < 13) result.Append(' ');
                }
                return result.ToString();
            }
        }
    }
}
