#define Olympus

using Bridge;

namespace Bridge.Networking
{
    public static class ProtocolHelper
    {
        internal static string Translate(Vulnerable v)
        {
            switch (v)
            {
                case Vulnerable.Neither:
                    return "Neither";
                case Vulnerable.NS:
                    return "N/S";
                case Vulnerable.EW:
                    return "E/W";
                case Vulnerable.Both:
                    return "Both";
                default:
                    throw new System.ArgumentOutOfRangeException();
            }
        }

        internal static string Translate(Seats s, Distribution d)
        {
            // "North's cards : S A K J 6.H A K J.D 8 6 2.C A 7 6."
            // Meadowlark expects ". " between suits
            var cards = string.Format("'s cards : ");
            for (Suits suit = Suits.Spades; suit >= Suits.Clubs; suit--)
            {
                cards += suit.ToXML();
                for (Ranks rank = Ranks.Ace; rank >= Ranks.Two; rank--)
                {
                    if (d.Owns(s, suit, rank)) cards += " " + rank.ToXML();
                }
                cards += ".";
            }

            return cards;
        }

        public static void HandleProtocolBid(string message, BridgeEventBus bus)
        {
            // North passes
            // North doubles
            // North redoubles
            // North bids 1H
            // North bids 1H Alert. 13 to 19 total points.
            // North bids 1H Alert.C=0-8,D=4-8,H=0-5,S=0-5,HCP=17-19,Total=19-21.
            // North bids 1H.Infos.C=0-8,D=4-8,H=0-5,S=0-5,HCP=17-19,Total=19-21.
            bool bidWasAlerted = false;
            string bidPhrase;
            string alertPhrase = string.Empty;
            int startAlert = message.ToLower().IndexOf("alert.");
            if (startAlert == -1) startAlert = message.ToLower().IndexOf("infos.");
            if (startAlert >= 0)
            {
                bidWasAlerted = true;
                bidPhrase = message.Substring(0, startAlert).Trim();
#if Olympus
                alertPhrase = AlertFromTM(message.Substring(startAlert + 6).Trim());
#else
                alertPhrase = message.Substring(startAlert + 6).Trim();
#endif
            }
            else
            {
                bidWasAlerted = false;
                bidPhrase = message.Trim();
            }

            // 24-07-09: TableMoniteur adds a . after a bid: "North doubles."
            if (bidPhrase.EndsWith(".")) bidPhrase = bidPhrase.Substring(0, bidPhrase.Length - 1);

            string[] answer = bidPhrase.Split(' ');
            Seats bidder = SeatsExtensions.FromXML(answer[0]);
            var bid = new Bid(answer[answer.Length - 1], alertPhrase);
            if (bidWasAlerted)      // && alertPhrase.Length == 0)
            {
                bid.NeedsAlert();
            }

            bus.HandleBidDone(bidder, bid);

            string AlertFromTM(string alert)
            {
                return "";
            }
        }

        public static void HandleProtocolPlay(string message, BridgeEventBus bus)
        {
            // North plays 3C
            string[] answer = message.Split(' ');
            var player = SeatsExtensions.FromXML(answer[0]);
            var suit = SuitHelper.FromXML(answer[2][1]);
            var rank = Rank.From(answer[2][0]);
            bus.HandleCardPosition(player, suit, rank);
            bus.HandleCardPlayed(player, suit, rank);
        }

        internal static string Translate(Bid bid, Seats source)
        {
            string bidText = SeatsExtensions.ToXMLFull(source) + " ";
            switch (bid.Special)
            {
                case SpecialBids.Pass:
                    bidText += "passes";
                    break;
                case SpecialBids.Double:
                    bidText += "doubles";
                    break;
                case SpecialBids.Redouble:
                    bidText += "redoubles";
                    break;
                case SpecialBids.NormalBid:
                    bidText += "bids " + ((int)bid.Level).ToString() + (bid.Suit == Suits.NoTrump ? "NT" : bid.Suit.ToString().Substring(0, 1));
                    break;
            }

            if (bid.Alert)
            {
                bidText += " Alert. " + AlertToTM(bid.Explanation, source);
            }
            else
            {
#if Olympus
                var info = AlertToTM(bid.Explanation, source);
                if (info.Length > 0) bidText += " Infos." + info;
#endif
            }
            return bidText;

            string AlertToTM(string alert, Seats whoseRule)
            {
                string result = "";
#if Olympus
                // pH0510*=H5*!S4*(C4+D4)
                // C=0-9,D=0-9,H=5-5,S=0-3,HCP=04-11,Total=06-11
                //var parseInfo = Rule.Conclude(alert, this.InterpretFactor, this.ConcludeFactor, whoseRule, false);
                //for (Suits s = Suits.Clubs; s <= Suits.Spades; s++)
                //{
                //    result += string.Format("{0}={1:0}-{2:0},", s.ToParser(), parseInfo.L[s].Min, parseInfo.L[s].Max > 9 ? 9 : parseInfo.L[s].Max);
                //}

                //result += string.Format("HCP={0:00}-{1:00},", parseInfo.P.Min, parseInfo.P.Max);
                //result += string.Format("Total={0:00}-{1:00}.", parseInfo.FitPoints.Min, parseInfo.FitPoints.Max);
#endif
                return result;
            }
        }

        internal static string Translate(Bid bid)
        {
            string bidText = " ";
            switch (bid.Special)
            {
                case SpecialBids.Pass:
                    bidText += "passes";
                    break;
                case SpecialBids.Double:
                    bidText += "doubles";
                    break;
                case SpecialBids.Redouble:
                    bidText += "redoubles";
                    break;
                case SpecialBids.NormalBid:
                    bidText += "bids " + ((int)bid.Level).ToString() + (bid.Suit == Suits.NoTrump ? "NT" : bid.Suit.ToString().Substring(0, 1));
                    break;
            }

            return bidText;
        }
    }
}
