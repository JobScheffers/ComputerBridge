using System;

namespace Sodes.Bridge.Base
{
    /// <summary>
    /// Summary description for BridgeRobot.
    /// </summary>
    public abstract class StatefulBridgeRobot
    {
        public abstract string[] PreferredConventions();
        public abstract string[] KnownConventions();
        public abstract void PlayConventions(string[] conventions);
        public abstract void InitTournament(Scorings Scoring, int maxTimePerBoard, int maxTimePerCard);
        public abstract void InitDeal(Seats Dealer, Vulnerable vulnerable);
        public abstract void ReceiveCardPosition(Seats Player, Card card);
        public abstract Bid DoBid();
        public abstract void InterpretBid(Bid bid);
        public abstract Card FindCard();
        public abstract void InterpretPlayedCard(Card card);
        public abstract void Exit();
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public abstract string GetTrace();
        public abstract bool Claim();

        /// <summary>
        /// Give the time for tracing purposes
        /// </summary>
        public static string CurrentTime { get { return DateTime.UtcNow.ToString("HH:mm:ss.ff"); } }		// use UtcNow because it is much faster than DateTime.Now
    }

    public abstract class StatelessBridgeRobot : BridgeEventHandlers
    {
        public event LongTraceHandler OnLongTrace;

        //public virtual void HandleReadyForBoardScore(int resultCount, Board2 currentBoard){}
        public virtual void HandleTimeUsed(TimeSpan board, TimeSpan total) { }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public virtual string GetTrace() { return string.Empty; }
        public virtual void Quit() { }
        public virtual void PlayConventions(string[] conventions) { }
        public virtual string[] PreferredConventions() { return null; }
        public virtual string[] KnownConventions() { return null; }
        public virtual void ReceiveCardPosition(Seats seat, Suits suit, Ranks rank) { }
        public virtual void SetPartnersRobot(StatelessBridgeRobot bot) { }
        public virtual void SetPartnersRobot(StatefulBridgeRobot bot) { }

        protected void FireLongTrace(string trace)
        {
            if (this.OnLongTrace != null) this.OnLongTrace(trace);
        }

        public virtual string Explain(Bid bid)
        {
            return string.Empty;
        }

        public virtual void AbortBoard()
        {
        }

        public virtual Exception UnhandledException()
        {
            return null;
        }
    }
}
