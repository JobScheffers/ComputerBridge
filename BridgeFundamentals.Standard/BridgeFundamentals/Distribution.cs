using System;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Text;		// StringBuilder
using System.Xml.Serialization;

namespace Bridge
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public class DistributionCard
    {
        private Seats theSeat;
        private Suits theSuit;
        private Ranks theRank;
        internal bool played;

        [XmlAttribute("Owner")]
        [DataMember]
        public Seats Seat
        {
            get { return theSeat; }
            set { theSeat = value; }
        }

        [XmlAttribute]
        [DataMember]
        public Suits Suit
        {
            get { return theSuit; }
            set { theSuit = value; }
        }

        [XmlAttribute]
        [DataMember]
        public Ranks Rank
        {
            get { return theRank; }
            set { theRank = value; }
        }
    }

    public enum ShufflingRequirement { Random, GameNS, SlamNS }

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public class Distribution
    {
        private Collection<DistributionCard> deal;
        private int lastCard = -1;

        public Distribution()
        {
        }

        ~Distribution()
        {
            this.deal?.Clear();
            this.deal = null;
        }

        [DataMember]
        public Collection<DistributionCard> Deal
        {
            get
            {
                return deal;
            }
            set
            {
                deal = value;
                this.lastCard = 51;
            }
        }

        public void InitCardDealing()
        {
            if (this.deal == null)
            {
                this.deal = [];
                this.lastCard = -1;
                for (int cardCounter = 1; cardCounter <= 52; cardCounter++)
                {
                    deal.Add(new DistributionCard());
                    deal[cardCounter - 1].Suit = (Suits)((cardCounter - 1) / 13);
                    deal[cardCounter - 1].Rank = (Ranks)((cardCounter - 1) % 13);
                    deal[cardCounter - 1].Seat = Seats.Null;
                }
            }
        }

        public void Give(Seats seat, Suits suit, Ranks rank)
        {
            if (this.deal == null)
            {
                this.InitCardDealing();
            }

            if (Owned(suit, rank) && !this.Owns(seat, suit, rank)) throw new FatalBridgeException($"Distribution.Give: card {rank}{suit} is already owned by {this.Owner(suit, rank)}");

            if (!this.Owns(seat, suit, rank))
            {
                lastCard++;
                if (this.deal[this.lastCard] == null) this.deal[this.lastCard] = new DistributionCard();
                for (int i = lastCard; i <= 51; i++)
                    if (deal[i].Suit == suit && deal[i].Rank == rank)
                    {
                        DistributionCard swap = Deal[i];
                        deal[i] = deal[lastCard];
                        deal[lastCard] = swap;
                        break;
                    }
                deal[lastCard].Seat = seat;
                deal[lastCard].played = false;
            }
        }

        public void Remove(Suits suit, Ranks rank)
        {
            for (int i = 0; i <= lastCard; i++)
                if (deal[i].Suit == suit && deal[i].Rank == rank)
                {
                    (deal[lastCard], deal[i]) = (deal[i], deal[lastCard]);
                    lastCard--;
                    break;
                }
        }

        public void Played(Seats seat, Card card)
        {
            this.Played(seat, card.Suit, card.Rank);
        }

        public void Played(Seats seat, Suits suit, Ranks rank)
        {
            for (int i = 0; i <= lastCard; i++)
            {
                if (deal[i].Seat == seat && deal[i].Suit == suit && deal[i].Rank == rank)
                {
                    deal[i].played = true;
                    return;
                }
            }

            throw new FatalBridgeException(string.Format("{0} does not own {1}{2}", seat, suit, rank));
        }

        public bool Owns(Seats seat, Suits suit, Ranks rank)
        {
            for (int i = 0; i <= lastCard; i++)
                if (deal[i].Seat == seat && !deal[i].played && deal[i].Suit == suit && deal[i].Rank == rank)
                    return true;
            return false;
        }

        public bool Owns(Seats seat, Suits suit)
        {
            for (int i = 0; i <= lastCard; i++)
                if (deal[i].Seat == seat && !deal[i].played && deal[i].Suit == suit)
                    return true;
            return false;
        }

        public bool Owns(Seats seat, Card card)
        {
            return Owns(seat, card.Suit, card.Rank);
        }
        public bool Owned(Seats seat, Suits suit, Ranks rank)
        {
            for (int i = 0; i <= lastCard; i++)
                if (deal[i].Seat == seat && deal[i].Suit == suit && deal[i].Rank == rank)
                    return true;
            return false;
        }
        public bool Owned(Suits suit, Ranks rank)
        {
            for (int i = 0; i <= lastCard; i++)
                if (deal[i].Suit == suit && deal[i].Rank == rank)
                    return true;
            return false;
        }
        public int Length(Seats seat, Suits suit)
        {
            int result = 0;
            for (int i = 0; i <= lastCard; i++)
                if (deal[i].Seat == seat && !deal[i].played && deal[i].Suit == suit)
                    result++;
            return result;
        }
        public int Length(Seats seat)
        {
            int result = 0;
            for (int i = 0; i <= lastCard; i++)
                if (deal[i].Seat == seat && !deal[i].played)
                    result++;
            return result;
        }
        public Seats Owner(Suits suit, Ranks rank)
        {
            for (int i = 0; i <= lastCard; i++)
                if (!deal[i].played && deal[i].Suit == suit && deal[i].Rank == rank)
                    return deal[i].Seat;
            throw new FatalBridgeException("No owner found for " + suit.ToString() + " " + rank.ToString());
        }
        //-------------------------------------------------------------------------------
        public void DealRemainingCards(ShufflingRequirement requirement)
        {
            this.InitCardDealing();
            Seats receiver = Seats.North;
            while (lastCard < 51)
            {
                if (Length(receiver) < 13)
                {
                    int randomCard = 51 - RandomGenerator.Instance.Next(51 - lastCard);
                    Give(receiver, deal[randomCard].Suit, deal[randomCard].Rank);
                }

                receiver = receiver.Next();
            }
        }
        public bool Incomplete { get { return lastCard < 51; } }

        public void Restore()
        {
            this.InitCardDealing();
            for (int cardCounter = 1; cardCounter <= 52; cardCounter++)
            {
                deal[cardCounter - 1].played = false;
            }
        }

        public void Clear()
        {
            lastCard = -1;
        }

        public Distribution Clone()
        {
            Distribution copy = new();
            if (this.deal != null)
            {
                copy.deal = [];
                foreach (var item in this.deal)
                {
                    var c = new DistributionCard
                    {
                        Seat = item.Seat,
                        Suit = item.Suit,
                        Rank = item.Rank
                    };
                    copy.deal.Add(c);
                }
            }

            copy.lastCard = this.lastCard;
            return copy;
        }

        public override string ToString()
        {
            StringBuilder result = new();
            if (this.deal != null)
            {
                foreach (var suit in SuitHelper.StandardSuitsDescending)
                {
                    result.Append("       ");
                    this.SeatSuit2String(Seats.North, suit, result);
                    result.AppendLine();
                }

                foreach (var suit in SuitHelper.StandardSuitsDescending)
                {
                    this.SeatSuit2String(Seats.West, suit, result);
                    result.Append("   ");
                    this.SeatSuit2String(Seats.East, suit, result);
                    result.AppendLine();
                }

                foreach (var suit in SuitHelper.StandardSuitsDescending)
                {
                    result.Append("       ");
                    this.SeatSuit2String(Seats.South, suit, result);
                    result.AppendLine();
                }
            }

            return result.ToString();
        }

        public string ToPbn()
        {
            // N:AK.AQT53.T82.A93 QT3.92.9643.KQT4 98.K76.AKJ7.J872 J76542.J84.Q5.65
            string result = "N:";
            foreach (Seats seat in SeatsExtensions.SeatsAscending)
            {
                foreach (var suit in SuitHelper.StandardSuitsDescending)
                {
                    foreach (var rank in RankHelper.RanksDescending)
                    {
                        if (this.Owns(seat, suit, rank))
                        {
                            result += rank.ToXML();
                        }
                    }

                    if (suit > Suits.Clubs) result += ".";
                }

                if (seat < Seats.West) result += " ";
            }

            return result;
        }

        private void SeatSuit2String(Seats seat, Suits suit, StringBuilder result)
        {
            result.Append(SuitHelper.ToXML(suit) + " ");
            int length = 0;
            foreach (var rank in RankHelper.RanksDescending)
            {
                if (this.Owns(seat, suit, rank))
                {
                    result.Append(RankHelper.ToXML(rank));
                    length++;
                }
            }

            for (int l = length + 1; l <= 13; l++)
            {
                result.Append(' ');
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is not Distribution board) return false;
            if (this.lastCard != board.lastCard) return false;
            for (int i = 0; i <= this.lastCard; i++)
            {
                if (this.deal[i].Rank != board.deal[i].Rank) return false;
                if (this.deal[i].Seat != board.deal[i].Seat) return false;
                if (this.deal[i].Suit != board.deal[i].Suit) return false;
                if (this.deal[i].played != board.deal[i].played) return false;
            }
            return true;
        }

        /// <summary>
        /// Required when overriding Equals
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
