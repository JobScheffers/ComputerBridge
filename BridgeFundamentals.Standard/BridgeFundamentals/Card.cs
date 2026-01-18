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
            _suit = (byte)(index / 13);
            _rank = (byte)(index % 13);
            _hcp = (byte)(_rank >= 9 ? _rank - 8 : 0);

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
            => _deck[index];

        // ---------------- Null (optional) ----------------

        public static readonly Card Null = null!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNull(Card card)
            => card is null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNotNull(Card card)
            => card is not null;
#pragma warning restore CA2211 // Non-constant fields should not be visible
    }

    public readonly struct ExplainedCard(Card _card, string _explanation)
    {
        public readonly Card Card { get; } = _card;

        public readonly string Explanation { get; } = _explanation;
    }

    //public class CardDeck
    //{
    //    private static readonly Lazy<CardDeck> lazy = new Lazy<CardDeck>(() => new CardDeck());

    //    public static CardDeck Instance { get { return lazy.Value; } }

    //    private static Card[] deck;

    //    private CardDeck()
    //    {
    //        deck = new Card[52];
    //        for (int i = 1; i <= 52; i++)
    //        {
    //            deck[i - 1] = new Card(i - 1);
    //        }
    //    }

    //    public Card this[Suits suit, Ranks rank]
    //    {
    //        get
    //        {
    //            return deck[13 * (int)suit + (int)rank];
    //        }
    //    }

    //    public Card this[int index]
    //    {
    //        get
    //        {
    //            return deck[index];
    //        }
    //    }

    //    public Card this[string card]
    //    {
    //        get
    //        {
    //            return this[SuitHelper.FromXML(card[0]), RankHelper.From(card[1])];
    //        }
    //    }
    //}

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
