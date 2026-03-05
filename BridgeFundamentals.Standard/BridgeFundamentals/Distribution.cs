using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Text;		// StringBuilder
using System.Xml.Serialization;

namespace Bridge
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public class DistributionCard
    {
        [XmlAttribute("Owner")]
        [DataMember]
        public Seats Seat { get; set; }

        [XmlAttribute]
        [DataMember]
        public Suits Suit { get; set; }

        [XmlAttribute]
        [DataMember]
        public Ranks Rank { get; set; }

        internal bool played;
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
                for (int cardCounter = 0; cardCounter < 52; cardCounter++)
                {
                    deal.Add(new DistributionCard
                    {
                        Suit = (Suits)(cardCounter / 13),
                        Rank = (Ranks)(cardCounter % 13),
                        Seat = Seats.Null
                    });
                }
            }
        }

        public void Give(Seats seat, Suits suit, Ranks rank)
        {
            if (this.deal == null)
            {
                this.InitCardDealing();
            }

            int cardIndex = FindCard(suit, rank, 0, 51);
            if (cardIndex != -1 && deal[cardIndex].Seat != Seats.Null && deal[cardIndex].Seat != seat)
            {
                throw new FatalBridgeException($"Distribution.Give: card {rank}{suit} is already owned by {deal[cardIndex].Seat}");
            }

            if (cardIndex == -1 || deal[cardIndex].Seat != seat)
            {
                lastCard++;
                if (cardIndex == -1)
                {
                    cardIndex = FindCard(suit, rank, lastCard, 51);
                }

                if (cardIndex > lastCard)
                {
                    (deal[lastCard], deal[cardIndex]) = (deal[cardIndex], deal[lastCard]);
                }

                deal[lastCard].Seat = seat;
                deal[lastCard].played = false;
            }
        }

        public void Remove(Suits suit, Ranks rank)
        {
            int cardIndex = FindCard(suit, rank, 0, lastCard);
            if (cardIndex != -1)
            {
                (deal[lastCard], deal[cardIndex]) = (deal[cardIndex], deal[lastCard]);
                lastCard--;
            }
        }

        public void Played(Seats seat, Card card)
        {
            this.Played(seat, card.Suit, card.Rank);
        }

        public void Played(Seats seat, Suits suit, Ranks rank)
        {
            int cardIndex = FindOwnedCard(seat, suit, rank);
            if (cardIndex != -1)
            {
                deal[cardIndex].played = true;
                return;
            }

            throw new FatalBridgeException($"{seat} does not own {suit}{rank}");
        }

        public bool Owns(Seats seat, Suits suit, Ranks rank)
        {
            return FindOwnedCard(seat, suit, rank) != -1;
        }

        public bool Owns(Seats seat, Suits suit)
        {
            for (int i = 0; i <= lastCard; i++)
            {
                var card = deal[i];
                if (card.Seat == seat && !card.played && card.Suit == suit)
                    return true;
            }
            return false;
        }

        public bool Owns(Seats seat, Card card)
        {
            return Owns(seat, card.Suit, card.Rank);
        }

        public bool Owned(Seats seat, Suits suit, Ranks rank)
        {
            return FindCardBySeat(seat, suit, rank) != -1;
        }

        public bool Owned(Suits suit, Ranks rank)
        {
            return FindCard(suit, rank, 0, lastCard) != -1;
        }

        public int Length(Seats seat, Suits suit)
        {
            int result = 0;
            for (int i = 0; i <= lastCard; i++)
            {
                var card = deal[i];
                if (card.Seat == seat && !card.played && card.Suit == suit)
                    result++;
            }
            return result;
        }

        public int Length(Seats seat)
        {
            int result = 0;
            for (int i = 0; i <= lastCard; i++)
            {
                var card = deal[i];
                if (card.Seat == seat && !card.played)
                    result++;
            }
            return result;
        }

        public Seats Owner(Suits suit, Ranks rank)
        {
            int cardIndex = FindCard(suit, rank, 0, lastCard, onlyUnplayed: true);
            if (cardIndex != -1)
            {
                return deal[cardIndex].Seat;
            }
            throw new FatalBridgeException($"No owner found for {suit} {rank}");
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
            for (int cardCounter = 0; cardCounter < 52; cardCounter++)
            {
                deal[cardCounter].played = false;
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
                for (int i = 0; i < 52; i++)
                {
                    var item = this.deal[i];
                    copy.deal.Add(new DistributionCard
                    {
                        Seat = item.Seat,
                        Suit = item.Suit,
                        Rank = item.Rank,
                        played = item.played
                    });
                }
            }

            copy.lastCard = this.lastCard;
            return copy;
        }

        public override string ToString()
        {
            var result = new StringBuilder(capacity: 256);
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
            var result = new StringBuilder(capacity: 80);
            result.Append("N:");

            foreach (Seats seat in SeatsExtensions.SeatsAscending)
            {
                foreach (var suit in SuitHelper.StandardSuitsDescending)
                {
                    foreach (var rank in RankHelper.RanksDescending)
                    {
                        if (this.Owns(seat, suit, rank))
                        {
                            result.Append(rank.ToXML());
                        }
                    }

                    if (suit > Suits.Clubs) result.Append('.');
                }

                if (seat < Seats.West) result.Append(' ');
            }

            return result.ToString();
        }

        private void SeatSuit2String(Seats seat, Suits suit, StringBuilder result)
        {
            result.Append(SuitHelper.ToXML(suit));
            result.Append(' ');
            int length = 0;
            foreach (var rank in RankHelper.RanksDescending)
            {
                if (this.Owns(seat, suit, rank))
                {
                    result.Append(RankHelper.ToXML(rank));
                    length++;
                }
            }

            result.Append(' ', 13 - length);
        }

        public override bool Equals(object obj)
        {
            if (obj is not Distribution board) return false;
            if (this.lastCard != board.lastCard) return false;
            for (int i = 0; i <= this.lastCard; i++)
            {
                var thisCard = this.deal[i];
                var boardCard = board.deal[i];
                if (thisCard.Rank != boardCard.Rank) return false;
                if (thisCard.Seat != boardCard.Seat) return false;
                if (thisCard.Suit != boardCard.Suit) return false;
                if (thisCard.played != boardCard.played) return false;
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

        // Helper methods to reduce code duplication and improve performance
        private int FindCard(Suits suit, Ranks rank, int start, int end, bool onlyUnplayed = false)
        {
            for (int i = start; i <= end && i < deal.Count; i++)
            {
                var card = deal[i];
                if (card.Suit == suit && card.Rank == rank && (!onlyUnplayed || !card.played))
                    return i;
            }
            return -1;
        }

        private int FindOwnedCard(Seats seat, Suits suit, Ranks rank)
        {
            for (int i = 0; i <= lastCard; i++)
            {
                var card = deal[i];
                if (card.Seat == seat && !card.played && card.Suit == suit && card.Rank == rank)
                    return i;
            }
            return -1;
        }

        private int FindCardBySeat(Seats seat, Suits suit, Ranks rank)
        {
            for (int i = 0; i <= lastCard; i++)
            {
                var card = deal[i];
                if (card.Seat == seat && card.Suit == suit && card.Rank == rank)
                    return i;
            }
            return -1;
        }
    }
}
