using System;
using System.Collections.Generic;

namespace Sodes.Bridge.Base
{
    public class BridgeEventBus : BridgeEventHandlers2
    {
        public static BridgeEventBus MainEventBus = new BridgeEventBus();

        private int nextStepUserCount = 0;
        private List<bool> readyForNextStep = new List<bool>();

        public int Register()
        {
            this.readyForNextStep.Add(true);
            return this.nextStepUserCount++;
        }

        protected override bool AllReadyForNextStep()
        {
            foreach (var ready in this.readyForNextStep)
            {
                if (!ready) return false;
            }

            return true;
        }

        public void Ready(int id)
        {
            this.readyForNextStep[id] = true;
            this.ProcessEvents();
        }

        public void NotReady(int id)
        {
            this.readyForNextStep[id] = false;
        }

        private Queue<Action> work = new Queue<Action>();
        private bool busy = false;

        private void ProcessEvents()
        {
            if (this.busy) return;
            this.busy = true;
            //if ()
            while (this.work.Count > 0 && this.AllReadyForNextStep())
            {
                (this.work.Dequeue())();
            }

            this.busy = false;
        }

        protected void ClearEvents()
        {
            this.work.Clear();
        }

        public void Clear()
        {
            this.ClearEvents();
            for (int i = 0; i < this.readyForNextStep.Count; i++)
            {
                this.readyForNextStep[i] = true;
            }
        }

        private void Add(Action toDo)
        {
            this.work.Enqueue(toDo);
            this.ProcessEvents();
        }

        #region Event handlers

        public override void HandleTournamentStarted(Scorings scoring, int maxTimePerBoard, int maxTimePerCard, string tournamentName)
        {
            if (this.OnTournamentStarted != null)
            {
                this.Add(() =>
                {
                    this.OnTournamentStarted.Invoke(scoring, maxTimePerBoard, maxTimePerCard, tournamentName);
                });
            }
        }

        public override void HandleRoundStarted(SeatCollection<string> participantNames, DirectionDictionary<string> conventionCards)
        {
            if (this.OnRoundStarted != null)
            {
                this.Add(() =>
                {
                    this.OnRoundStarted.Invoke(participantNames, conventionCards);
                });
            }
        }

        public override void HandleCardPosition(Seats seat, Suits suit, Ranks rank)
        {
            if (this.OnCardPosition != null)
            {
                this.Add(() =>
                {
                    this.OnCardPosition(seat, suit, rank);
                });
            }
        }

        public override void HandleBoardStarted(int boardNumber, Seats dealer, Vulnerable vulnerabilty)
        {
            this.Add(() =>
            {
                if (this.OnBoardStarted != null)
                {
                    this.OnBoardStarted(boardNumber, dealer, vulnerabilty);
                }
            });
        }

        public override void HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble, Bid givenBid)
        {
            if (this.OnBidNeeded != null)
            {
                this.Add(() =>
                {
                    this.OnBidNeeded(whoseTurn, lastRegularBid, allowDouble, allowRedouble, givenBid);
                });
            }
        }

        public override void HandleBidDone(Seats source, Bid bid)
        {
            this.Add(() =>
            {
                if (this.OnBidDone != null)
                {
                    this.OnBidDone(source, bid);
                }
            });
        }

        public override void HandleAuctionFinished(Seats declarer, Contract finalContract)
        {
            this.Add(() =>
            {
                if (this.OnAuctionFinished != null)
                {
                    this.OnAuctionFinished(declarer, finalContract);
                }
            });
        }

        public override void HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick, Card allowedCard)
        {
            this.Add(() =>
            {
                if (this.OnCardNeeded != null)
                {
                    this.OnCardNeeded(controller, whoseTurn, leadSuit, trump, trumpAllowed, leadSuitLength, trick, allowedCard);
                }
            });
        }

        public override void HandleCardPlayed(Seats source, Suits suit, Ranks rank)
        {
            this.Add(() =>
            {
                if (this.OnCardPlayed != null)
                {
                    this.OnCardPlayed(source, suit, rank);
                }
            });
        }

        public override void HandleTrickFinished(Seats trickWinner, int tricksForDeclarer, int tricksForDefense)
        {
            this.Add(() =>
            {
                if (this.OnTrickFinished != null)
                {
                    this.OnTrickFinished(trickWinner, tricksForDeclarer, tricksForDefense);
                }
            });
        }

        public override void HandlePlayFinished(BoardResult3 currentResult)
        {
            this.Add(() =>
            {
                if (this.OnPlayFinished != null)
                {
                    this.OnPlayFinished(currentResult);
                }
            });
        }

        public override void HandleReadyForNextStep(Seats source, NextSteps readyForStep)
        {
            this.Add(() =>
            {
                if (this.OnReadyForNextStep != null)
                {
                    this.OnReadyForNextStep(source, readyForStep);
                }
            });
        }

        public override void HandleReadyForBoardScore(int resultCount, Board2 currentBoard)
        {
            this.Add(() =>
            {
                if (this.OnOriginalDistributionRestoreFinished != null)
                {
                    this.OnOriginalDistributionRestoreFinished(resultCount, currentBoard);
                }
            });
        }

        public override void HandleTimeUsed(TimeSpan boardByNS, TimeSpan totalByNS, TimeSpan boardByEW, TimeSpan totalByEW)
        {
            this.Add(() =>
            {
                if (this.OnTimeUsed != null)
                {
                    this.OnTimeUsed(boardByNS, totalByNS, boardByEW, totalByEW);
                }
            });
        }

        public override void HandleTournamentStopped()
        {
            this.Add(() =>
            {
                if (this.OnTournamentStopped != null)
                {
                    this.OnTournamentStopped();
                }
            });
        }

        public override void HandleDummiesCardPosition(Suits suit, Ranks rank)
        {
            this.Add(() =>
            {
                if (this.OnDummiesCardPosition != null)
                {
                    this.OnDummiesCardPosition(suit, rank);
                }
            });
        }

        public override void HandleCardDealingEnded()
        {
            this.Add(() =>
            {
                if (this.OnCardDealingEnded != null)
                {
                    this.OnCardDealingEnded();
                }
            });
        }

        public override void HandleNeedDummiesCards(Seats dummy)
        {
            this.Add(() =>
            {
                if (this.OnNeedDummiesCards != null)
                {
                    this.OnNeedDummiesCards(dummy);
                }
            });
        }

        public override void HandleShowDummy(Seats dummy)
        {
            this.Add(() =>
            {
                if (this.OnShowDummy != null)
                {
                    this.OnShowDummy(dummy);
                }
            });
        }

        #endregion

        #region link/unlink

        public event TournamentStartedHandler2 OnTournamentStarted;
        public event RoundStartedHandler OnRoundStarted;
        public event BoardStartedHandler OnBoardStarted;
        public event CardPositionHandler OnCardPosition;
        public event BidNeededHandler OnBidNeeded;
        public event BidDoneHandler OnBidDone;
        public event AuctionFinishedHandler OnAuctionFinished;
        public event CardNeededHandler OnCardNeeded;
        public event CardPlayedHandler2 OnCardPlayed;
        public event TrickFinishedHandler OnTrickFinished;
        public event PlayFinishedHandler2 OnPlayFinished;
        public event ReadyForNextStepHandler OnReadyForNextStep;
        public event TournamentStoppedHandler OnTournamentStopped;
        public event ReadyForBoardScoreHandler OnOriginalDistributionRestoreFinished;
        public event TimeUsedHandler OnTimeUsed;
        public event DummiesCardPositionHandler OnDummiesCardPosition;
        public event CardDealingEndedHandler OnCardDealingEnded;
        public event ShowDummyHandler OnNeedDummiesCards;
        public event ShowDummyHandler OnShowDummy;

        public virtual void Link(BridgeEventHandlers2 other)
        {
            if (other == null) throw new ArgumentNullException("other");
            this.OnTournamentStarted += new TournamentStartedHandler2(other.HandleTournamentStarted);
            this.OnRoundStarted += new RoundStartedHandler(other.HandleRoundStarted);
            this.OnBoardStarted += new BoardStartedHandler(other.HandleBoardStarted);
            this.OnBidNeeded += new BidNeededHandler(other.HandleBidNeeded);
            this.OnBidDone += new BidDoneHandler(other.HandleBidDone);
            this.OnAuctionFinished += new AuctionFinishedHandler(other.HandleAuctionFinished);
            this.OnCardNeeded += new CardNeededHandler(other.HandleCardNeeded);
            this.OnCardPlayed += new CardPlayedHandler2(other.HandleCardPlayed);
            this.OnTrickFinished += new TrickFinishedHandler(other.HandleTrickFinished);
            this.OnPlayFinished += new PlayFinishedHandler2(other.HandlePlayFinished);
            this.OnReadyForNextStep += new ReadyForNextStepHandler(other.HandleReadyForNextStep);
            this.OnOriginalDistributionRestoreFinished += new ReadyForBoardScoreHandler(other.HandleReadyForBoardScore);
            this.OnTimeUsed += new TimeUsedHandler(other.HandleTimeUsed);
            this.OnTournamentStopped += new TournamentStoppedHandler(other.HandleTournamentStopped);
            this.OnCardPosition += new CardPositionHandler(other.HandleCardPosition);
            this.OnDummiesCardPosition += new DummiesCardPositionHandler(other.HandleDummiesCardPosition);
            this.OnCardDealingEnded += new CardDealingEndedHandler(other.HandleCardDealingEnded);
            this.OnNeedDummiesCards += new ShowDummyHandler(other.HandleNeedDummiesCards);
            this.OnShowDummy += new ShowDummyHandler(other.HandleShowDummy);
        }

        public virtual void Unlink(BridgeEventHandlers2 other)
        {
            this.OnTournamentStarted -= new TournamentStartedHandler2(other.HandleTournamentStarted);
            this.OnBoardStarted -= new BoardStartedHandler(other.HandleBoardStarted);
            this.OnBidNeeded -= new BidNeededHandler(other.HandleBidNeeded);
            this.OnBidDone -= new BidDoneHandler(other.HandleBidDone);
            this.OnAuctionFinished -= new AuctionFinishedHandler(other.HandleAuctionFinished);
            this.OnCardNeeded -= new CardNeededHandler(other.HandleCardNeeded);
            this.OnCardPlayed -= new CardPlayedHandler2(other.HandleCardPlayed);
            this.OnTrickFinished -= new TrickFinishedHandler(other.HandleTrickFinished);
            this.OnPlayFinished -= new PlayFinishedHandler2(other.HandlePlayFinished);
            this.OnReadyForNextStep -= new ReadyForNextStepHandler(other.HandleReadyForNextStep);
            this.OnOriginalDistributionRestoreFinished -= new ReadyForBoardScoreHandler(other.HandleReadyForBoardScore);
            this.OnTimeUsed -= new TimeUsedHandler(other.HandleTimeUsed);
            this.OnTournamentStopped -= new TournamentStoppedHandler(other.HandleTournamentStopped);
            this.OnCardPosition -= new CardPositionHandler(other.HandleCardPosition);
            this.OnDummiesCardPosition -= new DummiesCardPositionHandler(other.HandleDummiesCardPosition);
            this.OnCardDealingEnded -= new CardDealingEndedHandler(other.HandleCardDealingEnded);
            this.OnNeedDummiesCards += new ShowDummyHandler(other.HandleNeedDummiesCards);
            this.OnShowDummy -= new ShowDummyHandler(other.HandleShowDummy);
        }

        #endregion
    }
}
