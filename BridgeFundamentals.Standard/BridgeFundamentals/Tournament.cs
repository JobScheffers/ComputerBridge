using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Bridge
{
    public abstract class Tournament
    {
        public Tournament(string name)
            : this()
        {
            this.EventName = name;
        }

        /// <summary>
        /// Only for the serializer
        /// </summary>
        public Tournament()
        {
            this.Created = DateTime.Now;
            this.Boards = new Collection<Board2>();
            this.Participants = new List<Team>();
            this.ScoringMethod = Scorings.scPairs;
            this.AllowReplay = false;
            this.BidContest = false;
            this.AllowOvercalls = true;
        }

        #region Methods

        public Board2 GetNextBoard(int boardNumber, Guid userId)
        {
            return this.GetNextBoardAsync(boardNumber, userId).Result;
        }

        public Board2 FindBoard(int boardNumber)
        {
            foreach (var board in this.Boards)
            {
                if (board.BoardNumber == boardNumber) return board;
            }
            return null;
        }

        public abstract Task<Board2> GetNextBoardAsync(int relativeBoardNumber, Guid userId);

        public abstract Task<Board2> GetBoardAsync(int boardNumber);

        public abstract Task SaveAsync(BoardResult result);

        public Board2 ViewBoard(int boardNumber)
        {
            if (boardNumber < 1) throw new ArgumentOutOfRangeException("boardNumber", boardNumber + " (should be 1 or more)");
            foreach (var board in this.Boards)
            {
                if (board.BoardNumber == boardNumber)
                {
                    return board;
                }
            }

            return null;
        }

        public void CalcTournamentScores()
        {
            if (this.Participants.Count == 0) return;       // no need to calculate scores if there are no participants

            foreach (var team in this.Participants)
            {
                team.InitRecalc();
            }


            if (this.ScoringMethod == Scorings.scPairs)
            {
                foreach (var board in this.Boards)
                {
                    foreach (var result in board.Results)
                    {
                        var team = FindTeam(result.Participants.Names[Seats.South], result.Participants.Names[Seats.North]);
                        team.AddScore(result.TournamentScore);
                    }
                }

                foreach (var team in this.Participants)
                {
                    team.CalcScore();
                }

                this.Participants.Sort(delegate (Team p1, Team p2)
                {
                    return -p1.TournamentScore.CompareTo(p2.TournamentScore);
                });
            }
            else if (this.ScoringMethod == Scorings.scCross || this.ScoringMethod == Scorings.scIMP)
            {
                foreach (var board in this.Boards)
                {
                    if (board.Results.Count == 2)
                    {
                        var score1 = board.Results[0].NorthSouthScore;
                        var score2 = board.Results[1].NorthSouthScore;
                        var imps = Scoring.ToImp(score1 - score2);
                        board.Results[0].TournamentScore = imps;
                        board.Results[1].TournamentScore = -imps;
                        var teamNS = FindTeam(board.Results[0].Participants.Names[Seats.North], board.Results[0].Participants.Names[Seats.South]);
                        if (imps > 0) teamNS.TournamentScore += imps;
                        var teamEW = FindTeam(board.Results[0].Participants.Names[Seats.East], board.Results[0].Participants.Names[Seats.West]);
                        if (imps < 0) teamEW.TournamentScore -= imps;
                    }
                }
            }

            Team FindTeam(string member1, string member2)
            {
                foreach (var team in this.Participants)
                {
                    if (team.IsSame(member1, member2)) return team;
                }

                // corruption in tournament file
                var newTeam = new Team(member1, member2);
                this.Participants.Add(newTeam);
                return newTeam;
            }
        }

        public void AddResults(Tournament t2)
        {
            foreach (var board in this.Boards)
            {
                foreach (var result in t2.ViewBoard(board.BoardNumber).Results)
                {
                    var exists = false;
                    foreach (var item in board.Results)
                    {
                        if (SeatsExtensions.AllSeats(s => item.Participants.Names[s] == result.Participants.Names[s])) exists = true;
                    }

                    if (!exists) board.Results.Add(result);
                }
            }
        }

        #endregion

        #region Public Properties

        public string EventName { get; set; }

        public DateTime Created { get; set; }

        public Scorings ScoringMethod { get; set; }

        public bool AllowReplay { get; set; }

        public Collection<Board2> Boards { get; set; }

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
        public List<Team> Participants { get; private set; }

        public string Trainer { get; set; }

        public string TrainerComment { get; set; }

        public string TrainerConventionCard { get; set; }

        public bool BidContest { get; set; }

        public bool AllowOvercalls { get; set; }

        public bool Unattended { get; set; }

        public MatchProgress MatchInProgress { get; set; }

        #endregion
    }

    public class MatchProgress
    {
        public TeamData Team1 { get; set; }

        public TeamData Team2 { get; set; }

        public int Tables { get; set; }
    }

    public class TeamData
    {
        public string Name { get; set; }

        public long ThinkTimeOpenRoom { get; set; }
        public long ThinkTimeClosedRoom { get; set; }
    }

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public class OnlineTournament
    {
        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public Collection<OnlineTournamentResult> Ranking { get; set; }
        [DataMember]
        public Scorings Scoring { get; set; }
    }

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public class OnlineTournamentResult
    {
        [DataMember]
        public int Rank { get; set; }
        [DataMember]
        public Guid UserId { get; set; }
        [DataMember]
        public string Participant { get; set; }
        [DataMember]
        public string Country { get; set; }
        //public int Total { get; set; }
        [DataMember]
        public double Average { get; set; }
        [DataMember]
        public int Boards { get; set; }
    }

    #pragma warning disable 1998

    public class RandomBoardsTournament : Tournament
    {
        public RandomBoardsTournament(string name) : base(name) { }

        public override async Task<Board2> GetNextBoardAsync(int boardNumber, Guid userId)
        {
            var c = new Board2(boardNumber);
            c.Distribution.DealRemainingCards(ShufflingRequirement.Random);
            return c;
        }

        public override Task<Board2> GetBoardAsync(int boardNumber)
        {
            throw new NotImplementedException();
        }

        public override async Task SaveAsync(BoardResult result)
        {
        }
    }
}
