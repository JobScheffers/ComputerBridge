using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Bridge
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public struct PlayRecord
    {
        [IgnoreDataMember]
        public Seats seat;

        [DataMember]
        public Suits Suit;

        [DataMember]
        public Ranks Rank;

        [IgnoreDataMember]
        public int man;

        [IgnoreDataMember]
        public int trick;

        [IgnoreDataMember]
        public string Comment;

        public override string ToString()
        {
            return SuitHelper.ToParser(this.Suit).ToLowerInvariant() + Bridge.RankHelper.ToXML(this.Rank);
        }
    }

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public class PlaySequence
    {
        private short lastPlay;
        private Contract finalContract;
        private Seats declarer;
        private Seats declarersPartner;
        private PlayRecord2 play2;

        public PlaySequence(Contract bidResult, int tricksRemaining)
            : this()
        {
            finalContract = bidResult ?? throw new ArgumentNullException("bidResult");
            declarer = finalContract.Declarer;
            declarersPartner = declarer.Partner();
            whoseTurn = declarer.Next();
            leadSuit = Suits.NoTrump;
            remainingTricks = (byte)tricksRemaining;
        }

        /// <summary>
        /// Special constructor for unit tests that want to evaluate somewhere in the middle of the game
        /// </summary>
        /// <param name="bidResult"></param>
        /// <param name="tricksRemaining"></param>
        /// <param name="_whoseTurn"></param>
        public PlaySequence(Contract bidResult, int tricksRemaining, Seats _whoseTurn)
            : this(bidResult, tricksRemaining)
        {
            this.whoseTurn = _whoseTurn;
        }

        /// <summary>
        /// Only for serializer
        /// </summary>
        public PlaySequence()
        {
            lastPlay = -1;
            currentTrick = 1;
            man = 1;
        }

        [DataMember]
        internal List<PlayRecord> play
        {
            get
            {
                return AllCards;
            }
            private set
            {
                // Only called trhough deserialization
                lastPlay = -1;
                foreach (var item in value)
                {
                    lastPlay++;
                    play2.Seat[lastPlay] = item.seat;
                    play2.Suit[lastPlay] = item.Suit;
                    play2.Rank[lastPlay] = item.Rank;
                }
            }
        }

        public Contract Contract
        {
            get
            {
                return finalContract;
            }
        }

        [IgnoreDataMember]
        public byte man;

        [IgnoreDataMember]
        public byte currentTrick;

        [IgnoreDataMember]
        public Seats whoseTurn;
        [IgnoreDataMember]
        public Suits leadSuit;
        [IgnoreDataMember]
        public Seats bestMan;
        [IgnoreDataMember]
        public Suits bestSuit;
        [IgnoreDataMember]
        public Ranks bestRank;
        [IgnoreDataMember]
        public byte remainingTricks;

        [IgnoreDataMember]
        public bool PlayEnded
        {
            get
            {
                return this.finalContract.Bid.IsPass || this.currentTrick > 13;
            }
        }

        [IgnoreDataMember]
        public bool DeclarersTurn { get { return whoseTurn == declarer || whoseTurn == declarersPartner; } }

        [IgnoreDataMember]
        public Suits Trump { get { return this.finalContract.Bid.Suit; } }

        [IgnoreDataMember]
        public int CompletedTricks { get { return (lastPlay < 3) ? 0 : (lastPlay + 1) / 4; } }

        public void Record(Seats s, Suits c, Ranks r, string comment)
        {
            lastPlay++;
            //var p = (play.Count == lastPlay ? new PlayRecord() : play[lastPlay]);
            //p.seat = s;
            //p.Suit = c;
            //p.Rank = r;
            //p.man = man;
            //p.trick = currentTrick;
            //p.Comment = comment;
            //if (play.Count == lastPlay) play.Add(p); else play[lastPlay] = p;
            play2.Seat[lastPlay] = s;
            play2.Suit[lastPlay] = c;
            play2.Rank[lastPlay] = r;

            if (man == 1)
            {
                leadSuit = c;
            }

            if ((man == 1) || (c == bestSuit && r > bestRank) || (c != bestSuit && c == finalContract.Bid.Suit))
            {
                bestSuit = c;
                bestRank = r;
                bestMan = s;
            }

            if (man == 4)
            {
                man = 1;
                currentTrick++;
                remainingTricks--;
                leadSuit = Suits.NoTrump;
                whoseTurn = bestMan;
#if strict
				if (this.finalContract.tricksForDeclarer + this.finalContract.tricksForDefense + this.remainingTricks > 12)
					throw new FatalBridgeException("PlaySequence.Record: tricks > 12");
#endif
                if (bestMan == declarer || bestMan == declarersPartner)
                {
                    this.finalContract.tricksForDeclarer++;
                }
                else
                {
                    this.finalContract.tricksForDefense++;
                }
            }
            else
            {
                man++;
                whoseTurn = whoseTurn.Next();
            }
        }

        public void Record(Seats s, Suits c, Ranks r)
        {
            this.Record(s, c, r, "");
        }

        public void Record(Seats s, Card c)
        {
            Record(s, c.Suit, c.Rank, "");
        }
        
        public void Record(Suits c, Ranks r)
        {
            Record(whoseTurn, c, r, "");
        }

        public void Record(Suits c, Ranks r, string comment)
        {
            Record(whoseTurn, c, r, comment);
        }

        public void Record(Card c)
        {
            Record(whoseTurn, c);
        }

        public Card CardWhenPlayed(int trick, Seats seat)
        {
            for (int man = 1; man <= 4; man++)
            {
                //if (IsFuture(trick, man)) throw new FatalBridgeException($"CardPlayed: future card: t={trick} m={man}");
                if (play2.Seat[trick, man] == seat)
                    return CardDeck.Instance[play2.Suit[trick, man], play2.Rank[trick, man]];
            }
            return Card.Null;
        }

        private bool IsFuture(int trick, int man) => lastPlay < Position(trick, man);

        private int Position(int trick, int man) => 4 * trick + man - 5;

        public Card CardPlayed(int trick, Seats seat)
        {
            for (int man = 1; man <= 4; man++)
            {
                if (IsFuture(trick, man)) throw new FatalBridgeException($"CardPlayed: future card: t={trick.ToString()} m={man.ToString()}");
                if (play2.Seat[trick, man] == seat)
                    return CardDeck.Instance[play2.Suit[trick, man], play2.Rank[trick, man]];
            }
            throw new FatalBridgeException("CardPlayed: card not found");
        }

        public Card CardPlayed(int trick, int man)
        {
            var p = Position(trick, man);
            if (lastPlay < p) throw new FatalBridgeException($"CardPlayed: future card: t={trick.ToString()} m={man.ToString()}");
            return CardDeck.Instance[play2.Suit[p], play2.Rank[p]];
        }

        public Seats Player(int trick, int man)
        {
            var p = Position(trick, man);
            if (lastPlay < p) throw new FatalBridgeException($"CardPlayed: future card: t={trick.ToString()} m={man.ToString()}");
            return play2.Seat[p];
        }

        public int WhichMan(int trick, Seats seat)
        {
            for (int man = 1; man <= 4; man++)
            {
                if (IsFuture(trick, man)) throw new FatalBridgeException($"WhichMan: future card: t={trick.ToString()} m={man.ToString()}");
                if (play2.Seat[trick, man] == seat)
                    return man;
            }
            throw new FatalBridgeException("Man: player not found");
        }

        public int PlayedInTrick(Card card)
        {
            return this.PlayedInTrick(card.Suit, card.Rank);
        }

        public int PlayedInTrick(Suits s, Ranks r)
        {
            for (int i = 0; i <= lastPlay; i++)
                if (play2.Suit[i] == s && play2.Rank[i] == r)
                    return Trick(i);
            return 14;
        }

        public PlaySequence Clone()
        {
            var n = new PlaySequence
            {
                play2 = this.play2,
                lastPlay = this.lastPlay,
                declarer = this.declarer,
                declarersPartner = this.declarersPartner,
                whoseTurn = this.whoseTurn,
                leadSuit = this.leadSuit,
                remainingTricks = this.remainingTricks,
                man = this.man,
                currentTrick = this.currentTrick,
                bestMan = this.bestMan,
                bestSuit = this.bestSuit,
                bestRank = this.bestRank,
                finalContract = this.finalContract.Clone()
            };
            return n;
        }

        public bool IsLeader(Seats me)
        {
            return this.finalContract.IsLeader(me);
        }

        public void Undo()
        {
            if (man == 1)
            {
                man = 4;
                currentTrick--;
                remainingTricks++;
                if (bestMan == declarer || bestMan == declarersPartner)
                {
                    this.finalContract.tricksForDeclarer--;
                }
                else
                {
                    this.finalContract.tricksForDefense--;
                }

#if strict
				if (this.finalContract.tricksForDeclarer + this.finalContract.tricksForDefense + this.remainingTricks != 13)
					throw new FatalBridgeException("PlaySequence.Undo: tricks <> 13");
#endif
            }
            else
                man--;

            if (man == 1)
                leadSuit = Suits.NoTrump;
            else
            {
                // replay the trick to re-establish bestMan
                for (int m = 1; m < man; m++)
                {
                    int t = lastPlay - (man - m);
                    Suits s = play2.Suit[t];
                    Ranks r = play2.Rank[t];
                    if (man == 4 && m == 1) leadSuit = s;
                    if ((m == 1) || (s == bestSuit && r > bestRank) || (s != bestSuit && s == finalContract.Bid.Suit))
                    {
                        bestSuit = s;
                        bestRank = r;
                        bestMan = play2.Seat[t];
                    }
                }
            }

            whoseTurn = play2.Seat[lastPlay];
            lastPlay--;
        }

        public int AllCardsCount
        {
            get
            {
                return lastPlay + 1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c">0..51</param>
        /// <returns></returns>
        public PlayRecord this[int c]
        {
            get
            {
                return new PlayRecord { man = Man(c), trick = Trick(c), seat = play2.Seat[c], Suit = play2.Suit[c], Rank = play2.Rank[c] };
            }
        }

        public List<PlayRecord> AllCards
        {
            get
            {
                var l = new List<PlayRecord>();
                for (int c = 0; c <= lastPlay; c++)
                {
                    l.Add(new PlayRecord { man = Man(c), trick = Trick(c), seat = play2.Seat[c], Suit = play2.Suit[c], Rank = play2.Rank[c] });
                }

                return l;
            }
        }

        private byte Trick(int l) => (byte)((l / 4) + 1);

        private byte Man(int l) => (byte)((l % 4) + 1);

        public bool TrickEnded
        {
            get
            {
                return this.man == 1;
            }
        }

        public int Length(Seats who, Suits suit)
        {
            int cardsPlayed = 0;
            for (int i = 0; i <= lastPlay; i++)
            {
                if (play2.Suit[i] == suit && play2.Seat[i] == who)
                {
                    cardsPlayed++;
                }
            }

            return cardsPlayed;
        }

        public int LowerCardsCount(Suits suit, Ranks rank)
        {
            int result = 0;
            for (Ranks r = Ranks.Two; r < rank; r++)
            {
                if (this.PlayedInTrick(suit, r) == 14) result++;
            }
            return result;
        }

        public bool HasBeenRuffed(Suits suit)
        {
            for (int trick = 1; trick < this.currentTrick; trick++)
            {
                if (this.CardPlayed(trick, 1).Suit == suit)
                {		// the suit has been led in this trick
                    for (int manInTrick = 2; manInTrick <= 4; manInTrick++)
                    {
                        if (this.CardPlayed(trick, manInTrick).Suit == this.Trump)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public Seats Dummy { get { return this.finalContract.Declarer.Partner(); } }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i <= lastPlay; i++)
            {
                result.Append(SuitHelper.ToParser(play2.Suit[i]).ToLowerInvariant() + Bridge.RankHelper.ToXML(play2.Rank[i]) + " ");
            }

            return result.ToString();
        }


        private struct PlayRecord2
        {
            public TrickArrayOfSeats Seat;
            public TrickArrayOfSuits Suit;
            public TrickArrayOfRanks Rank;
        }
    }

    /// Does not inherit from FatalBridgeException to prevent a Debugger.Break
    public class NoGoodCardFoundException : Exception { }
}
