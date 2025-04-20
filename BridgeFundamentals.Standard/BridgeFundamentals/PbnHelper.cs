using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Bridge
{
    // other bridge notation formats:
    // RBN, RBX: http://www.rpbridge.net/7a12.htm

    [Obsolete("Replace with PbnHelper")]
    public static class Pbn2Tournament
    {

    }

    public static class PbnHelper
    {
        /// <summary>
        /// Read a pbn file
        /// </summary>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        public static async Task<Tournament> Load(Stream fileStream)
        {
            using (var sr = new StreamReader(fileStream))
            {
                string content = await sr.ReadToEndAsync().ConfigureAwait(false);
                return PbnHelper.Load(content);
            }
        }

        public static async Task<Tournament> LoadFile(string fileName)
        {
            Stream responseStream;
            if (fileName.StartsWith("http://"))
            {
                var url = new Uri(fileName);
                var myClient = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true });
                var response = await myClient.GetAsync(url).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                responseStream = await response.Content.ReadAsStreamAsync();
            }
            else
            {
                responseStream = File.OpenRead(fileName);
            }

            return await Load(responseStream).ConfigureAwait(false);
        }

        public static void Save(Stream fileStream, Tournament tournament)
        {
            Save(tournament, fileStream);
        }

        #region Save

        public static void Save(Tournament t, Stream s)
        {
            var impsScoring = t.ScoringMethod == Scorings.scIMP || t.ScoringMethod == Scorings.scCross;
            using (var w = new StreamWriter(s))
            {
                w.WriteLine("% PBN 2.1");
                w.WriteLine("% EXPORT");
                w.WriteLine("% Creator: RoboBridge");
                if (t.MatchInProgress != null)
                {
                    // save details about the match in progress so that it can be continued some other time
                    w.WriteLine($"% RoboBridge Match {t.MatchInProgress.Team1.Name.Replace(" ", "|")} {t.MatchInProgress.Team2.Name.Replace(" ", "|")} {t.MatchInProgress.Tables} {t.MatchInProgress.Team1.ThinkTimeOpenRoom} {t.MatchInProgress.Team2.ThinkTimeOpenRoom} {t.MatchInProgress.Team1.ThinkTimeClosedRoom} {t.MatchInProgress.Team2.ThinkTimeClosedRoom}");
                }
                w.WriteLine("");

                foreach (var board in t.Boards)
                {
                    int resultCount = board.Results.Count;
                    if (resultCount == 0) resultCount = 1;
                    for (int result = 0; result < resultCount; result++)
                    {
                        if (result == 0 || (result < board.Results.Count && board.Results[result].Auction.Ended))
                        {
                            //w.WriteLine("");
                            w.WriteLine("[Event \"{0}\"]", t.EventName);
                            w.WriteLine("[Site \"\"]");
                            if (t.Created.Year > 1700) w.WriteLine("[EventDate \"{0}\"]", t.Created.ToString("yyyy.MM.dd"));
                            w.WriteLine("[Scoring \"{0}\"]", impsScoring ? "IMP" : "MP");
                            w.WriteLine("[Board \"{0}\"]", board.BoardNumber);
                            w.WriteLine("[Dealer \"{0}\"]", board.Dealer.ToXML());
                            w.WriteLine("[Vulnerable \"{0}\"]", board.Vulnerable.ToPbn());
                            w.WriteLine("[Deal \"{0}\"]", board.Distribution.ToPbn());
                            if (board.BestContract != null) w.WriteLine("{{PAR of the deal: {0}}}", board.BestContract);
                            if (board.DoubleDummyTricks != null)
                            {
                                /// string of 20 hexadecimal digits indicating how many tricks can be made.
                                /// Starting with North's makeable tricks in NT, S, H, D & C
                                /// Followed by East, South & West in the same order.
                                /// Example: a11b8a11b81911119111
                                /// a9a97aaa971111111111
                                /// a9a97aaa971111111000
                                var tricks = new StringBuilder("00000000000000000000");
                                SeatsExtensions.ForEachSeat((seat) =>
                                {
                                    SuitHelper.ForEachTrump((suit) =>
                                    {
                                        tricks[5 * (int)seat + 4 - (int)suit] = ToHex(board.DoubleDummyTricks[seat][suit]);
                                    });
                                });
                                w.WriteLine("[DoubleDummyTricks \"{0}\"]", tricks);
                            }
                            if (board.FeasibleTricks != null)
                            {
                                w.Write("{Feasability:");
                                SeatsExtensions.ForEachSeat(seat => SuitHelper.ForEachTrump(suit => w.Write(" " + board.FeasibleTricks[seat][suit])));
                                w.WriteLine(" }");
                            }
                            if (board.OptimumScoreNS.HasValue)
                            {
                                w.WriteLine("[OptimumScore \"{1} {0}\"]", Math.Abs(board.OptimumScoreNS.Value), board.OptimumScoreNS.Value >= 0 ? "NS" : "EW");
                            }
                        }

                        if (result < board.Results.Count && board.Results.Count > 0)
                        {
                            var boardResult = board.Results.OrderByDescending(r => impsScoring ? r.Room : "").ToList()[result];     // open room before closed room
                            if (impsScoring && boardResult.Room != null)
                            {
                                w.WriteLine($"[Room \"{boardResult.Room}\"]");
                                if (t.Participants[0].System?.Length + t.Participants[1].System?.Length > 0)
                                {
                                    var participant = string.Equals(boardResult.Room, "Open", StringComparison.InvariantCultureIgnoreCase) ? 0 : 1;
                                    w.WriteLine($"[BidSystemNS \"{t.Participants[participant].System}\"]");
                                    w.WriteLine($"[BidSystemEW \"{t.Participants[(participant + 1) % 2].System}\"]");
                                }
                            }
                            if (boardResult.Auction != null && boardResult.Auction.Ended)
                            {
                                w.WriteLine("[Date \"{0}\"]", boardResult.Created.ToString("yyyy.MM.dd"));
                                w.WriteLine("[Time \"{0}\"]", boardResult.Created.ToString("HH:mm:ss"));
                                for (Seats seat = Seats.North; seat <= Seats.West; seat++)
                                {
                                    if (boardResult.Participants.Names[seat]?.Length > 0)
                                    {
                                        w.WriteLine("[{0} \"{1}\"]", seat.ToXMLFull(), boardResult.Participants.Names[seat]);
                                    }
                                }

                                w.WriteLine("[Contract \"{0}\"]", boardResult.Contract.ToXML());
                                if (!t.BidContest)
                                {
                                    w.WriteLine("[Score \"{1} {0}\"]", boardResult.NorthSouthScore * (boardResult.Contract.Declarer.Direction() == Directions.NorthSouth ? 1 : -1), boardResult.Contract.Declarer.Direction() == Directions.NorthSouth ? "NS" : "EW");
                                    w.WriteLine("[{1} \"{2} {0}\"]", ForceDecimalDot(boardResult.TournamentScore * (boardResult.Contract.Declarer.Direction() == Directions.NorthSouth || !impsScoring ? 1 : -1), "F2"), (impsScoring ? "ScoreIMP" : "ScorePercentage"), boardResult.Contract.Declarer.Direction() == Directions.NorthSouth ? "NS" : "EW");
                                }
                                w.WriteLine("[Auction \"{0}\"]", board.Dealer.ToXML());
                                int bidCount = 0;
                                var alerts = new List<string>();
                                foreach (var bid in boardResult.Auction.Bids)
                                {
                                    bidCount++;
                                    if (bidCount > 1 && bidCount % 4 == 1)
                                    {
                                        w.WriteLine();
                                    }
                                    w.Write(bid.ToXML());
                                    if (bid.Alert || bid.Explanation.Length > 0)
                                    {
                                        alerts.Add((bid.Alert ? "* " : "") + bid.Explanation);
                                        w.Write($" ={alerts.Count}= ");
                                    }

                                    if (bidCount % 4 > 0)
                                    {
                                        w.Write(" ");
                                    }
                                }
                                w.WriteLine();  // end of auction
                                for (int i = 1; i <= alerts.Count; i++)
                                {
                                    w.WriteLine($"[Note \"{i}:{alerts[i - 1]}\"]");
                                }

                                if (boardResult.Contract.Bid.IsRegular && boardResult.Play != null && !t.BidContest)
                                {
                                    alerts.Clear();
                                    w.WriteLine("[Declarer \"{0}\"]", boardResult.Contract.Declarer.ToXML());
                                    w.WriteLine("[Result \"{0}\"]", boardResult.Contract.tricksForDeclarer);
                                    w.WriteLine("[Play \"{0}\"]", boardResult.Contract.Declarer.Next().ToXML());
                                    for (int trick = 1; trick <= boardResult.Play.CompletedTricks; trick++)
                                    {
                                        var who = boardResult.Contract.Declarer;
                                        for (int man = 1; man <= 4; man++)
                                        {
                                            who = who.Next();
                                            bool played = false;
                                            Card c = boardResult.Play.CardWhenPlayed(trick, who);
                                            if (!Card.IsNull(c))
                                            {
                                                played = true;
                                            }

                                            w.Write(played ? c.ToString() : "- ");
                                            if (played)
                                            {
                                                boardResult.Play.comments.TryGetValue((byte)(13 * (int)c.Suit + (int)c.Rank), out var comment);
                                                if (!string.IsNullOrWhiteSpace(comment) && comment.StartsWith("signal ", StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    alerts.Add(comment.Substring(7));
                                                    w.Write($" ={alerts.Count}= ");
                                                }
                                            }

                                            if (man == 4)
                                            {
                                                w.WriteLine();
                                            }
                                            else
                                            {
                                                w.Write(" ");
                                            }
                                        }
                                    }

                                    for (int i = 1; i <= alerts.Count; i++)
                                    {
                                        w.WriteLine($"[Note \"{i}:{alerts[i - 1]}\"]");
                                    }

                                }
                            }
                        }

                        w.WriteLine("");
                    }

                    if (board.Results.Count == 0) w.WriteLine("");
                }

                // matchsheet for computerbridge
                if (impsScoring && t.Participants.Count >= 2)
                {
                    w.WriteLine("{");
                    {
                        w.WriteLine($"           | table 1            | table 2            |");
                        w.WriteLine($"BOARD VULN | CONTR BY RES    NS | CONTR BY RES    NS | {t.Participants[0].Member1.PadRight(4).Substring(0, 4)} {t.Participants[1].Member1.PadRight(4).Substring(0, 4)}");
                        foreach (var board in t.Boards)
                        {
                            if (board.Results.Count >= 2)
                            {
                                var table1 = 0;
                                var table2 = 1;
                                if (board.Results[0].Room == "Closed")
                                {
                                    table1 = 1;
                                    table2 = 0;
                                }
                                w.WriteLine($"{board.BoardNumber,5} {board.Vulnerable.ToPbn(),4} | {board.Results[table1].Contract.ToXML(),5} {board.Results[table1].Contract.Declarer.ToXML(),2} {board.Results[table1].Contract.Overtricks,3} {board.Results[table1].NorthSouthScore,5} | {board.Results[table2].Contract.ToXML(),5} {board.Results[table2].Contract.Declarer.ToXML(),2} {board.Results[table2].Contract.Overtricks,3} {board.Results[table2].NorthSouthScore,5} | {(board.Results[table1].TournamentScore > 0 ? board.Results[table1].TournamentScore.ToString("F1") : "").PadLeft(4)} {(board.Results[table2].TournamentScore > 0 ? board.Results[table2].TournamentScore.ToString("F1") : "").PadLeft(4)}");
                            }
                        }
                    }
                    w.WriteLine($"           | {(t.Participants[0].Member1 + " - " + t.Participants[1].Member1).PadRight(39)} | {t.Participants[0].TournamentScore.ToString("F1").PadLeft(4)} {t.Participants[1].TournamentScore.ToString("F1").PadLeft(4)}");
                    w.WriteLine("}");
                }
            }

            string ForceDecimalDot(double value, string format)
            {
                return value.ToString(format).Replace(",", ".");
            }
        }

        #endregion

        #region Load

        public static Tournament Load(string content)
        {
            Tournament tournament = new PbnTournament();
            tournament.Trainer = "";
            Board2 currentBoard = null;
            Seats declarer = (Seats)(-1);
            int tricksForDeclarer = 0;
            int round = 0;
            var nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";

            Regex macro = new Regex("^(%|;).*$");
            Regex endOfBoard = new Regex("^(\\*.*|)$");
            Regex emptyLine = new Regex("^\\s*$");
            Regex commentOneLine = new Regex("{(?<comment>.*)}\\s*");
            Regex commentMultipleLines = new Regex("^{(?<comment>.*)$");
            Regex data = new Regex("^\\[(?<item>.*)\\s\"(?<value>.*)\"[ ]*\\]$");

            var lines = content.Split('\n');
            var lineCount = lines.Length;
            var lineNumber = 0;
            string line;
            string pbnCreator = "";      // what software produced this pbn?
            string clubBridgeWebs = "";  // which bridge club
            var boardTags = new Dictionary<string, bool>();

            // Read and display lines from the file until the end of 
            // the file is reached.
            while (lineNumber < lineCount)
            {
                line = lines[lineNumber++].Trim();
                if (line.Contains("][")) throw new FatalBridgeException("a gamestate line cannot contain multiple data items ([....][....])");
                bool moreToParse;
                do
                {
                    moreToParse = false;

                    Match ma = macro.Match(line);
                    if (ma.Success)
                    {
                        int p = line.ToUpper().IndexOf("ROBOBRIDGE");
                        if (p > 0 && p < 10)
                        {		// specific RoboBridge instructions
                            line = line.Substring(p + 10).Trim();
                            if (line.ToUpper().StartsWith("MATCH"))
                            {       // this is a TableManager match in progress
                                line = line.Substring(5).Trim();
                                var matchParts = line.Split(' ');
                                tournament.MatchInProgress = new MatchProgress();
                                tournament.MatchInProgress.Team1 = new TeamData { Name = matchParts[0].Replace("|", " "), ThinkTimeOpenRoom = long.Parse(matchParts.Length >= 5 ? matchParts[3] : "0"), ThinkTimeClosedRoom = long.Parse(matchParts.Length >= 7 ? matchParts[5] : "0") };
                                tournament.MatchInProgress.Team2 = new TeamData { Name = matchParts[1].Replace("|", " "), ThinkTimeOpenRoom = long.Parse(matchParts.Length >= 5 ? matchParts[4] : "0"), ThinkTimeClosedRoom = long.Parse(matchParts.Length >= 7 ? matchParts[6] : "0") };
                                tournament.MatchInProgress.Tables = int.Parse(matchParts[2]);
                                tournament.Participants.Add(new Team { Member1 = tournament.MatchInProgress.Team1.Name, Member2 = tournament.MatchInProgress.Team1.Name });
                                tournament.Participants.Add(new Team { Member1 = tournament.MatchInProgress.Team2.Name, Member2 = tournament.MatchInProgress.Team2.Name });

                            }
                            else if (line.ToUpper().StartsWith("TRAINING"))
                            {		// this is a RoboBridge training file
                                tournament.Trainer = "?";
                                tournament.TrainerConventionCard = "";
                                if (line.Length > 8)
                                {
                                    line = line.Substring(8).Trim();
                                    p = line.IndexOf(' ');
                                    tournament.Trainer = line.Substring(0, p);
                                    tournament.TrainerComment = line.Substring(p).Trim();
                                }
                                line = lines[lineNumber++].Trim();
                                moreToParse = true;
                                while (macro.Match(line).Success && tournament.Trainer.Length > 0 && line.Contains(tournament.Trainer))
                                {
                                    p = line.IndexOf(tournament.Trainer) + tournament.Trainer.Length;
                                    tournament.TrainerComment += " " + line.Substring(p).Trim();
                                    line = lines[lineNumber++].Trim();
                                }
                            }
                            else if (line.ToUpper().StartsWith("CC "))
                            {		// convention card to use
                                tournament.TrainerConventionCard = "Acol";
                                if (line.Length > 3)
                                {
                                    line = line.Substring(3).Trim();
                                    tournament.TrainerConventionCard = line;
                                }
                                //line = (lineNumber < lineCount ? lines[lineNumber++].Trim() : null);
                                //moreToParse = true;
                                //while (macro.Match(line).Success && line.Contains(tournament.Trainer))
                                //{
                                //    p = line.IndexOf(tournament.Trainer) + tournament.Trainer.Length;
                                //    tournament.TrainerComment += " " + line.Substring(p).Trim();
                                //    line = (lineNumber < lineCount ? lines[lineNumber++].Trim() : null);
                                //}
                            }
                            else if (line.ToUpper().StartsWith("BIDCONTEST"))
                            {		// convention card to use
                                tournament.BidContest = true;
                            }
                            else if (line.ToUpper().StartsWith("NOOVERCALLS"))
                            {		// convention card to use
                                tournament.AllowOvercalls = false;
                            }
                            else if (line.ToUpper().StartsWith("UNATTENDED"))
                            {		// convention card to use
                                tournament.Unattended = true;
                            }
                            else
                            {
                                throw new PbnException("Unknown RoboBridge instruction: {0}", line);
                            }
                        }
                        else if (line.ToUpper().Contains("CREATOR"))
                        {
                            pbnCreator = line.Substring(10).Trim();
                        }

                        continue;
                    }

                    ma = endOfBoard.Match(line);
                    if (ma.Success) continue;

                    ma = emptyLine.Match(line);
                    if (ma.Success) continue;

                    ma = commentOneLine.Match(line);
                    if (ma.Success)
                    {
                        var comment = ma.Captures[0].Value;
                        line = line.Replace(comment, "");
                        moreToParse = (line.Trim().Length > 0);
                        if (currentBoard != null)
                        {
                            // {PAR of the deal: 4H  = played by North: 620 points}
                            // {PAR of the deal: 4S by East}
                            // {Feasability: 8 7 10 10 8 5 6 3 3 3 8 7 10 10 8 5 6 3 3 3 }

                            if (comment.StartsWith("{PAR of the deal: "))
                            {
                                var t = comment.Substring(18, comment.Length - 19);     // 4H  = played by North: 620 points}
                                var contractEnd = t.IndexOf('=');
                                if (contractEnd < 0) contractEnd = t.IndexOf('-');
                                if (contractEnd < 0) contractEnd = t.IndexOf(' ');
                                var contract = t.Substring(0, contractEnd).Trim();
                                var declarerStart = t.IndexOf(" by ") + 4;
                                var declarerEnd = t.IndexOf(": ");
                                if (declarerEnd >= 2)
                                {
                                    var d = t.Substring(declarerStart, declarerEnd - declarerStart - 1);
                                    currentBoard.BestContract = contract + " by " + d;
                                }
                                else
                                {
                                    currentBoard.BestContract = t;
                                }
                            }
                            else if (comment.StartsWith("{Feasability: "))
                            {
                                var tricks = comment.Substring(14, comment.Length - 16).Split(' ');
                                currentBoard.FeasibleTricks = new SeatCollection<SuitCollection<int>>();
                                SeatsExtensions.ForEachSeat(seat =>
                                    {
                                        currentBoard.FeasibleTricks[seat] = new SuitCollection<int>();
                                        SuitHelper.ForEachTrump(suit => currentBoard.FeasibleTricks[seat][suit] = int.Parse(tricks[5 * (int)seat + (int)suit]));
                                    });
                            }
                        }

                        continue;
                    }

                    ma = commentMultipleLines.Match(line);
                    if (ma.Success)
                    {
                        do
                        {
                            line = (lineNumber < lineCount ? lines[lineNumber++].Trim() : null);
                        } while (!Regex.IsMatch(line, "}$"));
                        continue;
                    }

                    ma = data.Match(line);
                    if (ma.Success)
                    {
                        string itemName = ma.Groups["item"].Value.TrimEnd().ToLowerInvariant();
                        string itemValue = ma.Groups["value"].Value.TrimEnd();
                        if (itemValue.Length > 0 || itemName == "board")
                        {
                            switch (itemName)
                            {
                                case "event":
                                    if (tournament.EventName == null && itemValue != "#") //)   will not handle multiple events in one file
                                    {
                                        tournament.EventName = itemValue;
                                    }
                                    break;

                                case "site":
                                    if (pbnCreator == "Bridgewebs") clubBridgeWebs = itemValue;
                                    break;

                                case "eventdate":
                                    if (itemValue != "#")
                                    {
                                        string[] dateParts = itemValue.Split('.');
                                        if (dateParts.Length > 1)
                                        {		// dots were used as seperator
                                            if (dateParts.Length >= 3)
                                            {
                                                try
                                                {
                                                    tournament.Created = new DateTime(int.Parse(dateParts[0]), int.Parse(dateParts[1]), int.Parse(dateParts[2]));
                                                }
                                                catch (ArgumentOutOfRangeException)
                                                {
                                                }
                                                catch (FormatException)
                                                {
                                                }
                                            }
                                        }
                                        else
                                        {
                                            DateTime eventDate;
                                            DateTime.TryParse(itemValue, out eventDate);
                                            tournament.Created = eventDate;
                                        }
                                    }
                                    break;

                                case "date":
                                    if (itemValue != "#")
                                    {
                                        string[] dateParts = itemValue.Split('.');
                                        if (dateParts.Length > 1)
                                        {		// dots were used as seperator: 2024.09.02
                                            if (dateParts.Length >= 3)
                                            {
                                                try
                                                {
                                                    var date = new DateTime(int.Parse(dateParts[0]), int.Parse(dateParts[1]), int.Parse(dateParts[2]));
                                                    AddNextBoard(itemName, () => currentBoard.CurrentResult(true).Created.Year > 1900);
                                                    currentBoard.CurrentResult(true).Created = date;

                                                    if (DateTime.Now.Subtract(tournament.Created).TotalSeconds < 10) tournament.Created = date;
                                                }
                                                catch (ArgumentOutOfRangeException)
                                                {
                                                }
                                                catch (FormatException)
                                                {
                                                }
                                            }
                                        }
                                        else
                                        {
                                            DateTime eventDate;
                                            DateTime.TryParse(itemValue, out eventDate);
                                            tournament.Created = eventDate;
                                        }
                                    }
                                    break;

                                case "time":
                                    if (itemValue != "#")
                                    {
                                        string[] dateParts = itemValue.Split(':');
                                        if (dateParts.Length > 1)
                                        {		// colons were used as seperator: 12:34:56
                                            if (dateParts.Length >= 3)
                                            {
                                                AddNextBoard(itemName, () => currentBoard.CurrentResult(true).Created.TimeOfDay.TotalSeconds > 0);
                                                try
                                                {
                                                    currentBoard.CurrentResult(true).Created = currentBoard.CurrentResult(true).Created
                                                        .AddHours(int.Parse(dateParts[0]))
                                                        .AddMinutes(int.Parse(dateParts[1]))
                                                        .AddSeconds(int.Parse(dateParts[2]));
                                                }
                                                catch (ArgumentOutOfRangeException)
                                                {
                                                }
                                                catch (FormatException)
                                                {
                                                }
                                            }
                                        }
                                        else
                                        {
                                            DateTime eventDate;
                                            DateTime.TryParse(itemValue, out eventDate);
                                            tournament.Created = eventDate;
                                        }
                                    }
                                    break;

                                case "board":
                                    int boardNumber = 0;
                                    if (string.IsNullOrWhiteSpace(itemValue))
                                    {
                                        boardNumber = tournament.Boards.Count + 1;
                                    }
                                    else
                                    {
                                        try
                                        {
                                            boardNumber = int.Parse(itemValue);
                                        }
                                        catch (FormatException)
                                        {
                                            throw new PbnException("Illegal board number in [board]: {0}", itemValue);
                                        }
                                    }

                                    AddNextBoard(itemName, () => true);
                                    currentBoard.BoardNumber = 100 * round + boardNumber;
                                    //if (boardNumber == 704) System.Diagnostics.Debugger.Break();
                                    break;

                                case "north":
                                case "east":
                                case "south":
                                case "west":
                                    if (itemValue.Length > 0 && itemValue != "#")
                                    {
                                        var seat2 = SeatsExtensions.FromXML(itemName);
                                        AddNextBoard(itemName, () => currentBoard.CurrentResult(true).Participants.Names[seat2] != null);
                                        if (!(currentBoard.Results.Count == 0 && (itemValue == "#" || itemValue == "?")))
                                        {
                                            currentBoard.CurrentResult(true).Participants.Names[seat2] = itemValue;
                                        }
                                    }
                                    break;

                                case "dealer":
                                    try
                                    {
                                        var newDealer = SeatsExtensions.FromXML(itemValue);
                                        AddNextBoard(itemName, () => currentBoard.Dealer >= Seats.North && currentBoard.Dealer != newDealer);
                                        currentBoard.Dealer = newDealer;
                                    }
                                    catch (FatalBridgeException)
                                    {
                                        throw new PbnException("Board {0}: Unknown [dealer]: {1}", currentBoard.BoardNumber, itemValue);
                                    }
                                    break;

                                case "vulnerable":
                                    try
                                    {
                                        currentBoard.Vulnerable = VulnerableConverter.FromXML(itemValue);
                                    }
                                    catch (FatalBridgeException)
                                    {
                                        throw new PbnException("Board {0}: Unknown [vulnerable]: {1}", currentBoard.BoardNumber, itemValue);
                                    }
                                    break;

                                case "deal":
                                    AddNextBoard(itemName, () => !currentBoard.Distribution.Incomplete);
                                    if (currentBoard.Distribution.Incomplete)
                                    {
                                        currentBoard.Distribution.InitCardDealing();
                                        // [Deal "N:T9643.AT.JT954.K J2.863.AQ7.A9854 AQ75.Q9754.2.QT6 K8.KJ2.K863.J732"]
                                        string players = "NESW";
                                        string suit = "SHDC";
                                        switch (itemValue[0])
                                        {
                                            case 'N':
                                                players = "NESW";
                                                break;
                                            case 'E':
                                                players = "ESWN";
                                                break;
                                            case 'S':
                                                players = "SWNE";
                                                break;
                                            case 'W':
                                                players = "WNES";
                                                break;
                                            default: throw new PbnException("Board {0}: Unknown seat {1} in [deal]", currentBoard.BoardNumber, itemValue[0]);
                                        }

                                        string[] hands = itemValue.Substring(2).Split(' ');
                                        for (int p = 0; p < hands.Length; p++)
                                        {
                                            string[] suits = hands[p].Split('.');
                                            if (suits.Length != 4) throw new PbnException("Board {0}: error in [deal]: not 4 suits for {1}", currentBoard.BoardNumber, SeatsExtensions.FromXML(players[p]));
                                            for (int s = 0; s < 4; s++)
                                            {
                                                for (int c = 0; c < suits[s].Length; c++)
                                                {
                                                    try
                                                    {
                                                        currentBoard.Distribution.Give(SeatsExtensions.FromXML(players[p]), SuitHelper.FromXML(suit[s]), RankHelper.From(suits[s][c]));
                                                    }
                                                    catch (FatalBridgeException x)
                                                    {
                                                        throw new PbnException("Board {0}: error in [deal]: {1}", currentBoard.BoardNumber, x.Message);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;

                                case "declarer":
                                    if (itemValue != "?")
                                    {
                                        try
                                        {
                                            declarer = SeatsExtensions.FromXML(itemValue);
                                        }
                                        catch (FatalBridgeException)
                                        {
                                            throw new PbnException("Board {0}: Unknown [declarer]: {1}", currentBoard.BoardNumber, itemValue);
                                        }
                                    }
                                    break;
                                case "contract":
                                    if (itemValue != "?")
                                    {
                                        try
                                        {
                                            currentBoard.CurrentResult(true).Contract = new Contract(itemValue, declarer, currentBoard.Vulnerable);
                                        }
                                        catch (FatalBridgeException)
                                        {
                                            throw new PbnException("Board {0}: Illegal [contract]: {1}", currentBoard.BoardNumber, itemValue);
                                        }

                                        if (tricksForDeclarer >= 0) currentBoard.CurrentResult(true).Contract.tricksForDeclarer = tricksForDeclarer;
                                    }
                                    break;
                                case "result":
                                    if (itemValue.Length > 0)
                                    {
                                        if (itemValue != "?" && itemValue != "##")
                                        {
                                            try
                                            {
                                                tricksForDeclarer = int.Parse(itemValue);
                                            }
                                            catch (FormatException)
                                            {
                                                throw new PbnException("Illegal number in [result]: {0}", itemValue);
                                            }
                                            if (currentBoard.CurrentResult(true).Auction != null && currentBoard.CurrentResult(true).Auction.Ended)
                                            {
                                                currentBoard.CurrentResult(true).Contract.tricksForDeclarer = tricksForDeclarer;
                                                currentBoard.CurrentResult(true).Contract.tricksForDefense = 13 - tricksForDeclarer;
                                                currentBoard.CalcBoardScore();
                                            }
                                        }
                                    }
                                    break;
                                case "annotator":
                                    break;
                                case "round":
                                    if (itemValue != "#")
                                    {
                                        var oldRound = round;
                                        int.TryParse(itemValue, out round);
                                        currentBoard.BoardNumber = currentBoard.BoardNumber + 100 * (round - oldRound);
                                    }
                                    break;
                                case "score":
                                    // ignore score; it will be calcultaed from contract and result
                                    break;
                                case "scoreimp":
                                    if (itemValue.StartsWith("NS")) itemValue = itemValue.Substring(2);
                                    else if (itemValue.StartsWith("EW")) itemValue = itemValue.Substring(2);
                                    currentBoard.CurrentResult(true).TournamentScore = double.Parse(itemValue, nfi);
                                    break;
                                case "room":
                                    currentBoard.CurrentResult(true).Room = itemValue;
                                    break;
                                case "scoretable":
                                    /*
[ScoreTable "PairId_NS\2R;PairId_EW\2R;Contract\6L;Declarer\1R;Result\2R"]
 7  8 4H     N 11
 9 12 3NT    N 11
[ScoreTable "Rank\3R;Contract\5L;Declarer\1;Result\2R;Score_NS\5R;IMP_NS\6R;Multiplicity"]
  1 6C    W  9   150   9.91 1
  2 5CX   E 10   100   9.46 1
 14 6C    W 11    50   8.47 5
123 12345 1 12 12345 123456 
                                     */

                                    if (currentBoard.Results.Count == 1 && !currentBoard.Results[0].Auction.Ended) currentBoard.Results.Clear();
                                    var columnDefinitions = itemValue.Split(';');
                                    if (clubBridgeWebs == "The Bridge Academy" && columnDefinitions[2] == "Contract\\5L") columnDefinitions[2] = "Contract\\6L";
                                    int rank = 0;
                                    do
                                    {
                                        line = (lineNumber < lineCount ? lines[lineNumber++] : "");
                                        if (line.Length >= 1 && line[line.Length - 1] == '\r') line = line.Substring(0, line.Length - 1);
                                        if (line.Length >= 1 && line[0] != '[')
                                        {
                                            ScoreTableEntry entry;// = new ScoreTableEntry();
                                            entry.Rank = ++rank;
                                            entry.Players = "";
                                            entry.Multiplicity = 1;
                                            entry.ScoreNS = 0;
                                            entry.ImpNS = 0;
                                            entry.Result = 0;
                                            entry.Declarer = Seats.North;
                                            entry.Contract = "";
                                            int p = 0;
                                            for (int c = 0; c < columnDefinitions.Length; c++)
                                            {
                                                var x = columnDefinitions[c].Split('\\');
                                                int fieldLength = 10;
                                                if (x.Length > 1) fieldLength = x[1] == "1" ? 1 : int.Parse(x[1].Substring(0, x[1].Length - 1));
                                                else
                                                {
                                                    int nextTab = line.IndexOf("\t", p);
                                                    if (nextTab >= 0) fieldLength = nextTab - p;
                                                }
                                                var value = (p + fieldLength > line.Length ? line.Substring(p) : line.Substring(p, fieldLength));
                                                p += fieldLength + 1;
                                                switch (x[0].ToLower())
                                                {
                                                    case "pairid_ns":
                                                        entry.Players = value + " - ";
                                                        break;
                                                    case "pairid_ew":
                                                        entry.Players += value;
                                                        break;
                                                    case "contract":
                                                        entry.Contract = value.Trim();
                                                        break;
                                                    case "declarer":
                                                        if (value != "-") entry.Declarer = SeatsExtensions.FromXML(value);
                                                        break;
                                                    case "result":
                                                        if (value.Trim() != "-") entry.Result = int.Parse(value);
                                                        break;
                                                    case "score_ns":
                                                        entry.ScoreNS = int.Parse(value);
                                                        break;
                                                    case "imp_ns":
                                                        entry.ImpNS = double.Parse(value, new CultureInfo("en-US"));
                                                        break;
                                                    case "multiplicity":
                                                        entry.Multiplicity = int.Parse(value);
                                                        rank += entry.Multiplicity - 1;
                                                        break;
                                                }
                                            }

                                            currentBoard.ClearCurrentResult();
                                            currentBoard.CurrentResult(true).IsFrequencyTable = true;
                                            currentBoard.CurrentResult(true).NorthSouthScore = entry.ScoreNS;
                                            currentBoard.CurrentResult(true).Multiplicity = entry.Multiplicity;
                                            if (entry.Contract.Length > 0)
                                            {
                                                currentBoard.CurrentResult(true).Contract = new Contract(entry.Contract, entry.Declarer, currentBoard.Vulnerable);
                                                currentBoard.CurrentResult(true).Contract.tricksForDeclarer = entry.Result;
                                            }
                                            currentBoard.CurrentResult(true).TournamentScore = entry.ImpNS;
                                            currentBoard.CurrentResult(true).Participants.Names[Seats.North] = entry.Players;
                                        }
                                    } while (line.Length >= 1 && line[0] != '[');
                                    break;

                                case "auction":
                                    //if (!ignoreBoardResult)
                                    {
                                        var auctionStarter = Seats.North;
                                        try
                                        {
                                            auctionStarter = SeatsExtensions.FromXML(itemValue);
                                        }
                                        catch (FatalBridgeException)
                                        {
                                            throw new PbnException("Board {0}: Unknown dealer {1} in [auction]", currentBoard.BoardNumber, itemValue);
                                        }
                                        if (auctionStarter != currentBoard.Dealer) throw new PbnException("Board {0}: [Auction] not started by dealer", currentBoard.BoardNumber);
                                        var currentResult = currentBoard.CurrentResult(true);
                                        currentResult.Auction = new Auction(currentResult);
                                        string auction = "";
                                        line = (lineNumber < lineCount ? lines[lineNumber++].Trim() : null);
                                        while (line != null && !line.Substring(0, line.Length > 20 ? 20 : line.Length).Contains("[") && line.Trim() != "*")
                                        {
                                            auction += "\n" + line;
                                            line = (lineNumber < lineCount ? lines[lineNumber++].Trim() : null);
                                        }

                                        auction = auction.Trim();
                                        // 1H 1S { hg jhghg jg } pass double { jhgjhg jg jhgj gjg} pass pass pass { hghjg jh gj }
                                        string openingComment = "";
                                        auction = Regex.Replace(auction, "^{(?<comment>.+?)}", (match) =>
                                            {
                                                openingComment = SuitSymbols(match.Groups["comment"].Value.Trim().Replace("\\n", "\n"));
                                                return "";
                                            }, RegexOptions.Singleline).Trim();

                                        while (auction.Length > 0)
                                        {
                                            string bid = "";
                                            auction = Regex.Replace(auction, "^(?<bid>[a-z0-9=\\*\\!\\$\\-]+)($|{| |\n|\t)", (match) =>
                                                {
                                                    bid = match.Groups["bid"].Value;
                                                    return "";
                                                }, RegexOptions.IgnoreCase).Trim();

                                            // footnotes: 1H 1S =1= pass pass pass
                                            int note = 0;
                                            auction = Regex.Replace(auction, "^=[0-9]+=", (match) =>
                                            {
                                                note = int.Parse(match.Value.Substring(1, match.Length - 2));
                                                return "";
                                            }, RegexOptions.Singleline).Trim();

                                            string comment = "";
                                            auction = Regex.Replace(auction, "^{(?<comment>.+?)}", (match) =>
                                            {
                                                comment = SuitSymbols(match.Groups["comment"].Value.Trim().Replace("\\n", "\n"));
                                                return "";
                                            }, RegexOptions.Singleline).Trim();

                                            if (bid.Length == 0 && comment.Length == 0) throw new PbnException("Board {0}: No bid or comment found after\n{1}", currentBoard.BoardNumber, currentResult.Auction);
                                            if (openingComment.Length > 0)
                                            {
                                                comment = (openingComment + " " + comment).Trim();
                                                openingComment = "";
                                            }

                                            if (bid.Length > 0)
                                            {
                                                if (bid == "*")
                                                {
                                                    currentResult.Auction.Record(new Bid(0));
                                                    currentResult.Auction.Record(new Bid(0));
                                                    currentResult.Auction.Record(new Bid(0));
                                                }
                                                else
                                                {
                                                    if (bid.StartsWith("$"))
                                                    {
                                                    }
                                                    else
                                                    {
                                                        if (bid.StartsWith("="))
                                                        {
                                                            // er gaat nog een note komen (een alert uitleg) waarvan het nummer correspondeert met dit nummer
                                                        }
                                                        else
                                                        {
                                                            Bid b;
                                                            try
                                                            {
                                                                b = new Bid(bid);
                                                                if (note > 0)
                                                                    b.Explanation = note.ToString();
                                                            }
                                                            catch (IndexOutOfRangeException)
                                                            {
                                                                throw new PbnException("Board {0}: unrecognized bid in [auction]: {1}", currentBoard.BoardNumber, bid);
                                                            }
                                                            catch (FatalBridgeException)
                                                            {
                                                                throw new PbnException("Board {0}: unrecognized bid in [auction]: {1}", currentBoard.BoardNumber, bid);
                                                            }

                                                            if (comment.Length > 0)
                                                            {
                                                                //if (comment.ToLower().StartsWith("alerted"))
                                                                //{
                                                                //    b.NeedsAlert();
                                                                //    comment = comment.Substring(7).Trim();
                                                                //}
                                                                b.HumanExplanation = comment;
                                                            }

                                                            try
                                                            {
                                                                currentResult.Auction.Record(b);
                                                            }
                                                            catch (AuctionException x)
                                                            {
                                                                throw new PbnException("Board {0}: Error in [auction]: {1}", currentBoard.BoardNumber, x.Message);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        if (currentResult.Auction.Ended)
                                        {
                                            //if (currentResult.Contract != null && currentResult.Contract.Text != currentResult.Auction.FinalContract.Text)
                                            //{
                                            //  throw new PbnException("Board {2}: [contract] ({0}) does not match [auction] ({1})", currentResult.Contract.Text, currentResult.Auction.FinalContract.Text, currentBoard.BoardNumber);
                                            //}

                                            currentResult.Contract = currentResult.Auction.FinalContract;
                                            if (declarer == (Seats)(-1))
                                            {
                                                declarer = currentResult.Auction.Declarer;
                                            }
                                            else if (declarer != currentResult.Auction.Declarer)
                                            {
                                                throw new PbnException("Board {2}: [declarer] ({0}) does not match [auction] ({1})", declarer, currentResult.Auction.Declarer, currentBoard.BoardNumber);
                                            }
                                        }
                                        moreToParse = true;		// make sure the current line (which has been read ahead) is parsed
                                    }
                                    break;

                                case "note":    // explanation of alert within auction
                                    var endOfNoteId = itemValue.IndexOf(':');
                                    var noteId = itemValue.Substring(0, endOfNoteId);
                                    itemValue = itemValue.Substring(1 + endOfNoteId).Trim();
                                    if (currentBoard.CurrentResult(true).Play == null)
                                    {   // notes for auction
                                        var isAlert = false;
                                        while (itemValue.StartsWith("* "))
                                        {
                                            isAlert = true;
                                            itemValue = itemValue.Substring(2).Trim();
                                        }
                                        foreach (var bid in currentBoard.CurrentResult(true).Auction.Bids)
                                        {
                                            if (bid.Explanation == noteId)
                                            {
                                                bid.Explanation = itemValue;
                                                if (isAlert) bid.NeedsAlert();
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {   // notes for play
                                        var comments = currentBoard.CurrentResult(true).Play.comments;
                                        var x = comments.Where(c => c.Value == noteId).First();
                                        comments[x.Key] = itemValue;
                                    }
                                    break;

                                case "play":
                                    if (itemValue != "?")
                                    {
                                        var currentResult = currentBoard.CurrentResult(true);
                                        if (itemValue != "-" && currentResult.Contract != null)
                                        {
                                            if (SeatsExtensions.FromXML(itemValue) != currentResult.Contract.Declarer.Next())
                                                throw new PbnException("Board {2}: Lead ({0}) does not match with the declarer ({1})", itemValue, currentResult.Contract.Declarer, currentBoard.BoardNumber);
                                        }

                                        string play = "";
                                        line = (lineNumber < lineCount ? lines[lineNumber++].Trim() : null);
                                        while (line != null && !line.Contains("[") && !line.StartsWith("%"))
                                        {
                                            play = (play + "\n" + line).Trim();
                                            line = (lineNumber < lineCount ? lines[lineNumber++].Trim() : null);
                                        }

                                        if (!play.StartsWith("*"))
                                        {
                                            currentResult.Play = new PlaySequence(currentResult.Auction.FinalContract, 13);

                                            Seats nextToPlay = (itemValue == "-" ? Seats.North : SeatsExtensions.FromXML(itemValue));
                                            Seats starter = nextToPlay;
                                            SeatCollection<string> trick = new SeatCollection<string>();
                                            while (play.Length > 0 && !play.StartsWith("*"))
                                            {
                                                string card = "";
                                                play = Regex.Replace(play, "^(?<card>([a-z0-9][a-z0-9])|(-))($|{| |\\*|\n|\t)", (match) =>
                                                {
                                                    card = match.Groups["card"].Value;
                                                    return "";
                                                }, RegexOptions.IgnoreCase).Trim();


                                                // footnotes: hK =1= h2 h3 h4
                                                play = Regex.Replace(play, "^=[0-9]+=", (match) =>
                                                {
                                                    card += ":" + match.Value.Substring(1, match.Length - 2);
                                                    return "";
                                                }, RegexOptions.Singleline).Trim();

                                                string comment = "";
                                                play = Regex.Replace(play, "^{(?<comment>.+?)}", (match) =>
                                                {
                                                    comment = SuitSymbols(match.Groups["comment"].Value.Trim().Replace("\\n", "\n"));
                                                    return "";
                                                }, RegexOptions.Singleline).Trim();

                                                if (card.Length == 0 && comment.Length == 0)
                                                    throw new PbnException("Board {0}: No card or comment found after\n{1}", currentBoard.BoardNumber, currentResult.Play);
                                                if (card.Length == 0 && comment.Length > 0)
                                                {
                                                    currentBoard.AfterAuctionComment = comment;
                                                }
                                                else
                                                {
                                                    if (itemValue == "-")
                                                    {
                                                        PlayCard(card + comment, currentResult.Play.whoseTurn, currentResult);
                                                    }
                                                    else
                                                    {
                                                        trick[nextToPlay] = card + comment;
                                                        nextToPlay = nextToPlay.Next();
                                                        if (nextToPlay == starter)
                                                        {		// have read 4 cards; now add them to the board
                                                            var s = currentResult.Play.whoseTurn;
                                                            for (int i = 0; i < 4; i++)
                                                            {
                                                                PlayCard(trick[s], s, currentResult);
                                                                trick[s] = "";
                                                                s = s.Next();
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            var s2 = currentResult.Play.whoseTurn;
                                            for (int i = 0; i < 4; i++)
                                            {
                                                PlayCard(trick[s2], s2, currentResult);
                                                trick[s2] = "";
                                                s2 = s2.Next();
                                            }
                                        }

                                        // closing comment?
                                        if (play.Length > 3)
                                        {
                                            string x = @"\{(?<comment>.+?)\}";
                                            currentBoard.ClosingComment = SuitSymbols(Regex.Match(play, x, RegexOptions.Singleline).Groups["comment"].Value.Trim().Replace("\\n", "\n"));
                                        }

                                        if (line != null) moreToParse = true;
                                        if (tricksForDeclarer >= 0 && currentResult.Auction.Ended)
                                        {
                                            currentResult.Contract.tricksForDeclarer = tricksForDeclarer;
                                            currentResult.Contract.tricksForDefense = 13 - tricksForDeclarer;
                                            currentBoard.CalcBoardScore();
                                        }
                                    }
                                    break;

                                //[OptimumScore "NS -100"]
                                case "optimumscore":
                                    if (itemValue.Length >= 5)
                                    {
                                        var optimumScoreParts = itemValue.Split(' ');
                                        if (optimumScoreParts.Length == 2)
                                        {
                                            int.TryParse(optimumScoreParts[1].Trim(), out var score);
                                            currentBoard.OptimumScoreNS = score * (optimumScoreParts[0].ToUpper() == "NS" ? 1 : -1);
                                        }
                                    }
                                    break;

                                //[OptimumResultTable "Declarer;Denomination\2R;Result\2R"]
                                //N  C  3
                                //N  D  7
                                //N  H  8
                                //N  S  3
                                //N NT  6
                                //E  C  8
                                //E  D  6
                                //E  H  4
                                //E  S  9
                                //E NT  7
                                //S  C  3
                                //S  D  7
                                //S  H  9
                                //S  S  3
                                //S NT  6
                                //W  C  8
                                //W  D  6
                                //W  H  4
                                //W  S  9
                                //W NT  7
                                case "optimumresulttable":
                                    for (int i = 0; i < 20; i++)
                                    {
                                        line = lines[lineNumber++].Trim();
                                    }
                                    break;

                                case "bidsystemns":
                                    if (itemValue.Length > 0 && itemValue != "#" && string.IsNullOrEmpty(tournament.Participants[0].System))
                                    {
                                        tournament.Participants[0].System = itemValue;
                                    }
                                    break;
                                case "bidsystemew":
                                    if (itemValue.Length > 0 && itemValue != "#" && string.IsNullOrEmpty(tournament.Participants[1].System))
                                    {
                                        tournament.Participants[1].System = itemValue;
                                    }
                                    break;

                                case "doubledummytricks":
                                    /// string of 20 hexadecimal digits indicating how many tricks can be made.
                                    /// Starting with North's makeable tricks in NT, S, H, D & C
                                    /// Followed by East, South & West in the same order.
                                    /// Example: a11b8a11b81911119111

                                    var tricks = HexToByteArray(itemValue, 1);
                                    currentBoard.DoubleDummyTricks = new SeatCollection<SuitCollection<byte>>();
                                    SeatsExtensions.ForEachSeat((seat) =>
                                    {
                                        currentBoard.DoubleDummyTricks[seat] = new SuitCollection<byte>(0);
                                        SuitHelper.ForEachTrump((suit) =>
                                        {
                                            currentBoard.DoubleDummyTricks[seat][suit] = tricks[5 * (int)seat + 4 - (int)suit];
                                        });
                                    });
                                    break;

                                default:
                                    //throw new InvalidDataException(string.Format("Unrecognized item: '{0}'", itemName));
                                    // ignore new/unknown tags
                                    break;
                            }
                        }

                        continue;
                    }

                    throw new PbnException("Unrecognized line {1}: '{0}'", line, lineNumber);
                } while (moreToParse && line != null);
            }

            SaveCurrentBoard();
            foreach (var board in tournament.Boards)
            {
                board.CalcBoardScore();
            }

            tournament.CalcTournamentScores();
            return tournament;

            void AddNextBoard(string tagName, Func<bool> mustAdd)
            {
                if (currentBoard == null || (boardTags.ContainsKey(tagName) && boardTags[tagName] && mustAdd()))
                {
                    //if (currentBoard != null && currentBoard.BoardNumber == 710) System.Diagnostics.Debugger.Break();

                    SaveCurrentBoard();

                    currentBoard = new Board2();
                    currentBoard.BoardNumber = -1;
                    currentBoard.ClearCurrentResult();
                    if (!string.IsNullOrEmpty(tournament.Trainer)) currentBoard.ClosingComment = ".";
                    tricksForDeclarer = -1;
                    declarer = (Seats)(-1);
                    currentBoard.Dealer = (Seats)(-1);
                    boardTags.Clear();
                }

                boardTags[tagName] = true;
            }

            void SaveCurrentBoard()
            {
                if (currentBoard != null)
                {   // save this board

                    // first remove incomplete results
                    // gamestate often has incomplete auction
                    while (currentBoard.Results.Count >= 1 && currentBoard.Results[0].Auction.AantalBiedingen == 0 && (int)currentBoard.Results[0].Auction.Dealer == -1 && !currentBoard.Results[0].IsFrequencyTable)
                    {
                        currentBoard.Results.RemoveAt(0);
                    }

                    if (currentBoard.BoardNumber == -1) currentBoard.BoardNumber = tournament.Boards.Count + 1;
                    var existingBoard = tournament.FindBoard(currentBoard.BoardNumber);
                    if (existingBoard == null)
                    {
                        tournament.Boards.Add(currentBoard);
                    }
                    else if (currentBoard.Results.Count >= 1)
                    {
                        existingBoard.Results.Add(currentBoard.Results[0]);
                    }
                }
            }
        }

        private static void PlayCard(string card, Seats who, BoardResult currentResult)
        {
            if (card != null && card.Length >= 2)
            {
                var suit = SuitHelper.FromXML(card[0]);
                var rank = RankHelper.From(card[1]);
                var cardCount = currentResult.Board.Distribution.Length(who);
                if (cardCount < 13 || currentResult.Board.Distribution.Owns(who, suit, rank))
                {
                    if (cardCount == 13 && currentResult.Play.leadSuit != Suits.NoTrump && suit != currentResult.Play.leadSuit)
                    {
                        var remaining = currentResult.Board.Distribution.Length(who, currentResult.Play.leadSuit) - currentResult.Play.Length(who, currentResult.Play.leadSuit);
                        if (remaining >= 1) throw new PbnException("Board {0}: error in [play] trick {3}: {2} must confess to lead suit instead of {1}", currentResult.Board.BoardNumber, card, who, currentResult.Play.currentTrick);
                    }

                    try
                    {
                        string signal = (card.Length > 3 && card[2] == ':') ? card.Substring(3) : card.Substring(2);
                        currentResult.Play.Record(suit, rank, signal);
                    }
                    catch (FatalBridgeException x)
                    {
                        throw new PbnException("Board {0}: error in [play]: {1} {2}", currentResult.Board.BoardNumber, card, x.Message);
                    }
                }
                else
                {
                    throw new PbnException("Board {0}: error in [play]: {1} does not have {2}", currentResult.Board.BoardNumber, who, card.Substring(0, 2));
                }
            }
        }

        private static string SuitSymbols(string comment)
        {
            foreach (var prefix in "\\_")
            {
                for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
                {
                    comment = comment.Replace(prefix + SuitHelper.ToXML(suit).Substring(0, 1), SuitHelper.ToUnicode(suit).ToString());
                }
            }

            return comment;
        }

        #endregion


        //[ScoreTable "PairId_NS\2R;PairId_EW\2R;Contract\6L;Declarer\1R;Result\2R"]
        //[ScoreTable "Rank\3R;Contract\5L;Declarer\1;Result\2R;Score_NS\5R;IMP_NS\6R;Multiplicity"]
        private struct ScoreTableEntry
        {
            public int Rank;
            public string Contract;
            public Seats Declarer;
            public int Result;
            public int Multiplicity;
            public string Players;
            public int ScoreNS;
            public double ImpNS;
        }

        private static byte[] HexToByteArray(string hex, int digitLength)
        {
            if (hex.Length % digitLength > 0)
                throw new ArgumentException("The binary key cannot have an odd number of digits");
            int digitCount = hex.Length / digitLength;
            byte[] arr = new byte[digitCount];

            for (int i = 0; i < digitCount; ++i)
            {
                arr[i] = 0;
                int start = i * digitLength;
                for (int digit = 0; digit < digitLength; digit++)
                {
                    arr[i] = (byte)((arr[i] << 4) + GetHexVal(hex[start + digit]));
                }
            }

            return arr;
        }

        private static int GetHexVal(char hex)
        {
            int val = (int)hex;
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        private static Char ToHex(byte value)
        {
            if (value > 9) return (Char)(87 + value);
            return (Char)(48 + value);
        }
    }

    public class PbnException : NoReportException
    {
        public PbnException(string format, params object[] args)
            : base(string.Format(format, args))
        {
        }
    }

    #pragma warning disable 1998

    public class PbnTournament : Tournament
    {
        public override async Task<Board2> GetNextBoardAsync(int relativeBoardNumber, Guid userId)
        {
            if (relativeBoardNumber < 1) throw new ArgumentOutOfRangeException(nameof(relativeBoardNumber), relativeBoardNumber + " (should be 1 or more)");
            if (relativeBoardNumber <= this.Boards.Count)
            {
                var board = this.Boards[relativeBoardNumber - 1];
                board.ClearCurrentResult();
                return board;
            }

            if (userId == Guid.Empty)
            { // parsing the pbn
              // add an empty board
                Board2 newBoard = new Board2();
                newBoard.BoardNumber = relativeBoardNumber;
                this.Boards.Add(newBoard);
                return newBoard;
            }
            else
            {
                return null;
            }
        }

        public override async Task<Board2> GetBoardAsync(int boardNumber)
        {
            foreach (var board in this.Boards)
            {
                if (board.BoardNumber == boardNumber)
                {
                    board.ClearCurrentResult();
                    return board;
                }
            }

            throw new ArgumentException($"board {boardNumber} not found");
        }

        public override async Task SaveAsync(BoardResult result)
        {
            foreach (var board in this.Boards)
            {
                if (board.BoardNumber == result.Board.BoardNumber)
                {
                    board.Results.Add(result);
                    return;
                }
            }
        }
    }
}
