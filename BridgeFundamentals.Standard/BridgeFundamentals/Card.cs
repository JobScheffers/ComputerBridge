using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Bridge
{
    /// <summary>
    /// Summary description for Card.
    /// </summary>
    [DebuggerStepThrough]
    //public class Card
    //{
    //    private string _comment;
    //    private Suits suit;
    //    private Ranks rank;

    //    public Card(Suits suit, Ranks rank)
    //    {
    //        this.suit = suit;
    //        this.rank = rank;
    //    }

    //    public Card(Suits suit, Ranks rank, string comment) : this(suit, rank)
    //    {
    //        this._comment = comment;
    //    }

    //    public Card(string cardDescription) : this(SuitHelper.FromXML(cardDescription.Substring(0, 1)), Bridge.Rank.From(cardDescription.Substring(1, 1)))
    //    {
    //    }
    //    public Card()
    //    {
    //        this.suit = Suits.Clubs;
    //        this.rank = Ranks.Ace;
    //    }

    //    public Suits Suit { get { return this.suit; } /*set { this.suit = value; }*/ }
    //    public Ranks Rank { get { return this.rank; } }
    //    public static bool operator >(Card card1, Card card2)
    //    {
    //        return card1.suit == card2.suit && card1.rank > card2.rank;
    //    }
    //    public static bool operator <(Card card1, Card card2)
    //    {
    //        return card1.suit == card2.suit && card1.rank < card2.rank;
    //    }
    //    public static bool operator ==(Card card1, Card card2)
    //    {
    //        return (((Object)card1 == null && (Object)card2 == null) || ((Object)card1 != null && (Object)card2 != null && card1.suit == card2.suit && card1.rank == card2.rank));
    //    }
    //    public static bool operator ==(Card card1, string card2)
    //    {
    //        return card1 == new Card(card2);
    //    }
    //    public static bool operator !=(Card card1, Card card2)
    //    {
    //        return !(card1 == card2);
    //    }
    //    public static bool operator !=(Card card1, string card2)
    //    {
    //        return !(card1 == card2);
    //    }
    //    public override bool Equals(Object obj)
    //    {
    //        Card c = obj as Card;
    //        return null != c && this == c;
    //    }

    //    public static bool IsNull(object card)
    //    {
    //        return card == null;
    //    }

    //    public override int GetHashCode()
    //    {
    //        return 0;    //re.GetHashCode()  im.GetHashCode();
    //    }
    //    public static bool Wins(Card card1, Card card2, Suits trump)
    //    {
    //        return card1 > card2 || (card1.suit != card2.suit && card1.suit == trump);
    //    }

    //    public override string ToString()
    //    {
    //        return "" + this.suit.ToXML().ToLowerInvariant() + Bridge.Rank.ToXML(Rank);
    //    }

    //    public byte HighCardPoints
    //    {
    //        get
    //        {
    //            return (byte)this.rank.HCP();
    //        }
    //    }
    //    public string Comment
    //    {
    //        get
    //        {
    //            return this._comment;
    //        }
    //    }
    //    public Card Clone()
    //    {
    //        return new Card(Suit, Rank);
    //    }
    //}


    public readonly struct Card
    {
        private readonly byte index;

        public Card(int _index)
        {
            index = (byte)_index;
        }

        public readonly Suits Suit { get { return (Suits)(index / 13); } }

        public readonly Ranks Rank { get { return (Ranks)(index % 13); } }

        public static bool operator >(Card card1, Card card2)
        {
            return card1.Suit == card2.Suit && card1.Rank > card2.Rank;
        }

        public static bool operator <(Card card1, Card card2)
        {
            return card1.Suit == card2.Suit && card1.Rank < card2.Rank;
        }

        public static bool operator ==(Card card1, Card card2)
        {
            return card1.Suit == card2.Suit && card1.Rank == card2.Rank;
        }

        public static bool operator !=(Card card1, Card card2)
        {
            return !(card1 == card2);
        }

        public static bool operator ==(Card card1, string card2)
        {
            return card1.Suit == SuitHelper.FromXML(card2[0]) && card1.Rank == RankHelper.From(card2[1]);
        }

        public static bool operator !=(Card card1, string card2)
        {
            return !(card1 == card2);
        }

        public override bool Equals(Object obj)
        {
            var c = (Card)obj;
            return this == c;
        }

        public static bool IsNull(Card card)
        {
            return card.index == 255;
        }

        public override int GetHashCode()
        {
            return index;
        }

        public static bool Wins(Card card1, Card card2, Suits trump)
        {
            return card1 > card2 || (card1.Suit != card2.Suit && card1.Suit == trump);
        }

        public override string ToString()
        {
            if (index == 255) return "null";
            return "" + this.Suit.ToXML().ToLowerInvariant() + RankHelper.ToXML(Rank);
        }

        public int HighCardPoints
        {
            get
            {
                return this.Rank.HCP();
            }
        }

        public static Card Null = new(255);
    }

    public class CardDeck
    {
        private static readonly Lazy<CardDeck> lazy = new Lazy<CardDeck>(() => new CardDeck());

        public static CardDeck Instance { get { return lazy.Value; } }

        private static Card[] deck;

        private CardDeck()
        {
            deck = new Card[52];
            for (int i = 1; i <= 52; i++)
            {
                deck[i - 1] = new Card(i - 1);
            }
        }

        public Card this[Suits suit, Ranks rank]
        {
            get
            {
                return deck[13 * (int)suit + (int)rank];
            }
        }

        public Card this[string card]
        {
            get
            {
                return this[SuitHelper.FromXML(card[0]), RankHelper.From(card[1])];
            }
        }
    }

    public class KaartSets
    {
        private string thisSet;
        public KaartSets(string setje)
        {
            thisSet = setje;
            if (setje.IndexOf("N") >= 0) throw new FatalBridgeException("N in KaartSets");
        }
        public bool Contains(VirtualRanks rank)
        {
            string s = RankHelper.ToXML((Ranks)rank);
            return thisSet.IndexOf(s) >= 0;
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
                if (thisSet.IndexOf(ranks[i]) >= 0) result = true;
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

    //public class SimpleMove
    //{
    //    public Suits Suit;
    //    public Ranks Rank;

    //    public SimpleMove() { Suit = (Suits)(-1); Rank = (Ranks)(-1); }
    //    public SimpleMove(Suits s, Ranks r) { Suit = s; Rank = r; }

    //    public override string ToString()
    //    {
    //        return "" + Suit.ToXML() + Bridge.Rank.ToXML(Rank);
    //    }

    //    public override bool Equals(object obj)
    //    {
    //        if (base.Equals(obj))
    //        {
    //            return true;
    //        }
    //        else
    //        {
    //            SimpleMove move = obj as SimpleMove;
    //            return (move != null && this.Suit == move.Suit && this.Rank == move.Rank);
    //        }
    //    }

    //    public override int GetHashCode()
    //    {
    //        return base.GetHashCode();
    //    }
    //}
}
