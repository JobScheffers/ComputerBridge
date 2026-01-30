using System;
using System.Runtime.CompilerServices;

namespace Bridge
{
    public sealed class Card
    {
        // 0..51
        private readonly byte _index;
        private readonly byte _suit; // 0..3
        private readonly byte _rank; // 0..12
        private readonly byte _hcp; // 0..4

        private Card(byte index)
        {
            _index = index;
            if (index == 255)
            {
                _suit = 255;
                _rank = 255;
                _hcp = 255;
            }
            else
            {
                _suit = (byte)(index / 13);
                _rank = (byte)(index % 13);
                _hcp = (byte)(_rank >= 9 ? _rank - 8 : 0);
            }
        }

        // ---------------- Properties ----------------

        public Suits Suit => (Suits)_suit;

        public Ranks Rank => (Ranks)_rank;

        public int Index => _index;

        // ---------------- Comparisons ----------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Card a, Card b) => a.Index == b.Index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Card a, Card b) => a.Index != b.Index;

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

        // ---------------- Equality ----------------

        public override bool Equals(object obj)
            => obj is Card other && other.Index == _index;

        public override int GetHashCode()
            => _index;

        // ---------------- String ----------------

        public override string ToString()
            => _index == 255 ? "null" : Suit.ToXML().ToLowerInvariant() + RankHelper.ToXML((Ranks)_rank);

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

    public class KaartSets
    {
        private string thisSet;
        public KaartSets(string setje)
        {
            thisSet = setje;
            if (setje.Contains('N', StringComparison.CurrentCulture)) throw new FatalBridgeException("N in KaartSets");
        }
        public bool Contains(VirtualRanks rank)
        {
            string s = RankHelper.ToXML((Ranks)rank);
            return thisSet.Contains(s, StringComparison.CurrentCulture);
        }
        public bool Contains(string ranks)
        {
            bool result = true;
            for (int i = 0; i <= ranks.Length - 1; i++)
            {
                if (thisSet.IndexOf(ranks[i]) < 0) result = false;
            }
            return result;
        }
        public bool ContainsAnyOf(string ranks)
        {
            bool result = false;
            for (int i = 0; i <= ranks.Length - 1; i++)
            {
                if (thisSet.Contains(ranks[i])) result = true;
            }
            return result;
        }
        public void Add(VirtualRanks rank)
        {
            thisSet += RankHelper.ToXML((Ranks)rank);
        }
        public bool IsEmpty()
        {
            return (thisSet.Length == 0);
        }

        public static KaartSets C(string set)
        {
            return new KaartSets(set);
        }
    }
}
