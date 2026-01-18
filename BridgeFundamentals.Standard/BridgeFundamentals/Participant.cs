using System;

namespace Bridge
{
    public class Participant
    {
        private SeatCollection<string> theNames;
        private int lastBoardCompleted;
        private DateTime theLastPlay;
        private int scoreCount;
        private double sumOfScores;
        private double totalTournamentScore;

        public Participant()
        {
            this.Names = new SeatCollection<string>();
        }

        public Participant(string north, string east, string south, string west)
        {
            this.Names = new SeatCollection<string>([north, east, south, west]);
        }

        public Participant(SeatCollection<string> allNames)
        {
            this.Names = allNames;
        }

        public SeatCollection<string> Names
        {
            get
            {
                return theNames;
            }
            set
            {
                theNames = value;
            }
        }

        public int LastBoard
        {
            get
            {
                return lastBoardCompleted;
            }
            set
            {
                lastBoardCompleted = value;
            }
        }

        public DateTime LastPlay
        {
            get
            {
                return theLastPlay;
            }
            set
            {
                theLastPlay = value;
            }
        }

        public double TournamentScore
        {
            get
            {
                return totalTournamentScore;
            }
            set
            {
                totalTournamentScore = value;
            }
        }

        public override string ToString()
        {
            return this.theNames[Seats.North] + "/" + this.theNames[Seats.South]
                + " - " + this.theNames[Seats.West] + "/" + this.theNames[Seats.East];
        }

        public bool IsSame(SeatCollection<string> other)
        {
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                if (this.theNames[s] != other[s])
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsSame(string[] other)
        {
            return this.IsSame(new SeatCollection<string>(other));
        }

        public void InitRecalc()
        {
            this.scoreCount = 0;
            this.sumOfScores = 0;
        }

        public void AddScore(double boardScore)
        {
            this.scoreCount++;
            this.sumOfScores += boardScore;
        }

        public void CalcScore()
        {
            this.totalTournamentScore = this.sumOfScores / this.scoreCount;
        }
    }

    public class Team
    {
        private int scoreCount;
        private double sumOfScores;

        public Team()
        {
        }

        public Team(string member1, string member2)
        {
            this.Member1 = member1;
            this.Member2 = member2;
        }

        public string Member1 { get; set; }
        public string Member2 { get; set; }
        public string System { get; set; }

        public int LastBoard { get; set; }

        public DateTime LastPlay { get; set; }

        public double TournamentScore { get; set; }

        public override string ToString()
        {
            return this.Member1 + "/" + this.Member2;
        }

        public bool IsSame(string otherMember1, string otherMember2)
        {
            this.Member1 ??= "";
            this.Member2 ??= "";
            if (Member1.Equals(otherMember1?.ToLower(), StringComparison.CurrentCultureIgnoreCase) && Member2.Equals(otherMember2?.ToLower(), StringComparison.CurrentCultureIgnoreCase)) return true;
            if (Member1.Equals(otherMember2?.ToLower(), StringComparison.CurrentCultureIgnoreCase) && Member2.Equals(otherMember1?.ToLower(), StringComparison.CurrentCultureIgnoreCase)) return true;
            return false;
        }

        public bool IsSame(Team other)
        {
            return this.IsSame(other.Member1, other.Member2);
        }

        public void InitRecalc()
        {
            this.scoreCount = 0;
            this.sumOfScores = 0;
            this.TournamentScore = 0;
        }

        public void AddScore(double boardScore)
        {
            this.scoreCount++;
            this.sumOfScores += boardScore;
        }

        public void CalcScore()
        {
            this.TournamentScore = this.sumOfScores / this.scoreCount;
        }
    }
}
