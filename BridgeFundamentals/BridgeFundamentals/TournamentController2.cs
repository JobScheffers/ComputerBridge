using Sodes.Base;
using System;
using System.Threading.Tasks;

namespace Sodes.Bridge.Base
{
    public class TournamentController2 : BridgeEventHandlers2
    {
        public Board2 currentBoard;
        public BoardResult3 currentResult;
        private int boardNumber;
        private Tournament currentTournament;
        private ParticipantInfo participant;
        private Action onTournamentFinished;

        public TournamentController2(Tournament t, ParticipantInfo p)
        {
            this.currentTournament = t;
            this.participant = p;
            BridgeEventBus.MainEventBus.Link(this);
        }

        public async Task StartTournament(Action onTournamentFinish)
        {
            //Log.Trace("TournamentController2.StartTournament");
            this.boardNumber = 0;
            this.onTournamentFinished = onTournamentFinish;
            BridgeEventBus.MainEventBus.HandleTournamentStarted(this.currentTournament.ScoringMethod, 120, this.participant.MaxThinkTime, this.currentTournament.EventName);
            BridgeEventBus.MainEventBus.HandleRoundStarted(this.participant.PlayerNames.Names, new DirectionDictionary<string>(this.participant.ConventionCardNS, this.participant.ConventionCardWE));
            await this.NextBoard();
        }

        public override async void HandlePlayFinished(BoardResult3 currentResult)
        {
            //Log.Trace("TournamentController2.HandlePlayFinished start");
            await this.currentTournament.SaveAsync(currentResult);
            //Log.Trace("TournamentController2.HandlePlayFinished after SaveAsync");
            await this.NextBoard();
            //Log.Trace("TournamentController2.HandlePlayFinished finished");
        }

        private async Task NextBoard()
        {
            //Log.Trace("TournamentController2.NextBoard start");
            this.boardNumber++;
            this.currentBoard = await this.currentTournament.GetNextBoardAsync(this.boardNumber, this.participant.UserId);
            if (this.currentBoard == null)
            {
                //Log.Trace("TournamentController2.NextBoard no next board");
                BridgeEventBus.MainEventBus.Unlink(this);
                //Log.Trace("TournamentController2.NextBoard after BridgeEventBus.MainEventBus.Unlink");
                this.onTournamentFinished();
                //Log.Trace("TournamentController2.NextBoard after onTournamentFinished");
            }
            else
            {
                //System.Diagnostics.Tracing. Trace.Wr("{0} TournamentController2.NextBoard b={1}", DateTime.UtcNow, this.currentBoard.BoardNumber);
                BridgeEventBus.MainEventBus.HandleBoardStarted(this.currentBoard.BoardNumber, this.currentBoard.Dealer, this.currentBoard.Vulnerable);
                this.currentResult = new BoardResult3(this.currentBoard, this.participant.UserId, this.participant.PlayerNames.Names);
                this.currentResult.Start();
            }
        }
    }

    public class ParticipantInfo
    {
        public Guid UserId { get; set; }
        public string ConventionCardNS { get; set; }
        public string ConventionCardWE { get; set; }
        public int MaxThinkTime { get; set; }
        public Participant PlayerNames { get; set; }
    }

    public delegate void TournamentFinishedHandler();

}
