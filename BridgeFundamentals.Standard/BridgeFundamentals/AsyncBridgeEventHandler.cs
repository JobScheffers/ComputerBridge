using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bridge
{
    //#if NET6_0_OR_GREATER
    public abstract class AsyncBridgeEventHandler
    {
        protected string NameForLog;
        private readonly List<AsyncBridgeEventHandler> handlers = [];

        public AsyncBridgeEventHandler(string name)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNullOrWhiteSpace(name);
#else
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
#endif
            NameForLog = name;
        }

        public abstract ValueTask Finish();

        public void AddEventHandler(AsyncBridgeEventHandler handler)
        {
            handlers.Add(handler);
        }

        #region Empty event handlers

        public virtual async ValueTask HandleTournamentStarted(Scorings scoring, int maxTimePerBoard, int maxTimePerCard, string tournamentName)
        {
            foreach (var handler in handlers) await handler.HandleTournamentStarted(scoring, maxTimePerBoard, maxTimePerCard, tournamentName);
        }

        public virtual async ValueTask HandleRoundStarted(SeatCollection<string> participantNames, DirectionDictionary<string> conventionCards)
        {
            foreach (var handler in handlers) await handler.HandleRoundStarted(participantNames, conventionCards);
        }

        public virtual async ValueTask HandleBoardStarted(int boardNumber, Seats dealer, Vulnerable vulnerabilty)
        {
            foreach (var handler in handlers) await handler.HandleBoardStarted(boardNumber, dealer, vulnerabilty);
        }

        public virtual async ValueTask HandleCardPosition(Seats seat, Suits suit, Ranks rank)
        {
            foreach (var handler in handlers) await handler.HandleCardPosition(seat, suit, rank);
        }

        public virtual async ValueTask HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            foreach (var handler in handlers) await handler.HandleBidNeeded(whoseTurn, lastRegularBid, allowDouble, allowRedouble);
        }

        public virtual async ValueTask HandleBidDone(Seats source, Bid bid, DateTimeOffset when)
        {
            foreach (var handler in handlers) await handler.HandleBidDone(source, bid, when);
        }

        //public virtual async ValueTask HandleExplanationNeeded(Seats source, Bid bid)
        //{
        //    foreach (var handler in handlers) await handler.HandleExplanationNeeded(source, bid);
        //}

        //public virtual async ValueTask HandleExplanationDone(Seats source, Bid bid)
        //{
        //    foreach (var handler in handlers) await handler.HandleExplanationDone(source, bid);
        //}

        public virtual async ValueTask HandleAuctionFinished(Seats declarer, Contract finalContract)
        {
            foreach (var handler in handlers) await handler.HandleAuctionFinished(declarer, finalContract);
        }

        public virtual async ValueTask HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            foreach (var handler in handlers) await handler.HandleCardNeeded(controller, whoseTurn, leadSuit, trump, trumpAllowed, leadSuitLength, trick);
        }

        public virtual async ValueTask HandleCardPlayed(Seats source, Suits suit, Ranks rank, DateTimeOffset when)
        {
            foreach (var handler in handlers) await handler.HandleCardPlayed(source, suit, rank, when);
        }

        public virtual async ValueTask HandleTrickFinished(Seats trickWinner, int tricksForDeclarer, int tricksForDefense)
        {
            foreach (var handler in handlers) await handler.HandleTrickFinished(trickWinner, tricksForDeclarer, tricksForDefense);
        }

        public virtual async ValueTask HandlePlayFinished(TimeSpan boardByNS, TimeSpan totalByNS, TimeSpan boardByEW, TimeSpan totalByEW)
        {
            foreach (var handler in handlers) await handler.HandlePlayFinished(boardByNS, totalByNS, boardByEW, totalByEW);
        }

        public virtual async ValueTask HandleTournamentStopped()
        {
            foreach (var handler in handlers) await handler.HandleTournamentStopped();
        }

        public virtual async ValueTask HandleCardDealingEnded()
        {
            foreach (var handler in handlers) await handler.HandleCardDealingEnded();
        }

        //public virtual async ValueTask HandleNeedDummiesCards(Seats dummy)
        //{
        //    foreach (var handler in handlers) await handler.HandleNeedDummiesCards(dummy);
        //}

        //public virtual async ValueTask HandleShowDummy(Seats dummy)
        //{
        //    foreach (var handler in handlers) await handler.HandleShowDummy(dummy);
        //}

        #endregion
    }

    public abstract class BaseAsyncDisposable : IAsyncDisposable
    {
        protected bool IsDisposed = false; // To detect redundant calls

        protected abstract ValueTask DisposeManagedObjects();

        // This code added to correctly implement the disposable pattern.
        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in DisposeManagedObjects above.
            await this.DisposeManagedObjects().ConfigureAwait(false);
            GC.SuppressFinalize(this);
            IsDisposed = true;
        }
    }
}
