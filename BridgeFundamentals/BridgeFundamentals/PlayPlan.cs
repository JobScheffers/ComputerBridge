#undef debugComparer
#if DEBUG
//#define strict
//#define ExtendedPlan
#endif

using System;
using System.Collections.Generic;   // Dictionary
using System.Text;                  // StringBuilder
using Sodes.Base;

namespace Sodes.Bridge.Base
{
    public class SimpleMove
    {
        public Suits Suit;
        public Ranks Rank;

        public SimpleMove() { Suit = (Suits)(-1); Rank = (Ranks)(-1); }
        public SimpleMove(Suits s, Ranks r) { Suit = s; Rank = r; }

        public override string ToString()
        {
            return "" + SuitHelper.ToString(Suit) + Sodes.Bridge.Base.Rank.ToXML(Rank);
        }

        public override bool Equals(object obj)
        {
            if (base.Equals(obj))
            {
                return true;
            }
            else
            {
                SimpleMove move = obj as SimpleMove;
                return (move != null && this.Suit == move.Suit && this.Rank == move.Rank);
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class PlayPlan : CircularList<SimpleMove>
    {
        public int compareDepth;

        public PlayPlan(int playPlanDepth)
            : base(
#if ExtendedPlan
      52
#else
playPlanDepth
#endif
)
        {
            this.compareDepth = playPlanDepth;
        }

        public PlayPlan(string plan)
            : this(plan.Length / 2)
        {
            for (int i = this.compareDepth - 1; i >= 0; i--)
            {
                this.Add(new SimpleMove(SuitHelper.FromXML(plan.Substring(2 * i, 1)), Rank.From(plan.Substring(2 * i + 1, 1))));
            };
        }

        public new void Add(SimpleMove move)
        {
#if strict
      for (int item = 0; item < this.Length; item++)
      {
        if (this[item].Equals(move))
        {
          //System.Diagnostics.Debugger.Break();
          throw new InvalidOperationException(string.Format("{0} already exists in {1}", move, this));
        }
      }
#endif
            base.Add(move);
        }

        public override string ToString()
        {
            return this.Plan2String(this.compareDepth);
        }

        public string FullPlan()
        {
            return this.Plan2String(this.Length);
        }

        private string Plan2String(int lengthToShow)
        {
            StringBuilder s = new StringBuilder(lengthToShow);
            for (int level = lengthToShow; level >= 1; level--)
            {
                if (level <= this.Length && this[level - 1] != null)
                {
                    s.Insert(0, this[level - 1].ToString());
                }
                else
                {
                    s.Insert(0, "  ");
                }
            }

            return s.ToString();
        }

        public PlayPlan Clone()
        {
            PlayPlan result = new PlayPlan(this.compareDepth);
            this.ClonePlanTo(result);
            return result;
        }

        internal bool Contains(SimpleMove card)
        {
            for (int i = 0; i < this.Length; i++)
            {
                if (this[i].Suit == card.Suit && this[i].Rank == card.Rank) return true;
            }

            return false;
        }
    }

    public class PlayPlanComparer : IEqualityComparer<PlayPlan>
    {
        private int currentManFirstTrick;
        private Suits leadSuitFirstTrick;
        private Suits trump;
        private int bestManFirstTrick;
        private Suits bestSuitFirstTrick;
        private Ranks bestRankFirstTrick;

        public PlayPlanComparer(int _currentMan, Suits _leadSuit, Suits _trump, int _bestMan, Suits _bestSuit, Ranks _bestRank)
        {
            this.currentManFirstTrick = _currentMan;
            this.leadSuitFirstTrick = _leadSuit;
            this.trump = _trump;
            this.bestManFirstTrick = _bestMan;
            this.bestSuitFirstTrick = _bestSuit;
            this.bestRankFirstTrick = _bestRank;
        }

        //public int Compare(PlayPlan x, PlayPlan y)
        //{
        //  throw new FatalBridgeException("PlayPlanComparer.Compare");
        //}

        public bool Equals(PlayPlan x, PlayPlan y)
        {
#if debugComparer
      string p1 = "K3SQK8K6K9K7S5";
      string p2 = "S5SQK8S7K9K6K3";
      string _x = "";
      string _y = "";
      for (int _depth = 0; _depth < PlayPlan.playPlanDepth; _depth++)
      {
        _x += x[_depth];
        _y += y[_depth];
      }
      if ((_x == p1 && _y == p2) || (_x == p2 && _y == p1))
        System.Diagnostics.Debugger.Break();
      if (_x == p2 || _y == p2)
        System.Diagnostics.Debugger.Break();
#endif
            if (x == y)
                return true;

            //      if (x.Length != y.Length)
            //      {
            //#if DEBUG
            //        //System.Diagnostics.Debugger.Break();
            //#endif
            //        return false;
            //      }

            bool match = true;
            int man = currentManFirstTrick;
            Suits leadSuit = man == 1 ? x[0].Suit : leadSuitFirstTrick;
            int xHighest = -1;
            int yHighest = -1;
            int depth = 0;
            int maxDepth = Math.Min(x.compareDepth, Math.Min(x.Length, y.Length));
            bool leadersCard = true;
            Suits xBestSuit = Suits.NoTrump;
            Ranks xBestRank = Ranks.Ace;
            Suits yBestSuit = Suits.NoTrump;
            Ranks yBestRank = Ranks.Ace;

            while (match && depth < maxDepth && x[depth] != null && y[depth] != null)
            {
                //if (depth > (currentManFirstTrick == 1 ? 0 : 4-currentManFirstTrick) 
                //    && (((man == 1 || man == 3) && !leadersCard) || ((man == 2 || man == 4) && leadersCard)))
                //{   // Somewhere in my plan I have given away a trick; after that all plans are equal
                //  match = true;
                //  depth = x.Length;
                //  break;
                //}

                if (man == 1 || depth == 0)
                {
                    /// first determine which man wins the trick
                    int minMan = Math.Max(1, man - depth);
                    int maxMan = Math.Min(4, man + maxDepth - 1 - depth);
                    if (depth == 0 && bestManFirstTrick > 0)
                    {
                        xHighest = bestManFirstTrick;
                        yHighest = bestManFirstTrick;
                        minMan--;     // nextMan = minMan + 1; 
                        xBestSuit = bestSuitFirstTrick;
                        xBestRank = bestRankFirstTrick;
                        yBestSuit = bestSuitFirstTrick;
                        yBestRank = bestRankFirstTrick;
                    }
                    else
                    {
                        xHighest = minMan;
                        yHighest = minMan;
                        xBestSuit = x[depth].Suit;
                        xBestRank = x[depth].Rank;
                        yBestSuit = y[depth].Suit;
                        yBestRank = y[depth].Rank;
                    }

                    for (int nextMan = minMan + 1; nextMan <= maxMan; nextMan++)
                    {
                        int nextManDepth = depth + nextMan - man;
                        if (x[nextManDepth] != null && y[nextManDepth] != null)
                        {
                            if ((x[nextManDepth].Suit == xBestSuit
                                    && x[nextManDepth].Rank > xBestRank
                                   )
                                || (x[nextManDepth].Suit == trump
                                    && (xBestSuit != trump
                                        || x[nextManDepth].Rank > xBestRank
                                    )
                                   )
                               )
                            {
                                xHighest = nextMan;
                                xBestSuit = x[nextManDepth].Suit;
                                xBestRank = x[nextManDepth].Rank;
                            }

                            if ((y[nextManDepth].Suit == yBestSuit
                                    && y[nextManDepth].Rank > yBestRank
                                   )
                                || (y[nextManDepth].Suit == trump
                                    && (yBestSuit != trump
                                        || y[nextManDepth].Rank > yBestRank
                                    )
                                   )
                               )
                            {
                                yHighest = nextMan;
                                yBestSuit = y[nextManDepth].Suit;
                                yBestRank = y[nextManDepth].Rank;
                            }
                        }
                    }
                }

                if (x[depth].Suit != y[depth].Suit || x[depth].Rank != y[depth].Rank)
                {
                    match = false;
                    if (xHighest == yHighest && x.compareDepth > 1)   // winning card must occur in the same hand in both plans
                    {
                        if (xHighest == man)      // winning card must be same suit but may differ a bit in rank
                        {
                            if (x[depth].Suit == y[depth].Suit
                                && ((int)Math.Abs(x[depth].Rank - y[depth].Rank) <= 1  // win with A differs from win with Q if the K is still with opponents
                                    //|| (x[depth].Suit != leadSuit		// trick is won by a ruff
                                    //    && depth < maxDepth - 1		// must be sure that next man cannot overruff
                                    //    )
                                    || man == 4
                                    || (x.Contains(y[depth])
                                        && y.Contains(x[depth])
                                        && ((x[depth].Rank > y[depth].Rank
                                                && !y.Contains(new SimpleMove(leadSuit, x[depth].Rank - 1))
                                                )
                                            || (x[depth].Rank < y[depth].Rank
                                                && !x.Contains(new SimpleMove(leadSuit, y[depth].Rank - 1))
                                                )
                                            )
                                        // test if there are no cards in between in the plan
                                        // should test for all depths
                                        && (depth + 1 >= maxDepth || x[depth + 1].Rank < y[depth].Rank || x[depth + 1].Suit != leadSuit)
                                        && (depth + 1 >= maxDepth || y[depth + 1].Rank < x[depth].Rank || y[depth + 1].Suit != leadSuit)
                                        )
                                   )
                               )
                            {
                                match = true;
                            }
                        }
                        else
                        {
                            if ((x[depth].Suit != leadSuit && y[depth].Suit != leadSuit
                                    && x[depth].Suit != trump && y[depth].Suit != trump
                                    )
                                || (xBestSuit == trump
                                    && yBestSuit == trump
                                    && x[depth].Suit != trump
                                    && y[depth].Suit != trump
                                    && leadSuit != trump
                                //21-08-05: SAS7C3 was equal to S5S7C3 (where C was trump)
                                    && (x[depth].Suit != y[depth].Suit
                                        || (int)Math.Abs(x[depth].Rank - y[depth].Rank) <= 4
                                //TODO: actually I should test on virtual ranks being equal
                                       )
                                   )
                                )
                            { // in both plans a discard without ruff or no trump when highest is trump
                                // a discard by 'my' team might be a crucial part of the plan!
                                //17-03-05  match = true;
                                //18-03-05  match = depth == 1;   // on depth 1 I know for sure it is not my team
                                //match = true;
                                //22-12-06 a discard in the leaders plan can be crucial, 
                                //          good plans were discarded because they were 'equal' to a bad plan.
                                //          as long as I cannot discover whether this card is from the defense, I must assume
                                //          it is a crucial card.
                                //06-01-07 match = depth == 1 || depth == 3;   // deeper I have to involve who won the first trick
                                if (leadersCard)
                                {
                                    match = false;
                                    if (x[depth].Suit == y[depth].Suit)
                                    {		// same suit is discarded
                                        match = true;			// probably a swap of cards: d6dAd2d3cAc2c3d8 - d8dAd2d3cAc2c3d6
                                    }
                                }
                                else
                                {
                                    match = true;
                                }
                            }
                            else
                            {
                                if (x[depth].Suit == leadSuit && y[depth].Suit == leadSuit)
                                { // both plans follow lead suit, ranks may be different, but should be relative the same to man 2, 3 and 4
                                    if (leadersCard)   // leaders cards must match exactly
                                    {
                                        if (xBestSuit == yBestSuit && xBestRank == yBestRank)
                                        {		// trick is won with the same card
                                            match = true;
                                        }
                                    }
                                    else
                                    {
                                        match = true;
                                        // no match if: this card in plan x higher than any other card in the trick while in plan y lower than a card
                                        // no match if: this card in plan y higher than any other card in the trick while in plan x lower than a card
                                        // x: SK S5 - RJ D5 D4 D2 - S7
                                        // y: SK S5 - RJ D2 D4 RT - S7
                                        //    HA H3 - D7 D6 RA D3 - SJ
                                        //    HA H3 - D7 R9 RA D3 - SJ

                                        //06-01-07: already determined that this card does not win the trick
                                        //          and it follows the leadsuit, so any card is good

                                        //if ((x[depth].Rank > x[depth + xHighest - man].Rank
                                        //        && y[depth].Rank < y[depth + yHighest - man].Rank)
                                        //    || (y[depth].Rank > y[depth + yHighest - man].Rank
                                        //        && x[depth].Rank < x[depth + xHighest - man].Rank)
                                        //  //06-01-07 || ((int)Math.Abs(x[depth].Rank - y[depth].Rank) > 5 && depth != 1 && depth != 3)   // small or very small does not matter for defense
                                        //  || ((int)Math.Abs(x[depth].Rank - y[depth].Rank) > 5)   // small or very small does not matter for defense
                                        //    )
                                        //  match = false;  // 
                                    }
                                }
                                else
                                {
                                    if (x[depth].Suit == trump && y[depth].Suit == trump)
                                    { // both plans ruff, ranks may be different, but should be relative the same to ruff of man 2, 3 and 4
                                        match = true;
                                        // no match if: this ruff in plan x higher than any previous ruff while in plan y lower than a previous ruff
                                        // no match if: this ruff in plan y higher than any previous ruff while in plan x lower than a previous ruff
                                        if (((xBestSuit != trump || xBestRank < x[depth].Rank)
                                                && yBestSuit == trump
                                                && yBestRank > y[depth].Rank
                                                )
                                            || ((yBestSuit != trump || yBestRank < y[depth].Rank)
                                                && xBestSuit == trump
                                                && xBestRank > x[depth].Rank
                                                )
                                            )
                                            match = false;  // 
                                    }
                                    else
                                    { // remains: a discard in plan x and a small card in plan y or vice versa
                                        // x: c2 h2 cA, y: c2 c3 cA - can be the same plan
                                        // I have already checked that the winning card is in the same hand and not this hand
                                        if ((x[depth].Suit != trump && y[depth].Suit != trump) || (leadSuit == trump && man > 1))
                                        { // check for discard or smaller than one of the others
                                            match = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                depth++;
                man++;
                if (match && depth < maxDepth)
                {
                    leadersCard = !leadersCard;
                    if (man == 5)
                    {
                        man = 1;
                        //if (   this.currentManFirstTrick == 1 
                        //    || (   this.bestSuitFirstTrick == this.leadSuitFirstTrick
                        //        && (   (   x[depth + xHighest - 5].Suit == this.bestSuitFirstTrick
                        //                && x[depth + xHighest - 5].Rank > this.bestRankFirstTrick
                        //               )
                        //            || x[depth + xHighest - 5].Suit == this.trump
                        //           )
                        //       )
                        //    || (   this.bestSuitFirstTrick == this.trump
                        //        && x[depth + xHighest - 5].Suit == this.trump
                        //        && x[depth + xHighest - 5].Rank > this.bestRankFirstTrick
                        //       )
                        //   )
                        //{
                        leadersCard = (leadersCard && (xHighest == 1 || xHighest == 3)) || (!leadersCard && (xHighest == 2 || xHighest == 4));
                        //}
                        //else
                        //{
                        //  leadersCard = ((this.bestManFirstTrick % 2) == (this.currentManFirstTrick % 2));
                        //}

                        xHighest = -1;
                        leadSuit = x[depth].Suit;
                    }
                }
            }

            //if (match
            //  && depth < x.compareDepth
            //  && depth < x.Length       // 07-12-06: voor debuggen nodig: plannen van maar 1 lang
            //  && ((x[depth] != null && y[depth] == null)
            //      || (x[depth] == null && y[depth] != null)
            //      )
            //  )
            //{
            //  match = false;
            //}

#if debugComparer
      //if (match)
      //  System.Diagnostics.Debugger.Break();
#endif
            return match;
        }

        public int GetHashCode(PlayPlan obj)
        {
            return 0;
        }
    }

    public class PlayPlans<T>
    {
        private Dictionary<PlayPlan, T> store;
        private PlayPlanComparer comparer;
        //bool needExtendedPlans;

        public PlayPlans(int _currentMan, Suits _leadSuit, Suits _trump, int _bestMan, Suits _bestSuit, Ranks _bestRank)
        {
            //needExtendedPlans = _needExtendedPlans;
            //PlayPlan.needExtendedPlans = _needExtendedPlans;
            //this.playPlanDepth = _needExtendedPlans ? PlayPlan.defaultPlayPlanDepth : 1;
            //PlayPlan.maxDepthForMultiplePlans = _needExtendedPlans ? PlayPlan.defaultDepthForMultiplePlans : 1;
            //comparer = new PlayPlanComparer(needExtendedPlans, _currentMan, _leadSuit, _trump);
            comparer = new PlayPlanComparer(_currentMan, _leadSuit, _trump, _bestMan, _bestSuit, _bestRank);
            store = new Dictionary<PlayPlan, T>(comparer);
        }

        public T this[PlayPlan key]
        {
            get
            {
                return store[key];
            }
            set
            {
#if strict
        if (!this.store.ContainsKey(key))
        {
          foreach (PlayPlan k in this.store.Keys)
          {
            if (k.compareDepth != key.compareDepth) throw new FatalBridgeException("Different type of plan");
          }
        }
#endif

                store[key] = value;
#if strict
        if (Count == 0) throw new System.Exception("");
#endif
            }
        }

        public bool ContainsKey(PlayPlan key)
        {
            return store.ContainsKey(key);
        }

        public Dictionary<PlayPlan, T>.KeyCollection Keys
        {
            get
            {
                return store.Keys;
            }
        }

        public int Count
        {
            get
            {
                return store.Count;
            }
        }
    }
}
