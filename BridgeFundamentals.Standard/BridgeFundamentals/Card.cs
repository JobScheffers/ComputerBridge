using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Bridge
{
    public sealed class Card : IEquatable<Card>
    {
        // 0..51
        private readonly byte _index;
        private readonly byte _suit; // 0..3
        private readonly Suits __suit;
        private readonly byte _rank; // 0..12
        private readonly Ranks __rank;
        private readonly byte _hcp; // 0..4

        private Card(byte index)
        {
            _index = index;
            _suit = (byte)(index / 13);
            __suit = (Suits)_suit;
            _rank = (byte)(index % 13);
            __rank = (Ranks)_rank;
            _hcp = (byte)(_rank >= 9 ? _rank - 8 : 0);
        }

        // ---------------- Properties ----------------

        public Suits Suit => __suit;

        public Ranks Rank => __rank;

        public byte Index => _index;

        // ---------------- Comparisons ----------------

        public override bool Equals(object obj) => obj is Card other && Equals(other);

        public bool Equals(Card other) => other is not null && other._index == _index;

        public override int GetHashCode() => _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Card a, Card b) => a._index == b._index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Card a, Card b) => a._index != b._index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Card a, Card b) => a._suit == b._suit && a._rank > b._rank;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Card a, Card b) => a._suit == b._suit && a._rank < b._rank;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Wins(Card a, Card b, Suits trump)
        {
            byte t = (byte)trump;
            return (a._suit == b._suit && a._rank > b._rank)
                || (b._suit != t);
        }

        // ---------------- String ----------------

        public override string ToString()
            => Suit.ToXML().ToLowerInvariant() + RankHelper.ToXML((Ranks)_rank);

        // ---------------- HCP ----------------

        public int HighCardPoints => _hcp;

        // ---------------- Flyweight Deck ----------------

        private static readonly Card[] _deck = CreateDeck();

        private static Card[] CreateDeck()
        {
            var d = new Card[52];
            for (byte i = 0; i < 52; i++)
                d[i] = new Card(i);
            return d;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Card Get(Suits suit, Ranks rank)
            => _deck[(int)suit * 13 + (int)rank];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Card Get(int index)
        {
            if (index == 255) return Null;
            if (index < 0 || index > 51) throw new ArgumentOutOfRangeException(nameof(index), index.ToString());
            return _deck[index];
        }

        // ---------------- Null (optional) ----------------

        public static readonly Card Null = new(255);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNull(Card card)
            => card.Index == 255;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNotNull(Card card)
            => card.Index != 255;
    }

    public readonly struct ExplainedCard(Card _card, string _explanation)
    {
        public readonly Card Card { get; } = _card;

        public readonly string Explanation { get; } = _explanation;
    }

    public sealed class KaartSets
    {
        // 13 bits used: bit 0 -> Ranks.Two, bit 12 -> Ranks.Ace
        private ushort _mask;

        // Map single-char rank representation -> bit index 0..12
        private static readonly Dictionary<char, int> CharToIndex;

        static KaartSets()
        {
            CharToIndex = new Dictionary<char, int>(13);
            int idx = 0;
            for (int r = (int)Ranks.Two; r <= (int)Ranks.Ace; r++, idx++)
            {
                // RankHelper.ToXML returns a string like "2", "3", "T", "J", "Q", "K", "A"
                // We assume the first char is the canonical single-char representation.
                string s = RankHelper.ToXML((Ranks)r);
                if (string.IsNullOrEmpty(s))
                    throw new InvalidOperationException($"RankHelper returned empty for rank {(Ranks)r}.");
                char ch = s[0];
                // If duplicates occur, last wins; normally there are no duplicates.
                CharToIndex[ch] = idx;
            }
        }

        public KaartSets() => _mask = 0;

        public KaartSets(string ranks)
        {
            ArgumentNullException.ThrowIfNull(ranks);
            _mask = 0;
            Add(ranks);
        }

        // Add by Ranks enum
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Ranks rank)
        {
            int bit = (int)rank - (int)Ranks.Two; // 0..12
            if ((uint)bit >= 13u) throw new ArgumentOutOfRangeException(nameof(rank));
            _mask |= (ushort)(1u << bit);
        }

        // Add by VirtualRanks if you use that type
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(VirtualRanks vrank)
        {
            // Convert VirtualRanks -> Ranks if mapping is direct; otherwise adapt here.
            Add((Ranks)vrank);
        }

        // Add by single-char (e.g., "A", "K", "Q", "J", "T", "9", ...)
        public void Add(char rankChar)
        {
            if (CharToIndex.TryGetValue(rankChar, out int bit))
                _mask |= (ushort)(1u << bit);
            else
                throw new ArgumentException($"Unknown rank character '{rankChar}'", nameof(rankChar));
        }

        // Add from string of characters, e.g., "AKQJT9"
        public void Add(string ranks)
        {
            ArgumentNullException.ThrowIfNull(ranks);
            for (int i = 0; i < ranks.Length; i++)
            {
                char ch = ranks[i];
                if (CharToIndex.TryGetValue(ch, out int bit))
                    _mask |= (ushort)(1u << bit);
                else
                    throw new ArgumentException($"Unknown rank character '{ch}' in input", nameof(ranks));
            }
        }

        // Check by Ranks enum
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Ranks rank)
        {
            int bit = (int)rank - (int)Ranks.Two;
            if ((uint)bit >= 13u) return false;
            return (_mask & (1u << bit)) != 0;
        }

        // Check by VirtualRanks
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(VirtualRanks vrank) => Contains((Ranks)vrank);

        // Check by single-char
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(char rankChar)
            => CharToIndex.TryGetValue(rankChar, out int bit) && ((_mask & (1u << bit)) != 0);

        public bool Contains(string ranks)
        {
            return ContainsAll(ranks);
        }

        // Any of the characters present
        public bool ContainsAnyOf(string ranks)
        {
            ArgumentNullException.ThrowIfNull(ranks);
            for (int i = 0; i < ranks.Length; i++)
            {
                if (Contains(ranks[i])) return true;
            }
            return false;
        }

        // All characters present
        public bool ContainsAll(string ranks)
        {
            ArgumentNullException.ThrowIfNull(ranks);
            for (int i = 0; i < ranks.Length; i++)
            {
                if (!Contains(ranks[i])) return false;
            }
            return true;
        }

        public bool IsEmpty() => _mask == 0;

        public void Clear() => _mask = 0;

        // Optional: return textual representation in canonical order Two..Ace
        public override string ToString()
        {
            Span<char> buf = stackalloc char[13];
            int pos = 0;
            for (int r = (int)Ranks.Two; r <= (int)Ranks.Ace; r++)
            {
                int bit = r - (int)Ranks.Two;
                if ((_mask & (1u << bit)) != 0)
                {
                    string s = RankHelper.ToXML((Ranks)r);
                    buf[pos++] = s[0];
                }
            }
            return new string(buf.Slice(0, pos));
        }
    }
}
