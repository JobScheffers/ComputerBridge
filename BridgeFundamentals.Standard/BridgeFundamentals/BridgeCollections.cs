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
    public class SuitCollection<T>
    {
        private readonly T[] x = new T[5];

        public SuitCollection()
        {
        }

        public SuitCollection(T initialValue)
        {
            this.Set(initialValue);
        }

        public SuitCollection(T[] initialValues)
        {
            foreach (Suits s in SuitHelper.TrumpSuitsAscending)
                this[s] = initialValues[(int)s];
        }

        public ref T this[Suits index]
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return ref x[(int)index];
            }
            //[DebuggerStepThrough]
            //set
            //{
            //    x[(int)index] = value;
            //}
        }

        //public T this[int index]
        //{
        //    [DebuggerStepThrough]
        //    get
        //    {
        //        return x[index];
        //    }
        //    [DebuggerStepThrough]
        //    set
        //    {
        //        x[index] = value;
        //    }
        //}

        public void Set(T value)
        {
            foreach (Suits s in SuitHelper.TrumpSuitsAscending)
                this[s] = value;
        }

        //public SuitCollection<T> Clone()
        //{
        //  SuitCollection<T> result = new SuitCollection<T>();
        //  foreach (Suits s in SuitHelper.TrumpSuitsAscending)
        //  {
        //    result[s] = this[s];
        //  }

        //  return result;
        //}
    }

    public unsafe struct Deal
    {
        // 52 cards * 3 bits = 156 bits -> 20 bytes
        // cardIndex = suit * 13 + rank  (suit: 0..3, rank: 0..12)
        private const int CardCount = 52;
        private const int BitsPerCard = 3;
        private const int UnassignedValue = 4; // 0..3 seats, 4 = unassigned

        // Inline storage: 20 bytes
        private fixed byte _data[20];

        public Deal()
        {
            Clear();
        }

        [DebuggerStepThrough]
        public Deal(in string pbnDeal) : this()
        {
            var firstHand = pbnDeal[0];
            var hands = pbnDeal[2..].Split2(' ');
            var hand = HandFromPbn(in firstHand);
            foreach (var handHolding in hands)
            {
                var suits = handHolding.Line.Split2('.');
                int pbnSuit = 1;
                foreach (var suitHolding in suits)
                {
                    var suitCards = suitHolding.Line;
                    var suitLength = suitCards.Length;
                    var suit = SuitFromPbn(pbnSuit);
                    for (int r = 0; r < suitLength; r++)
                    {
                        var rank = RankFromPbn(in suitCards[r]);
                        this[hand, suit, rank] = true;
                    }
                    pbnSuit++;
                }

                hand = NextHandPbn(hand);
            }

            [DebuggerStepThrough]
            static Seats HandFromPbn(ref readonly Char hand)
            {
                return hand switch
                {
                    'n' or 'N' => Seats.North,
                    'e' or 'E' => Seats.East,
                    's' or 'S' => Seats.South,
                    'w' or 'W' => Seats.West,
                    _ => throw new ArgumentOutOfRangeException(nameof(hand), $"unknown {hand}"),
                };
            }

            [DebuggerStepThrough]
            static Seats NextHandPbn(Seats hand)
            {
                return hand switch
                {
                    Seats.North => Seats.East,
                    Seats.East => Seats.South,
                    Seats.South => Seats.West,
                    Seats.West => Seats.North,
                    _ => throw new ArgumentOutOfRangeException(nameof(hand), $"unknown {hand}"),
                };
            }

            [DebuggerStepThrough]
            static Suits SuitFromPbn(int relativeSuit)
            {
                return relativeSuit switch
                {
                    1 => Suits.Spades,
                    2 => Suits.Hearts,
                    3 => Suits.Diamonds,
                    4 => Suits.Clubs,
                    _ => throw new ArgumentOutOfRangeException(nameof(relativeSuit), $"unknown {relativeSuit}"),
                };
            }

            [DebuggerStepThrough]
            static Ranks RankFromPbn(ref readonly Char rank)
            {
                return rank switch
                {
                    'a' or 'A' => Ranks.Ace,
                    'k' or 'h' or 'H' or 'K' => Ranks.King,
                    'q' or 'Q' => Ranks.Queen,
                    'j' or 'b' or 'B' or 'J' => Ranks.Jack,
                    't' or 'T' => Ranks.Ten,
                    '9' => Ranks.Nine,
                    '8' => Ranks.Eight,
                    '7' => Ranks.Seven,
                    '6' => Ranks.Six,
                    '5' => Ranks.Five,
                    '4' => Ranks.Four,
                    '3' => Ranks.Three,
                    '2' => Ranks.Two,
                    _ => throw new ArgumentOutOfRangeException(nameof(rank), $"unknown {rank}"),
                };
            }
        }

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

        /// <summary>
        /// Indexer: get -> true if seat owns card.
        /// set = true -> assign card to seat.
        /// set = false -> unassign only if that seat currently owns the card.
        /// </summary>
        public bool this[int seat, int suit, int rank]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ValidateSeatSuitRank(seat, suit, rank);
                int cardIndex = suit * 13 + rank;
                return GetOwnerBits(cardIndex) == (byte)seat;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                ValidateSeatSuitRank(seat, suit, rank);
                int cardIndex = suit * 13 + rank;
                if (value)
                {
                    SetOwnerBits(cardIndex, (byte)seat);
                }
                else
                {
                    if (GetOwnerBits(cardIndex) == (byte)seat)
                        SetOwnerBits(cardIndex, UnassignedValue);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int? GetOwner(int suit, int rank)
        {
            ValidateSuitRank(suit, rank);
            int cardIndex = suit * 13 + rank;
            byte v = GetOwnerBits(cardIndex);
            return v == UnassignedValue ? (int?)null : v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetOwner(int? seat, int suit, int rank)
        {
            ValidateSuitRank(suit, rank);
            int cardIndex = suit * 13 + rank;
            if (seat == null)
                SetOwnerBits(cardIndex, UnassignedValue);
            else
            {
                if (seat < 0 || seat > 3) throw new ArgumentOutOfRangeException(nameof(seat));
                SetOwnerBits(cardIndex, (byte)seat);
            }
        }

        public void Clear()
        {
            fixed (byte* p = _data)
            {
                // set all bytes to 0 first
                for (int i = 0; i < 20; i++) p[i] = 0;
                // then set each card to UnassignedValue (4)
                for (int cardIndex = 0; cardIndex < CardCount; cardIndex++)
                    SetOwnerBits(cardIndex, UnassignedValue);
            }
        }

        public ulong[] ToSeatBitmasks()
        {
            var masks = new ulong[4];
            for (int cardIndex = 0; cardIndex < CardCount; cardIndex++)
            {
                byte owner = GetOwnerBits(cardIndex);
                if (owner != UnassignedValue)
                    masks[owner] |= 1UL << cardIndex;
            }
            return masks;
        }

        public void FromSeatBitmasks(ulong[] seatMasks)
        {
            ArgumentNullException.ThrowIfNull(seatMasks);
            if (seatMasks.Length != 4) throw new ArgumentException("seatMasks must have length 4", nameof(seatMasks));

            for (int cardIndex = 0; cardIndex < CardCount; cardIndex++)
            {
                byte owner = UnassignedValue;
                ulong mask = 1UL << cardIndex;
                for (byte s = 0; s < 4; s++)
                {
                    if ((seatMasks[s] & mask) != 0UL) owner = s;
                }
                SetOwnerBits(cardIndex, owner);
            }
        }

        public IEnumerable<(int suit, int rank)> EnumerateCards(int seat)
        {
            if (seat < 0 || seat > 3) throw new ArgumentOutOfRangeException(nameof(seat));
            for (int cardIndex = 0; cardIndex < CardCount; cardIndex++)
            {
                if (GetOwnerBits(cardIndex) == (byte)seat)
                {
                    int suit = cardIndex / 13;
                    int rank = cardIndex % 13;
                    yield return (suit, rank);
                }
            }
        }

        public string ToPBN()
        {
            var result = new StringBuilder(70);
            result.Append("N:");
            foreach (Seats hand in SeatsExtensions.SeatsAscending)
            {
                foreach(var suit in SuitHelper.StandardSuitsDescending)
                {
                    foreach (var rank in RankHelper.RanksDescending)
                    {
                        if (this[hand, suit, rank])
                        {
                            result.Append(RankToPbn(rank));
                        }
                    }
                    ;

                    if (suit != Suits.Clubs) result.Append('.');
                }
                ;

                if (hand != Seats.West) result.Append(' ');
            }
            ;

            return result.ToString();

            static string RankToPbn(Ranks rank)
            {
                return rank switch
                {
                    Ranks.Ace => "A",
                    Ranks.King => "K",
                    Ranks.Queen => "Q",
                    Ranks.Jack => "J",
                    Ranks.Ten => "T",
                    Ranks.Nine => "9",
                    Ranks.Eight => "8",
                    Ranks.Seven => "7",
                    Ranks.Six => "6",
                    Ranks.Five => "5",
                    Ranks.Four => "4",
                    Ranks.Three => "3",
                    Ranks.Two => "2",
                    _ => throw new ArgumentOutOfRangeException(nameof(rank), $"unknown {rank}"),
                };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte GetOwnerBits(int cardIndex)
        {
            Debug.Assert(cardIndex >= 0 && cardIndex < CardCount);
            int bitPos = cardIndex * BitsPerCard;
            int byteIndex = bitPos >> 3; // /8
            int shift = bitPos & 7;      // bit offset within byte

            fixed (byte* p = _data)
            {
                if (shift <= 5)
                {
                    return (byte)((p[byteIndex] >> shift) & 0x7);
                }
                else
                {
                    int low = p[byteIndex] >> shift;
                    int high = p[byteIndex + 1] << (8 - shift);
                    return (byte)((low | high) & 0x7);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetOwnerBits(int cardIndex, int owner)
        {
            Debug.Assert(cardIndex >= 0 && cardIndex < CardCount);
            Debug.Assert(owner >= 0 && owner <= 7);
            int bitPos = cardIndex * BitsPerCard;
            int byteIndex = bitPos >> 3;
            int shift = bitPos & 7;

            fixed (byte* p = _data)
            {
                if (shift <= 5)
                {
                    int mask = 0x7 << shift;
                    p[byteIndex] = (byte)((p[byteIndex] & ~mask) | ((owner & 0x7) << shift));
                }
                else
                {
                    // spans two bytes
                    int lowBits = 8 - shift;
                    int highBits = BitsPerCard - lowBits;

                    int lowMask = ((1 << lowBits) - 1) << shift;
                    int lowVal = (owner & ((1 << lowBits) - 1)) << shift;
                    p[byteIndex] = (byte)((p[byteIndex] & ~lowMask) | lowVal);

                    int highMask = (1 << highBits) - 1;
                    int highVal = (owner >> lowBits) & highMask;
                    p[byteIndex + 1] = (byte)((p[byteIndex + 1] & ~highMask) | highVal);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateSeatSuitRank(int seat, int suit, int rank)
        {
            if (seat < 0 || seat > 3) throw new ArgumentOutOfRangeException(nameof(seat));
            ValidateSuitRank(suit, rank);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateSuitRank(int suit, int rank)
        {
            if (suit < 0 || suit > 3) throw new ArgumentOutOfRangeException(nameof(suit));
            if (rank < 0 || rank > 12) throw new ArgumentOutOfRangeException(nameof(rank));
        }

        public override string ToString()
        {
            int[] counts = new int[5]; // 0..3 seats, 4 unassigned
            for (int i = 0; i < CardCount; i++) counts[GetOwnerBits(i)]++;
            return $"S0={counts[0]} S1={counts[1]} S2={counts[2]} S3={counts[3]} Unassigned={counts[4]}";
        }

        /// <summary>
        /// Construct from big-endian random bytes (most-significant byte first).
        /// </summary>
        public Deal(byte[] bigEndianRandom) : this()
        {
            ArgumentNullException.ThrowIfNull(bigEndianRandom);
            if (bigEndianRandom.Length == 0) throw new ArgumentException("random bytes must not be empty", nameof(bigEndianRandom));

            // Convert big-endian bytes to a non-negative BigInteger
            // Ensure a leading zero byte to force positive two's complement representation
            byte[] tmp = new byte[bigEndianRandom.Length + 1];
            for (int i = 0; i < bigEndianRandom.Length; i++)
                tmp[tmp.Length - 1 - i] = bigEndianRandom[i]; // reverse into little-endian order
            BigInteger value = new(tmp); // non-negative because top byte is zero

            // Delegate to BigInteger constructor
            // (this(...) cannot be used because we already called : this() above)
            // So reuse the same logic inline:
            BigInteger[] fact = new BigInteger[53];
            fact[0] = BigInteger.One;
            for (int i = 1; i <= 52; i++) fact[i] = fact[i - 1] * i;

            BigInteger modulus = fact[52];
            if (value >= modulus) value %= modulus;

            var remaining = Enumerable.Range(0, 52).ToList();
            int[] permutation = new int[52];

            for (int i = 0; i < 52; i++)
            {
                int k = 51 - i;
                BigInteger divisor = fact[k];
                BigInteger q = value / divisor;
                value %= divisor;

                int idx = (int)q;
                permutation[i] = remaining[idx];
                remaining.RemoveAt(idx);
            }

            for (int i = 0; i < 52; i++)
            {
                int cardIndex = permutation[i];
                int seat = i / 13;
                SetOwnerBits(cardIndex, seat);
            }
        }

        /// <summary>
        /// Fill only the currently unassigned cards using the provided BigInteger.
        /// The BigInteger is converted to a permutation of the remaining cards via Lehmer code.
        /// The permutation is then distributed to seats so each seat ends up with 13 cards.
        /// </summary>
        public void FillUnassignedFromBigInteger(BigInteger value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Value must be non-negative.");

            // Collect unassigned card indices
            var remaining = new List<int>(52);
            for (int cardIndex = 0; cardIndex < CardCount; cardIndex++)
            {
                if (GetOwnerBits(cardIndex) == UnassignedValue)
                    remaining.Add(cardIndex);
            }

            int n = remaining.Count;
            if (n == 0) return; // nothing to do

            // Precompute factorials 0..n
            BigInteger[] fact = new BigInteger[n + 1];
            fact[0] = BigInteger.One;
            for (int i = 1; i <= n; i++) fact[i] = fact[i - 1] * i;

            BigInteger modulus = fact[n];
            if (value >= modulus) value %= modulus;

            // Build permutation of the remaining list using Lehmer code
            var permuted = new int[n];
            var pool = new List<int>(remaining); // mutable pool
            for (int i = 0; i < n; i++)
            {
                int k = n - 1 - i;
                BigInteger divisor = fact[k];
                BigInteger q = value / divisor;
                value %= divisor;

                int idx = (int)q; // q < (k+1) <= n
                permuted[i] = pool[idx];
                pool.RemoveAt(idx);
            }

            // Determine how many cards each seat still needs (13 total per seat)
            int[] need = new int[4];
            for (int s = 0; s < 4; s++)
            {
                int count = 0;
                // count current cards for seat s
                for (int cardIndex = 0; cardIndex < CardCount; cardIndex++)
                    if (GetOwnerBits(cardIndex) == (byte)s) count++;
                need[s] = 13 - count;
                if (need[s] < 0) need[s] = 0; // defensive: if seat already has >13, we won't remove cards
            }

            // Sanity: total needed should equal n (unless some seats already >13)
            int totalNeeded = need.Sum();
            if (totalNeeded != n)
            {
                // If mismatch (e.g., some seat already has >13), we still assign remaining cards in seat order
                // but we compute a fallback distribution: fill seats with need>0 first, then any seat with space.
                // For simplicity, if totalNeeded < n, we will assign extra cards to seats in round-robin order.
                // If totalNeeded > n (shouldn't happen), we assign as many as available.
            }

            // Assign permuted cards to seats in seat order, respecting need[] as much as possible.
            int pos = 0;
            // First pass: satisfy needs
            for (int s = 0; s < 4 && pos < n; s++)
            {
                int take = Math.Min(need[s], n - pos);
                for (int t = 0; t < take; t++)
                {
                    int cardIndex = permuted[pos++];
                    SetOwnerBits(cardIndex, s);
                }
            }

            // If any permuted cards remain (due to mismatch), distribute round-robin to seats 0..3
            int seatRound = 0;
            while (pos < n)
            {
                int cardIndex = permuted[pos++];
                // find next seat that currently has <13 cards (prefer seats with <13)
                int assignedSeat = -1;
                for (int attempt = 0; attempt < 4; attempt++)
                {
                    int s = (seatRound + attempt) & 3;
                    int currentCount = 0;
                    for (int ci = 0; ci < CardCount; ci++)
                        if (GetOwnerBits(ci) == (byte)s) currentCount++;
                    if (currentCount < 13)
                    {
                        assignedSeat = s;
                        seatRound = (s + 1) & 3;
                        break;
                    }
                }
                if (assignedSeat == -1)
                {
                    // all seats already have 13 or more; just assign to seatRound and advance
                    assignedSeat = seatRound;
                    seatRound = (seatRound + 1) & 3;
                }
                SetOwnerBits(cardIndex, assignedSeat);
            }
        }

        /// <summary>
        /// Construct a complete deal (52 cards, 13 per seat) from a non-negative BigInteger.
        /// The integer is interpreted in the factorial number system (Lehmer code) and
        /// mapped to a permutation of the 52 cards. If value >= 52!, it is reduced modulo 52!.
        /// </summary>
        public Deal(BigInteger value) : this()
        {
            FillUnassignedFromBigInteger(value);
        }

        /// <summary>
        /// Overload that accepts big-endian random bytes and fills only unassigned cards.
        /// </summary>
        public void FillUnassignedFromBigEndianBytes(byte[] bigEndianRandom)
        {
            ArgumentNullException.ThrowIfNull(bigEndianRandom);
            if (bigEndianRandom.Length == 0) throw new ArgumentException("random bytes must not be empty", nameof(bigEndianRandom));

            // Convert big-endian bytes to non-negative BigInteger
            byte[] tmp = new byte[bigEndianRandom.Length + 1];
            for (int i = 0; i < bigEndianRandom.Length; i++)
                tmp[tmp.Length - 1 - i] = bigEndianRandom[i]; // reverse into little-endian order
            BigInteger value = new(tmp); // non-negative because top byte is zero

            FillUnassignedFromBigInteger(value);
        }

        /// <summary>
        /// Produce a single random completion of this partial deal using the provided BigInteger seed.
        /// The current object is not modified; a new DealFixed is returned.
        /// </summary>
        public Deal CompletedFromSeed(BigInteger seed)
        {
            var result = this.Clone();
            result.FillUnassignedFromBigInteger(seed);
            return result;
        }

        // Example sketch — requires implementing SetByte(int index, byte value)
        public Deal Clone()
        {
            var copy = new Deal();
            fixed (byte* src = _data)
            {
                for (int i = 0; i < 20; i++)
                    copy.SetByte(i, src[i]); // SetByte must write into copy._data safely
            }
            return copy;
        }

        private void SetByte(int index, byte value)
        {
            if (index < 0 || index >= 20) throw new ArgumentOutOfRangeException(nameof(index));
            fixed (byte* p = _data)
            {
                p[index] = value;
            }
        }
    }

    /// <summary>
    /// This specific version of a SuitRankCollection is a fraction faster in cloning, uses bytes to store data while allowing int in the interface
    /// </summary>
    [Obsolete("use the generic SuitRankCollection")]
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

    public class SuitRankCollection<T>
    {
        private readonly T[] data = new T[52];
        private readonly int typeSize = -1;

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
            for (int i = 0; i < 52; i++)
            {
                data[i] = initialValue;
            }
        }

        public T this[Suits suit, Ranks rank]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetValue(suit, rank);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetValue(suit, rank, value);
        }

        public T this[int suit, int rank]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return data[Index(suit, rank)];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                data[Index(suit, rank)] = value;
            }
        }

        // do not expose the internal storage structure
        //public T this[int suitRank]
        //{
        //    get
        //    {
        //        return data[suitRank];
        //    }
        //    set
        //    {
        //        data[suitRank] = value;
        //    }
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetValue(Suits suit, Ranks rank)
        {
            return (T)data[Index(suit, rank)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(Suits suit, Ranks rank, T value)
        {
            data[Index(suit, rank)] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(Suits suit, Ranks rank)
            => ((int)rank << 2) | (int)suit;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(int suit, int rank)
            => (rank << 2) | suit;

        public SuitRankCollection<T> Clone()
        {
            SuitRankCollection<T> result = new(this.typeSize);

            if (this.typeSize > 0)
            {
                System.Buffer.BlockCopy(this.data, 0, result.data, 0, typeSize);
            }
            else
            {
                for (int i = 0; i < 52; i++)
                {
                    result.data[i] = this.data[i];
                }
            }

            return result;
        }
    }

    [DebuggerDisplay("{DisplayValue}")]
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

    [DebuggerDisplay("{DisplayValue}")]
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

    [DebuggerDisplay("{DisplayValue}")]
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

    [DebuggerDisplay("{DisplayValue}")]
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

    /// <summary>
    /// =14 = still have the card
    /// >=1 = played the card in trick ...
    /// 
    /// </summary>
    [DebuggerDisplay("{DisplayValue}")]
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
    [DebuggerDisplay("{DisplayValue}")]
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

    /// <summary>
    /// for clubs..notrump (5 suits)
    /// </summary>
    [DebuggerDisplay("{DisplayValue}")]
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

    [DebuggerDisplay("{DisplayValue}")]
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

    [DebuggerDisplay("{DisplayValue}")]
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

    [DebuggerDisplay("{DisplayValue}")]
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
