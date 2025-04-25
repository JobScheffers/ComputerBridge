using System;
using System.Threading.Tasks;

namespace Bridge.Test.Helpers
{
    public class TestRobot : BridgeRobot
    {
        public TestRobot(Seats seat) : this(seat, null)
        {
        }

        public TestRobot(Seats seat, BridgeEventBus bus) : base(seat, bus)
        {
            this.seat = seat;
        }

        private Seats seat;

        //public override void HandleMyCardPosition(Seats seat, Suits suit, Ranks rank)
        //{
        //}

        public override void HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            if (this.CardCount < 13) throw new InvalidOperationException("not 13 cards");
            base.HandleBidNeeded(whoseTurn, lastRegularBid, allowDouble, allowRedouble);
        }

        /// <summary>
        /// this is just some basic logic to enable testing
        /// (always produces 1C 1S x 2S p p p
        /// override this method and implement your own logic
        /// </summary>
        /// <param name="lastRegularBid"></param>
        /// <param name="allowDouble"></param>
        /// <param name="allowRedouble"></param>
        /// <returns></returns>
        public override async Task<Bid> FindBid(Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            /// 
            await Task.CompletedTask;       // only to prevent a warning while this body is synchronous
            if (lastRegularBid.IsPass) return Bid.C("1C?(!(ps0911*pa0012*D8*o1D*!H4*!S4*r23D5))*(!(ps0911*pa0012*C8*o1C*!H4*!S4*r23C5))*(!(pS1019*pa0612*S7*o7S3*r23S4))*(!(pH1019*pa0612*H7*o7H3*r23H4))*(!(pD1019*pa0712*D7*o7D1*!H4*!S4*r23D4))*(!(pC1019*pa0712*C7*o7C1*!H4*!S4*r23C4))*(!(pS0814*pa0510*(pa0509+th3)*S7*o7S1*!H4*r23S3))*(!(pH0814*pa0512*(pa0509+th3)*H7*o7H0*!S4*r23H3))*(!(pD0714*pa0011*D6*(D7+o7D6*(!S3+!H3))*o7D1*!H4*!S4*r23D3))*(!(pC0714*pa0011*C6*(C7+o7C6*(!S3+!H3))*o7C1*!H4*!S4*r23C3))*(!(S5*!S8*!>HS*o7S1*(!u+S6*(o7S3+th3+pS1011+pa0009))*r23S2*pa0011*pS0413*(pa0010+th3)*!(pS1213*pa1021*(H5+D5+C5+S7))*!(pa1115*=S5*H3*(D4+C4))*!(pa1019*S6*(H4+D4+C4))*!(vS6*pa1240)))*(!(H5*!H8*>HS*o7H1*(!u+H6*(o7H3+th3+pH1011+pa0009))*r23H2*pa0011*pH0413*(pa0010+th3)*!(pH1213*pa1021*(S5+D5+C5+H7))*!(pa1019*H6*(S4+D4+C4))*!(vH6*pa1240)))*(!(D5*!S5*!H5*(!u+D6*o7D1*(o7D3+th3))*r23D2*pa0311*pD0413*(pa0010+th3)))*(!(pN2022*S2*!S6*H2*!H6*D1*(D2+o7D3*C3*!C6*H3*!H5*S3*!S5)*!D7*C1*(C2+o7C3*D3*!D6*H3*!H5*S3*!S5)*!C7*!(S5*(H4*!t07+D5+C5))*!(H5*(S4*!t07+D5+C5))*!(pa2240*(S5+H5))))*(pN1517*pa1418*t07*(t01+th4+th3))");
            if (lastRegularBid.Equals(1, Suits.Clubs)) return Bid.C("1S");
            if (lastRegularBid.Equals(1, Suits.Spades) && allowDouble) return Bid.C("x");
            if (lastRegularBid.Equals(1, Suits.Spades) && !allowDouble) return Bid.C("2S");
            return Bid.C("Pass");
        }

        /// <summary>
        /// finds the first valid card
        /// override this method and implement your own logic
        /// </summary>
        /// <param name="whoseTurn"></param>
        /// <param name="leadSuit"></param>
        /// <param name="trump"></param>
        /// <param name="trumpAllowed"></param>
        /// <param name="leadSuitLength"></param>
        /// <param name="trick"></param>
        /// <returns></returns>
        public override async Task<ExplainedCard> FindCard(Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            await Task.CompletedTask;       // only to prevent a warning while this body is synchronous
            if (leadSuit == Suits.NoTrump || leadSuitLength == 0)
            {   // 1st man or void in lead suit
                for (Suits s = Suits.Clubs; s <= Suits.Spades; s++)
                {
                    for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
                    {
                        if (this.CurrentResult.Distribution.Owns(whoseTurn, s, r))
                        {
                            return new ExplainedCard(CardDeck.Instance[s, r], "test");
                        }
                    }
                }
            }
            else
            {
                for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
                {
                    if (this.CurrentResult.Distribution.Owns(whoseTurn, leadSuit, r))
                    {
                        return new ExplainedCard(CardDeck.Instance[leadSuit, r], "test");
                    }
                }
            }

            throw new InvalidOperationException("BridgeRobot.FindCard: no card found");
        }

        private int CardCount
        {
            get
            {
                int count = 0;
                for (Suits s = Suits.Clubs; s <= Suits.Spades; s++)
                {
                    for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
                    {
                        if (this.CurrentResult.Distribution.Owns(this.seat, s, r))
                        {
                            count++; ;
                        }
                    }
                }

                return count;
            }
        }
    }
}
