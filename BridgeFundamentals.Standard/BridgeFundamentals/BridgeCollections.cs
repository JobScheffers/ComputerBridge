using Bridge.NonBridgeHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace Bridge
{
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

        /// <summary>
        /// Indexer: get -> true if seat owns card.
        /// set = true -> assign card to seat.
        /// set = false -> unassign only if that seat currently owns the card.
        /// </summary>
        public bool this[Seats seat, Suits suit, Ranks rank]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetOwns((int)seat, (int)suit, (int)rank);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetOwns((int)seat, (int)suit, (int)rank, value);
        }

        public bool this[int seat, int suit, int rank]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetOwns(seat, suit, rank);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetOwns(seat, suit, rank, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int? GetOwner(int suit, int rank)
        {
            ValidateSuitRank(suit, rank);
            int cardIndex = CardIndex(suit, rank);
            fixed (byte* p = _data)
            {
                byte v = GetOwnerBits(p, cardIndex);
                return v == UnassignedValue ? (int?)null : v;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetOwner(int? seat, int suit, int rank)
        {
            ValidateSuitRank(suit, rank);
            int cardIndex = CardIndex(suit, rank);
            fixed (byte* p = _data)
            {
                if (seat == null)
                    SetOwnerBits(p, cardIndex, UnassignedValue);
                else
                {
                    if (seat < 0 || seat > 3) throw new ArgumentOutOfRangeException(nameof(seat));
                    SetOwnerBits(p, cardIndex, (int)seat);
                }
            }
        }

        public void Clear()
        {
            fixed (byte* p = _data)
            fixed (byte* src = UnassignedPattern)
            {
                Unsafe.CopyBlockUnaligned(p, src, 20);
            }
        }

        private ulong[] ToSeatBitmasks()
        {
            var masks = new ulong[4];
            fixed (byte* p = _data)
            {
                for (int cardIndex = 0; cardIndex < CardCount; cardIndex++)
                {
                    byte owner = GetOwnerBits(p, cardIndex);
                    if (owner != UnassignedValue)
                        masks[owner] |= 1UL << cardIndex;
                }
            }
            return masks;
        }

        private void FromSeatBitmasks(ulong[] seatMasks)
        {
            ArgumentNullException.ThrowIfNull(seatMasks);
            if (seatMasks.Length != 4) throw new ArgumentException("seatMasks must have length 4", nameof(seatMasks));

            fixed (byte* p = _data)
            {
                fixed (byte* src = UnassignedPattern)
                {
                    Unsafe.CopyBlockUnaligned(p, src, 20);
                }

                for (byte s = 0; s < 4; s++)
                {
                    ulong mask = seatMasks[s];
                    while (mask != 0UL)
                    {
                        int idx = BitOperations.TrailingZeroCount(mask);
                        SetOwnerBits(p, idx, s);
                        mask &= mask - 1;
                    }
                }
            }
        }

        public IEnumerable<(int suit, int rank)> EnumerateCards(int seat)
        {
            if (seat < 0 || seat > 3) throw new ArgumentOutOfRangeException(nameof(seat));
            var result = new List<(int suit, int rank)>(13);
            fixed (byte* p = _data)
            {
                for (int cardIndex = 0; cardIndex < CardCount; cardIndex++)
                {
                    if (GetOwnerBits(p, cardIndex) == (byte)seat)
                    {
                        int suit = cardIndex / 13;
                        int rank = cardIndex % 13;
                        result.Add((suit, rank));
                    }
                }
            }
            return result;
        }

        public override string ToString()
        {
            int[] counts = new int[5]; // 0..3 seats, 4 unassigned
            fixed (byte* p = _data)
            {
                for (int i = 0; i < CardCount; i++) counts[GetOwnerBits(p, i)]++;
            }
            return $"S0={counts[0]} S1={counts[1]} S2={counts[2]} S3={counts[3]} Unassigned={counts[4]}";
        }

        public string ToPBN()
        {
            var result = new StringBuilder(70);
            result.Append("N:");
            foreach (Seats hand in SeatsExtensions.SeatsAscending)
            {
                foreach (var suit in SuitHelper.StandardSuitsDescending)
                {
                    foreach (var rank in RankHelper.RanksDescending)
                    {
                        if (this[hand, suit, rank])
                        {
                            result.Append(RankToPbn(rank));
                        }
                    }

                    if (suit != Suits.Clubs) result.Append('.');
                }

                if (hand != Seats.West) result.Append(' ');
            }

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
        private static byte GetOwnerBits(byte* p, int cardIndex)
        {
            int bitPos = cardIndex * BitsPerCard;
            int byteIndex = bitPos >> 3; // /8
            int shift = bitPos & 7;      // bit offset within byte
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetOwnerBits(byte* p, int cardIndex, int owner)
        {
            int bitPos = cardIndex * BitsPerCard;
            int byteIndex = bitPos >> 3;
            int shift = bitPos & 7;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GetOwns(int seat, int suit, int rank)
        {
            ValidateSeatSuitRank(seat, suit, rank);
            int cardIndex = CardIndex(suit, rank);
            fixed (byte* p = _data)
            {
                return GetOwnerBits(p, cardIndex) == (byte)seat;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetOwns(int seat, int suit, int rank, bool value)
        {
            ValidateSeatSuitRank(seat, suit, rank);
            int cardIndex = CardIndex(suit, rank);
            fixed (byte* p = _data)
            {
                if (value)
                {
                    SetOwnerBits(p, cardIndex, seat);
                }
                else
                {
                    if (GetOwnerBits(p, cardIndex) == (byte)seat)
                        SetOwnerBits(p, cardIndex, UnassignedValue);
                }
            }
        }

        //private string DisplayValue => ToString();
        private static string DisplayValue => "fixed array";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CardIndex(int suit, int rank) => suit * 13 + rank;

        [MethodImpl(MethodImplOptions.AggressiveInlining), Conditional("DEBUG")]
        private static void ValidateSeatSuitRank(int seat, int suit, int rank)
        {
            if (seat < 0 || seat > 3) throw new ArgumentOutOfRangeException(nameof(seat));
            ValidateSuitRank(suit, rank);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Conditional("DEBUG")]
        private static void ValidateSuitRank(int suit, int rank)
        {
            if (suit < 0 || suit > 3) throw new ArgumentOutOfRangeException(nameof(suit));
            if (rank < 0 || rank > 12) throw new ArgumentOutOfRangeException(nameof(rank));
        }

        /// <summary>
        /// Construct from big-endian random bytes (most-significant byte first).
        /// </summary>
        public Deal(byte[] bigEndianRandom) : this()
        {
            ArgumentNullException.ThrowIfNull(bigEndianRandom);
            if (bigEndianRandom.Length == 0) throw new ArgumentException("random bytes must not be empty", nameof(bigEndianRandom));

            byte[] tmp = new byte[bigEndianRandom.Length + 1];
            for (int i = 0; i < bigEndianRandom.Length; i++)
                tmp[tmp.Length - 1 - i] = bigEndianRandom[i];
            var value = new BigInteger(tmp);

            var modulus = FactCache52[52];
            if (value >= modulus) value %= modulus;

            var remaining = Enumerable.Range(0, 52).ToList();
            int[] permutation = new int[52];

            for (int i = 0; i < 52; i++)
            {
                int k = 51 - i;
                var divisor = FactCache52[k];
                var q = value / divisor;
                value %= divisor;

                int idx = (int)q;
                permutation[i] = remaining[idx];
                remaining.RemoveAt(idx);
            }

            fixed (byte* p = _data)
            {
                for (int i = 0; i < 52; i++)
                {
                    int cardIndex = permutation[i];
                    int seat = i / 13;
                    SetOwnerBits(p, cardIndex, seat);
                }
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

            int[] counts = new int[4];
            int[] remaining = System.Buffers.ArrayPool<int>.Shared.Rent(52);
            int remCount = 0;
            fixed (byte* p = _data)
            {
                for (int i = 0; i < CardCount; i++)
                {
                    byte v = GetOwnerBits(p, i);
                    if (v == UnassignedValue)
                    {
                        remaining[remCount++] = i;
                    }
                    else
                    {
                        counts[v]++;
                    }
                }
            }

            int n = remCount;
            if (n == 0)
            {
                System.Buffers.ArrayPool<int>.Shared.Return(remaining);
                return;
            }

            var fact = new BigInteger[n + 1];
            fact[0] = BigInteger.One;
            for (int i = 1; i <= n; i++) fact[i] = fact[i - 1] * i;

            var modulus = fact[n];
            if (value >= modulus) value %= modulus;

            int[] permuted = System.Buffers.ArrayPool<int>.Shared.Rent(n);
            int poolSize = n;
            for (int i = 0; i < n; i++)
            {
                int k = n - 1 - i;
                var divisor = fact[k];
                var q = value / divisor;
                value %= divisor;
                int idx = (int)q;
                permuted[i] = remaining[idx];
                remaining[idx] = remaining[--poolSize];
            }

            int[] need = new int[4];
            for (int s = 0; s < 4; s++)
            {
                int have = counts[s];
                int want = 13 - have;
                need[s] = Math.Max(0, want);
            }

            int pos = 0;
            fixed (byte* p = _data)
            {
                for (int s = 0; s < 4 && pos < n; s++)
                {
                    int take = Math.Min(need[s], n - pos);
                    for (int t = 0; t < take; t++)
                    {
                        int cardIndex = permuted[pos++];
                        SetOwnerBits(p, cardIndex, s);
                    }
                }

                int seatRound = 0;
                while (pos < n)
                {
                    int cardIndex = permuted[pos++];
                    int assignedSeat = -1;
                    for (int attempt = 0; attempt < 4; attempt++)
                    {
                        int s = (seatRound + attempt) & 3;
                        int currentCount = 0;
                        for (int ci = 0; ci < CardCount; ci++)
                            if (GetOwnerBits(p, ci) == (byte)s) currentCount++;
                        if (currentCount < 13)
                        {
                            assignedSeat = s;
                            seatRound = (s + 1) & 3;
                            break;
                        }
                    }
                    if (assignedSeat == -1)
                    {
                        assignedSeat = seatRound;
                        seatRound = (seatRound + 1) & 3;
                    }
                    SetOwnerBits(p, cardIndex, assignedSeat);
                }
            }

            System.Buffers.ArrayPool<int>.Shared.Return(remaining);
            System.Buffers.ArrayPool<int>.Shared.Return(permuted);
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
        /// Fill only the currently unassigned cards using the provided BigInteger.
        /// The BigInteger is converted to a permutation of the remaining cards via Lehmer code.
        /// The permutation is then distributed to seats so each seat ends up with 13 cards.
        /// </summary>
        public void FillUnassignedFromBigEndianBytes(byte[] bigEndianRandom)
        {
            if (bigEndianRandom == null) throw new ArgumentNullException(nameof(bigEndianRandom));
            if (bigEndianRandom.Length == 0) throw new ArgumentException("random bytes must not be empty", nameof(bigEndianRandom));

            byte[] tmp = new byte[bigEndianRandom.Length + 1];
            for (int i = 0; i < bigEndianRandom.Length; i++)
                tmp[tmp.Length - 1 - i] = bigEndianRandom[i];
            var value = new BigInteger(tmp);
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

        // Cached factorials for up to 52
        private static readonly BigInteger[] FactCache52 = CreateFactorials(52);

        // Precomputed 20-byte pattern where every 3-bit slot == UnassignedValue
        private static readonly byte[] UnassignedPattern = CreateUnassignedPattern();

        private static BigInteger[] CreateFactorials(int n)
        {
            var f = new BigInteger[n + 1];
            f[0] = BigInteger.One;
            for (int i = 1; i <= n; i++) f[i] = f[i - 1] * i;
            return f;
        }

        private static byte[] CreateUnassignedPattern()
        {
            var buf = new byte[20];
            for (int card = 0; card < CardCount; card++)
            {
                int bitPos = card * BitsPerCard;
                int byteIndex = bitPos >> 3;
                int shift = bitPos & 7;
                int owner = UnassignedValue & 0x7;
                if (shift <= 5)
                {
                    buf[byteIndex] |= (byte)(owner << shift);
                }
                else
                {
                    int lowBits = 8 - shift;
                    buf[byteIndex] |= (byte)((owner & ((1 << lowBits) - 1)) << shift);
                    buf[byteIndex + 1] |= (byte)(owner >> lowBits);
                }
            }
            return buf;
        }

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

    public class SuitCollection<T>
    {
        private T _clubs, _diamonds, _hearts, _spades, _notrump;

        public SuitCollection()
        {
        }

        public SuitCollection(T initialValue)
        {
            this.Set(initialValue);
        }

        public SuitCollection(T[] initialValues)
        {
            _clubs = initialValues[0];
            _diamonds = initialValues[1];
            _hearts = initialValues[2];
            _spades = initialValues[3];
            _notrump = initialValues[4];
        }

        public ref T this[Suits index]
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (index)
                {
                    case Suits.Clubs: return ref _clubs;
                    case Suits.Diamonds: return ref _diamonds;
                    case Suits.Hearts: return ref _hearts;
                    case Suits.Spades: return ref _spades;
                    case Suits.NoTrump: return ref _notrump;
                    default: throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
        }

        public void Set(T value)
        {
            _clubs = value;
            _diamonds = value;
            _hearts = value;
            _spades = value;
            _notrump = value;
        }
    }

    public struct SuitArray<T> where T : struct
    {
        private T _clubs, _diamonds, _hearts, _spades, _notrump;

        public SuitArray()
        {
        }

        public SuitArray(T initialValue)
        {
            this.Set(initialValue);
        }

        public SuitArray(T[] initialValues)
        {
            _clubs = initialValues[0];
            _diamonds = initialValues[1];
            _hearts = initialValues[2];
            _spades = initialValues[3];
            _notrump = initialValues[4];
        }

        public T this[Suits index]
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (index)
                {
                    case Suits.Clubs: return _clubs;
                    case Suits.Diamonds: return _diamonds;
                    case Suits.Hearts: return _hearts;
                    case Suits.Spades: return _spades;
                    case Suits.NoTrump: return _notrump;
                    default: throw new ArgumentOutOfRangeException(nameof(index), index, "");
                }
            }
            set
            {
                switch (index)
                {
                    case Suits.Clubs: _clubs = value; break;
                    case Suits.Diamonds: _diamonds = value; break;
                    case Suits.Hearts: _hearts = value; break;
                    case Suits.Spades: _spades = value; break;
                    case Suits.NoTrump: _notrump = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(index), index, "");
                }
            }
        }

        public void Set(T value)
        {
            _clubs = value;
            _diamonds = value;
            _hearts = value;
            _spades = value;
            _notrump = value;
        }
    }

    [DebuggerDisplay("{values}")]
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public class SeatCollection<T>
    {
        [DataMember]
        private T[] values = new T[4];

        public SeatCollection()
        {
            foreach (var s in SeatsExtensions.SeatsAscending)
            {
                this[s] = default;
            }
        }

        public SeatCollection(T[] initialValue)
        {
            foreach (var s in SeatsExtensions.SeatsAscending)
            {
                this[s] = initialValue[(int)s];
            }
        }

        [IgnoreDataMember]
        public T this[Seats index]
        {
            get
            {
                return values[(int)index];
            }
            set
            {
                values[(int)index] = value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (Seats s = Seats.North; s <= Seats.West; s++)
                if (this[s] != null)
                    yield return this[s];
        }
    }

    public struct SeatArray<T> where T : struct
    {
        private T _north, _east, _south, _west;

        public SeatArray()
        {
        }

        public SeatArray(T initialValue)
        {
            this.Set(initialValue);
        }

        /// <summary>
        /// Initializes a new instance of the SeatArray<T> class with the specified values for each seat.
        /// </summary>
        /// <remarks>The order of elements in the array determines the value assigned to each seat. If the
        /// array does not contain exactly five elements, the behavior is undefined.</remarks>
        /// <param name="initialValues">An array containing the initial values for the seats. Must contain exactly four elements,
        /// element 0 for North, element 1 for East, element 2 for South, and element 3 for West.</param>
        public SeatArray(T[] initialValues)
        {
            _north = initialValues[0];
            _east = initialValues[1];
            _south = initialValues[2];
            _west = initialValues[3];
        }

        public T this[Seats index]
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (index)
                {
                    case Seats.North: return _north;
                    case Seats.East: return _east;
                    case Seats.South: return _south;
                    case Seats.West: return _west;
                    default: throw new ArgumentOutOfRangeException(nameof(index), index, "");
                }
            }
        }

        public void Set(T value)
        {
            _north = value;
            _east = value;
            _south = value;
            _west = value;
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

        //public T this[int suit, int rank]
        //{
        //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //    get
        //    {
        //        return data[Index(suit, rank)];
        //    }

        //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //    set
        //    {
        //        data[Index(suit, rank)] = value;
        //    }
        //}

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

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private static int Index(int suit, int rank)
        //    => (rank << 2) | suit;

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

    [DebuggerDisplay("{DisplayValue,nq}")]
    public unsafe struct SuitsRanksArray<T> where T : unmanaged
    {
        private fixed sbyte data[52];   // 4 suits * 13 ranks = 52 entries; assume T can be safely cast to sbyte for storage

        public T this[Suits suit, Ranks rank]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetValue(suit, rank);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetValue(suit, rank, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetValue(Suits suit, Ranks rank)
        {
            return FromSByte(data[Index(suit, rank)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(Suits suit, Ranks rank, T value)
        {
            data[Index(suit, rank)] = ToSByte(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(Suits suit, Ranks rank)
            => ((int)rank << 2) | (int)suit;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(T value)
        {
            var _value = ToSByte(value);
            byte v = unchecked((byte)_value);

            fixed (sbyte* p = data)
            {
                Unsafe.InitBlockUnaligned((void*)p, v, 52);
            }
        }

        public void Fill(Suits suit, T value)
        {
            for (Ranks rank = Ranks.Two; rank <= Ranks.Ace; rank++)
            {
                SetValue(suit, rank, value);
            }
        }

        /// <summary>
        /// replace a value in the array and return the old value
        /// </summary>
        /// <returns>old value</returns>
        public T Replace(Suits suit, Ranks rank, T newValue)
        {
            var oldValue = this[suit, rank];
            this[suit, rank] = newValue;
            return oldValue;
        }

        public T[,] Data
        {
            get
            {
                var result = new T[4, 13];
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

        public override string ToString()
        {
            var result = new StringBuilder(512);
            foreach (Suits s in SuitHelper.StandardSuitsAscending)
            {
                result.Append(s.ToXML());
                result.Append(": ");
                foreach (Ranks r in RankHelper.RanksAscending)
                {
                    var v = this[s, r];
                    result.Append(v);
                    if (r < Ranks.Ace) result.Append(' ');
                }
                if (s < Suits.Spades) result.Append(' ');
            }
            return result.ToString();
        }


        //private string DisplayValue => ToString();
        private static string DisplayValue => "fixed array";

        private static readonly bool IsByte;
        private static readonly bool IsSByte;
        private static readonly bool IsEnum;
        private static readonly bool IsByteEnum;
        private static readonly bool IsSByteEnum;

        static SuitsRanksArray()
        {
            IsByte = typeof(T) == typeof(byte);
            IsSByte = typeof(T) == typeof(sbyte);
            IsEnum = typeof(T).IsEnum;

            if (!IsByte && !IsSByte && !IsEnum) throw new NotSupportedException($"SuitsRanksArray<{typeof(T).Name}> is not supported. Only byte, sbyte, or enum with underlying type byte or sbyte are supported.");
            
            IsByteEnum = IsEnum && Enum.GetUnderlyingType(typeof(T)) == typeof(byte);
            IsSByteEnum = IsEnum && Enum.GetUnderlyingType(typeof(T)) == typeof(sbyte);

            if (IsEnum && !IsByteEnum && !IsSByteEnum) throw new NotSupportedException($"SuitsRanksArray<{typeof(T).Name}> is not supported. Only enum with underlying type byte or sbyte are supported.");
        }

        // ---------------- Conversion helpers ----------------

        private static sbyte ToSByte(T value)
        {
            if (IsByte || IsSByte || IsByteEnum || IsSByteEnum)
                return Unsafe.As<T, sbyte>(ref value);

            // fallback
            return (sbyte)Convert.ToInt32(value);
        }

        private static T FromSByte(sbyte value)
        {
            if (IsByte || IsSByte || IsByteEnum || IsSByteEnum)
                return Unsafe.As<sbyte, T>(ref value);

            // fallback
            return (T)Enum.ToObject(typeof(T), value);
        }
    }

    /// <summary>
    /// only for clubs..spades (4 suits)
    /// </summary>
    [DebuggerDisplay("{DisplayValue,nq}")]
    public unsafe struct SeatsSuitsArray<T> where T : struct
    {
        private fixed sbyte data[16];   // 4 suits × 4 seats

        public T this[Seats seat, Suits suit]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetValue(seat, suit);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetValue(seat, suit, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetValue(Seats seat, Suits suit)
        {
            return FromSByte(data[Index(seat, suit)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(Seats seat, Suits suit, T value)
        {
            data[Index(seat, suit)] = ToSByte(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(Seats seat, Suits suit)
        {
            var index = ((int)suit << 2) | (int)seat; // suit * 4 + seat
            if (index < 0 || index >= 16) throw new IndexOutOfRangeException($"Index out of range: seat={seat}, suit={suit}");
            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(T value)
        {
            sbyte _value = ToSByte(value);
            byte v = unchecked((byte)_value);

            fixed (sbyte* p = data)
            {
                Unsafe.InitBlockUnaligned((void*)p, v, 16);
            }
        }

        //private string DisplayValue => ToString();
        private static string DisplayValue => "fixed array";

        public override string ToString()
        {
            return $"North: {this[Seats.North, Suits.Spades]} {this[Seats.North, Suits.Hearts]} {this[Seats.North, Suits.Diamonds]} {this[Seats.North, Suits.Clubs]} East: {this[Seats.East, Suits.Spades]} {this[Seats.East, Suits.Hearts]} {this[Seats.East, Suits.Diamonds]} {this[Seats.East, Suits.Clubs]} South: {this[Seats.South, Suits.Spades]} {this[Seats.South, Suits.Hearts]} {this[Seats.South, Suits.Diamonds]} {this[Seats.South, Suits.Clubs]} West: {this[Seats.West, Suits.Spades]} {this[Seats.West, Suits.Hearts]} {this[Seats.West, Suits.Diamonds]} {this[Seats.West, Suits.Clubs]}";
        }

        private static readonly bool IsByte;
        private static readonly bool IsSByte;
        private static readonly bool IsEnum;
        private static readonly bool IsByteEnum;
        private static readonly bool IsSByteEnum;

        static SeatsSuitsArray()
        {
            IsByte = typeof(T) == typeof(byte);
            IsSByte = typeof(T) == typeof(sbyte);
            IsEnum = typeof(T).IsEnum;

            if (!IsByte && !IsSByte && !IsEnum) throw new NotSupportedException($"SuitsRanksArray<{typeof(T).Name}> is not supported. Only byte, sbyte, or enum with underlying type byte or sbyte are supported.");

            IsByteEnum = IsEnum && Enum.GetUnderlyingType(typeof(T)) == typeof(byte);
            IsSByteEnum = IsEnum && Enum.GetUnderlyingType(typeof(T)) == typeof(sbyte);

            if (IsEnum && !IsByteEnum && !IsSByteEnum) throw new NotSupportedException($"SuitsRanksArray<{typeof(T).Name}> is not supported. Only enum with underlying type byte or sbyte are supported.");
        }

        // ---------------- Conversion helpers ----------------

        private static sbyte ToSByte(T value)
        {
            if (IsByte || IsSByte || IsByteEnum || IsSByteEnum)
                return Unsafe.As<T, sbyte>(ref value);

            // fallback
            return (sbyte)Convert.ToInt32(value);
        }

        private static T FromSByte(sbyte value)
        {
            if (IsByte || IsSByte || IsByteEnum || IsSByteEnum)
                return Unsafe.As<sbyte, T>(ref value);

            // fallback
            return (T)Enum.ToObject(typeof(T), value);
        }
    }

    /// <summary>
    /// for clubs..notrump (5 suits)
    /// </summary>
    [DebuggerDisplay("{DisplayValue,nq}")]
    public unsafe struct SeatsTrumpsArray<T> where T : Enum
    {
        private fixed sbyte data[20];   // 5 suits × 4 seats

        public T this[Seats seat, Suits suit]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetValue(seat, suit);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetValue(seat, suit, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetValue(Seats seat, Suits suit)
        {
            return FromSByte(data[Index(seat, suit)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(Seats seat, Suits suit, T value)
        {
            data[Index(seat, suit)] = ToSByte(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(Seats seat, Suits suit)
        {
            var index = ((int)suit << 2) | (int)seat; // suit * 4 + seat
            if (index < 0 || index >= 20) throw new IndexOutOfRangeException($"Index out of range: seat={seat}, suit={suit}");
            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(T value)
        {
            sbyte _value = ToSByte(value);
            byte v = unchecked((byte)_value);

            fixed (sbyte* p = data)
            {
                Unsafe.InitBlockUnaligned((void*)p, v, 20);
            }
        }

        private static string DisplayValue => "fixed array";

        public override string ToString()
        {
            return DisplayValue;
        }

        private static readonly bool IsByte;
        private static readonly bool IsSByte;
        private static readonly bool IsEnum;
        private static readonly bool IsByteEnum;
        private static readonly bool IsSByteEnum;

        static SeatsTrumpsArray()
        {
            IsByte = typeof(T) == typeof(byte);
            IsSByte = typeof(T) == typeof(sbyte);
            IsEnum = typeof(T).IsEnum;

            if (!IsByte && !IsSByte && !IsEnum) throw new NotSupportedException($"SuitsRanksArray<{typeof(T).Name}> is not supported. Only byte, sbyte, or enum with underlying type byte or sbyte are supported.");

            IsByteEnum = IsEnum && Enum.GetUnderlyingType(typeof(T)) == typeof(byte);
            IsSByteEnum = IsEnum && Enum.GetUnderlyingType(typeof(T)) == typeof(sbyte);

            if (IsEnum && !IsByteEnum && !IsSByteEnum) throw new NotSupportedException($"SuitsRanksArray<{typeof(T).Name}> is not supported. Only enum with underlying type byte or sbyte are supported.");
        }

        // ---------------- Conversion helpers ----------------

        private static sbyte ToSByte(T value)
        {
            if (IsByte || IsSByte || IsByteEnum || IsSByteEnum)
                return Unsafe.As<T, sbyte>(ref value);

            // fallback
            return (sbyte)Convert.ToInt32(value);
        }

        private static T FromSByte(sbyte value)
        {
            if (IsByte || IsSByte || IsByteEnum || IsSByteEnum)
                return Unsafe.As<sbyte, T>(ref value);

            // fallback
            return (T)Enum.ToObject(typeof(T), value);
        }
    }

    [DebuggerDisplay("{DisplayValue,nq}")]
    public unsafe struct TrickArray<T> where T : Enum
    {
        private fixed sbyte data[52];   // 4 men * 13 tricks = 52 entries

        public T this[int trick, int man]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetValue(trick, man);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetValue(trick, man, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetValue(int trick, int man)
        {
            return FromSByte(data[Index(trick, man)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(int trick, int man, T value)
        {
            data[Index(trick, man)] = ToSByte(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(int trick, int man)
        {
            var index = 4 * trick + man - 5;
            if (index < 0 || index >= 52)
                throw new IndexOutOfRangeException($"TrickArray<{typeof(T).Name}> index out of range: trick={trick} man={man}");
            return index;
        }

        public T this[int lastCard]
        {
            get => FromSByte(data[lastCard]);
            set => this.data[lastCard] = ToSByte(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(T value)
        {
            sbyte _value = ToSByte(value);
            byte v = unchecked((byte)_value);

            fixed (sbyte* p = data)
            {
                Unsafe.InitBlockUnaligned((void*)p, v, 52);
            }
        }

        public override string ToString()
        {
            var result = new StringBuilder(512);
            for (int trick = 1; trick <= 13; trick++)
            {
                result.Append(trick);
                result.Append(": ");
                for (int man = 1; man <= 4; man++)
                {
                    result.Append(this[trick, man].ToString()[0]);
                    if (man < 4) result.Append(',');
                }
                if (trick < 13) result.Append(' ');
            }
            return result.ToString();
        }

        private static string DisplayValue => "fixed array";

        private static readonly bool IsEnum;
        private static readonly bool IsByteEnum;
        private static readonly bool IsSByteEnum;

        static TrickArray()
        {
            IsEnum = typeof(T).IsEnum;

            if (!IsEnum) throw new NotSupportedException($"TrickArray<{typeof(T).Name}> is not supported. Only byte, sbyte, or enum with underlying type byte or sbyte are supported.");

            IsByteEnum = IsEnum && Enum.GetUnderlyingType(typeof(T)) == typeof(byte);
            IsSByteEnum = IsEnum && Enum.GetUnderlyingType(typeof(T)) == typeof(sbyte);

            if (!IsByteEnum && !IsSByteEnum) throw new NotSupportedException($"TrickArray<{typeof(T).Name}> is not supported. Only enum with underlying type byte or sbyte are supported.");
        }

        // ---------------- Conversion helpers ----------------

        private static sbyte ToSByte(T value)
        {
            return Unsafe.As<T, sbyte>(ref value);
        }

        private static T FromSByte(sbyte value)
        {
            return Unsafe.As<sbyte, T>(ref value);
        }
    }

    [DebuggerDisplay("{DisplayValue,nq}")]
    public unsafe struct SeatsSuitsRanksArray<T> where T : struct
    {
        private fixed sbyte data[208];

        // benchmark showed that using a fixed array with index calculation is almost 2x faster than using a 3D array, and also uses much less memory (256 bytes vs 4*4*13*sizeof(T))
        // a fixed array is 1.12x faster than a 1D array with index calculation

        public T this[Seats seat, Suits suit, Ranks rank]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetValue((int)seat, (int)suit, (int)rank);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetValue((int)seat, (int)suit, (int)rank, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetValue(int seat, int suit, int rank)
        {
            return FromSByte(data[Index(seat, suit, rank)]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(int seat, int suit, int rank, T value)
        {
            data[Index(seat, suit, rank)] = ToSByte(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Index(int seat, int suit, int rank)
            => seat | suit << 2 | rank << 4;
        //{
        //    var index = seat + 4 * suit + 16 * rank;
        //    if (index < 0 || index >= 208) throw new IndexOutOfRangeException($"seat={seat}, suit={suit}, rank={rank}, index={index}");
        //    var index2 = seat | suit << 2 | rank << 4;
        //    if (index != index2) throw new InvalidOperationException($"Index calculation mismatch: seat={seat}, suit={suit}, rank={rank}, index1={index}, index2={index2}");
        //    return index;
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(T value)
        {
            sbyte _value = ToSByte(value);
            byte v = unchecked((byte)_value);

            fixed (sbyte* p = data)
            {
                Unsafe.InitBlockUnaligned((void*)p, v, 208);
            }
        }

        public T[,,] Data
        {
            get
            {
                var result = new T[4, 4, 13];
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

        private static string DisplayValue => "fixed array";


        private static readonly bool IsByte;
        private static readonly bool IsSByte;
        private static readonly bool IsEnum;
        private static readonly bool IsByteEnum;
        private static readonly bool IsSByteEnum;

        static SeatsSuitsRanksArray()
        {
            IsByte = typeof(T) == typeof(byte);
            IsSByte = typeof(T) == typeof(sbyte);
            IsEnum = typeof(T).IsEnum;

            if (!IsByte && !IsSByte && !IsEnum) throw new NotSupportedException($"SuitsRanksArray<{typeof(T).Name}> is not supported. Only byte, sbyte, or enum with underlying type byte or sbyte are supported.");

            IsByteEnum = IsEnum && Enum.GetUnderlyingType(typeof(T)) == typeof(byte);
            IsSByteEnum = IsEnum && Enum.GetUnderlyingType(typeof(T)) == typeof(sbyte);

            if (IsEnum && !IsByteEnum && !IsSByteEnum) throw new NotSupportedException($"SuitsRanksArray<{typeof(T).Name}> is not supported. Only enum with underlying type byte or sbyte are supported.");
        }

        // ---------------- Conversion helpers ----------------

        private static sbyte ToSByte(T value)
        {
            if (IsByte || IsSByte || IsByteEnum || IsSByteEnum)
                return Unsafe.As<T, sbyte>(ref value);

            // fallback
            return (sbyte)Convert.ToInt32(value);
        }

        private static T FromSByte(sbyte value)
        {
            if (IsByte || IsSByte || IsByteEnum || IsSByteEnum)
                return Unsafe.As<sbyte, T>(ref value);

            // fallback
            return (T)Enum.ToObject(typeof(T), value);
        }
    }
}
