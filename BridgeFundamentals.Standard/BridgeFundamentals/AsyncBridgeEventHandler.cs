using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Bridge
{
#if NET6_0_OR_GREATER
    public abstract class AsyncBridgeEventHandler
    {
        protected string NameForLog;

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

        #region Empty event handlers

        public virtual ValueTask HandleTournamentStarted(Scorings scoring, int maxTimePerBoard, int maxTimePerCard, string tournamentName)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask HandleRoundStarted(SeatCollection<string> participantNames, DirectionDictionary<string> conventionCards)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask HandleBoardStarted(int boardNumber, Seats dealer, Vulnerable vulnerabilty)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask HandleCardPosition(Seats seat, Suits suit, Ranks rank)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask HandleBidDone(Seats source, Bid bid, DateTimeOffset when)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask HandleExplanationNeeded(Seats source, Bid bid)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask HandleExplanationDone(Seats source, Bid bid)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask HandleAuctionFinished(Seats declarer, Contract finalContract)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask HandleCardPlayed(Seats source, Suits suit, Ranks rank, DateTimeOffset when)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask HandleTrickFinished(Seats trickWinner, int tricksForDeclarer, int tricksForDefense)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask HandlePlayFinished(TimeSpan boardByNS, TimeSpan totalByNS, TimeSpan boardByEW, TimeSpan totalByEW)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask HandleTournamentStopped()
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask HandleCardDealingEnded()
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask HandleNeedDummiesCards(Seats dummy)
        {
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask HandleShowDummy(Seats dummy)
        {
            return ValueTask.CompletedTask;
        }

        #endregion
    }

    //public abstract class ClientCommunicationBase : BaseAsyncDisposable
    //{
    //    protected Func<string, ValueTask> processMessage;
    //    protected Seats seat;

    //    public async ValueTask Connect(Func<string, ValueTask> _processMessage, Seats _seat)
    //    {
    //        this.processMessage = _processMessage;
    //        this.seat = _seat;
    //        await this.Connect().ConfigureAwait(false);
    //    }

    //    protected abstract ValueTask Connect();

    //    public abstract ValueTask WriteProtocolMessageToRemoteMachine(string message);

    //    public abstract ValueTask<string> GetResponseAsync();
    //}

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
#endif
}
