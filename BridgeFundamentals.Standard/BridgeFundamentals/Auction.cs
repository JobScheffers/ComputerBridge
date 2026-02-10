namespace Bridge
{
    using System;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Auction maintains all bids that occur in a game and allows to query them.
    /// </summary>
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]
    public class Auction
    {
        private int passCount;
        private bool doubled;
        private bool redoubled;
        private Bid lastBid = Bid.GetPass();
        private Contract contract;
        private Vulnerable theVulnerability;
        private Seats firstSeatNotToPass;
        private bool allPassesTillNow;
        private Seats theDealer;
        private BoardResult parent;

        public Auction(Vulnerable v, Seats dealer)
            : this()
        {
            this.theVulnerability = v;
            this.theDealer = dealer;
        }

        public Auction(BoardResult p)
            : this()
        {
            this.parent = p;
        }

        internal Auction()
        {
            this.passCount = 4;
            this.allPassesTillNow = true;
            this.Bids = new Collection<AuctionBid>();
        }

        [DataMember]
        public Collection<AuctionBid> Bids { get; private set; }

        public bool Ended { get { return this.passCount == 0; } }

        public Contract FinalContract
        {
            get
            {
                if (!this.Ended && this.contract == null) throw new InvalidOperationException("The contract has not yet been determined");
                this.contract ??= new Contract(this.lastBid, this.Doubled, this.Redoubled, this.Declarer, this.Vulnerability);
                return this.contract;
            }
            internal set
            {
                this.contract = value;
            }
        }

        public Seats WhoseTurn
        {
            get
            {
                if (this.Ended)
                {
                    return this.Declarer.Next();
                }
                else
                {
                    var who = this.Dealer;
                    var shifts = this.Bids.Count % 4;
                    for (int i = 0; i < shifts; i++) who = who.Next();
                    return who;
                }
            }
        }

        public Seats Dealer
        {
            get { return this.parent == null || this.parent.Board == null ? this.theDealer : this.parent.Board.Dealer; }
            internal set { this.theDealer = value; }
        }

        public Seats Declarer
        {
            get
            {
                int i = this.Bids.Count - 1;    // pointing to last bid
                while (i >= 0 && !this.Bids[i].Bid.IsRegular) i--;  // pointing to winning bid
                if (i < 0) return this.Dealer;        // 4 passes
                int j = i % 2;        // point to first bid of partnership
                while (!(this.Bids[j].Bid.IsRegular && this.Bids[i].Bid.Suit == this.Bids[j].Bid.Suit)) j += 2;  // pointing first bid in contract suit
                return this.WhoBid0(j);
            }
        }

        public Seats FirstNotToPass { get { return this.firstSeatNotToPass; } }

        public Bid LastRegularBid
        {
            get { return this.lastBid; }
        }

        public bool Doubled
        {
            get { return this.doubled; }
        }

        public bool Redoubled
        {
            get { return this.redoubled; }
        }

        public Vulnerable Vulnerability
        {
            get { return this.parent == null || this.parent.Board == null ? this.theVulnerability : this.parent.Board.Vulnerable; }
            internal set { this.theVulnerability = value; }
        }

        public bool AllowDouble
        {
            get
            {
                if (this.doubled) return false;
                if (!this.lastBid.IsRegular) return false;
                var who = this.WhoBid0(this.Bids.Count - 1).Next();
                return this.Declarer != who && this.Declarer.Partner() != who;
            }
        }

        public bool AllowRedouble
        {
            get
            {
                if (!this.doubled) return false;
                if (this.redoubled) return false;
                var who = this.WhoBid0(this.Bids.Count - 1).Next();
                return this.Declarer.Partner() == who || this.Declarer == who;
            }
        }

        public byte AantalBiedingen { get { return (byte)this.Bids.Count; } }

        public Suits WasVierdeKleur(int skip)
        {
            SuitCollection<bool> genoemd = new(false);
            Suits result = Suits.NoTrump;
            int nr = 2 + skip;
            while (nr <= this.AantalBiedingen)
            {
                if (this.Terug(nr).Bid.IsRegular)
                {
                    genoemd[this.Terug(nr).Bid.Suit] = true;
                }
                nr += 2;
            }
            nr = 0;
            foreach (Suits cl in SuitHelper.StandardSuitsAscending)
            {
                if (!genoemd[cl])
                {
                    result = cl;
                    nr++;
                }
            }

            if (nr > 1) result = Suits.NoTrump;
            return result;
        }

        public Suits VierdeKleur
        {
            get
            {
                return this.WasVierdeKleur(0);
            }
        }

        public void Record(Seats seat, AuctionBid bid)
        {
            if (bid.Bid.IsDouble && !this.AllowDouble) throw new AuctionException("Double not allowed");
            if (bid.Bid.IsRedouble && !this.AllowRedouble) throw new AuctionException("Redouble not allowed");
            if (bid.Bid.IsRegular && this.lastBid >= bid.Bid) throw new AuctionException("Bid {0} is too low", bid);
            if (this.Ended) throw new AuctionException("Auction has already ended");
            if (seat != this.WhoseTurn) throw new AuctionException(string.Format("Expected a bid from {0} instead of {1}", this.WhoseTurn, seat));

            this.Bids.Add(bid);

            if (bid.Bid.IsPass)
            {
                this.passCount--;
            }
            else
            {
                this.passCount = 3;
                if (this.allPassesTillNow)
                {
                    this.allPassesTillNow = false;
                    this.firstSeatNotToPass = seat;
                }
            }

            if (bid.Bid.IsRegular)
            {
                this.doubled = false;
                this.redoubled = false;
                this.lastBid = bid.Bid;
            }
            else
            {
                if (bid.Bid.IsDouble)
                {
                    this.doubled = true;
                }
                else
                {
                    if (bid.Bid.IsRedouble) this.redoubled = true;
                }
            }
        }

        public void Record(AuctionBid bid)
        {
            this.Record(this.WhoseTurn, bid);
        }

        public bool Vergelijkbaar(string biedSerie)
        {
            return this.VergelijkbaarMUV(biedSerie, (byte)0);
        }

        public bool StartedWith(string biedSerie)
        {
            return this.VergelijkbaarMUV(biedSerie, -1);
        }

        public bool VergelijkbaarMUV(string biedSerie, int muv)
        {
            Suits w = Suits.NoTrump;
            Suits x = Suits.NoTrump;
            Suits y = Suits.NoTrump;
            Suits z = Suits.NoTrump;
            string specialKeywords = "WXYZMN";
            byte number = 0;
            byte bod = 1;

            ReadOnlySpan<char> span = biedSerie == null ? ReadOnlySpan<char>.Empty : biedSerie.AsSpan();
            int pos = 0;

            while (pos < span.Length && bod <= this.Bids.Count - muv)
            {
                NextKeyWord(span, ref pos, out ReadOnlySpan<char> token, out ReadOnlySpan<char> keyWordSpan, out number);

                 // case-insensitive token comparisons using ReadOnlySpan
                 if (MemoryExtensions.Equals(keyWordSpan, "**".AsSpan(), StringComparison.OrdinalIgnoreCase))
                 {
                     bod = (byte)(this.Bids.Count - muv + 1);
                 }
                 else if (MemoryExtensions.Equals(keyWordSpan, "??".AsSpan(), StringComparison.OrdinalIgnoreCase))
                 {
                     if (bod <= this.Bids.Count) bod++;
                     else return false;
                 }
                 else if (MemoryExtensions.Equals(keyWordSpan, "NP".AsSpan(), StringComparison.OrdinalIgnoreCase))
                 {
                     if (bod - 1 > this.Bids.Count || this.Bids[bod - 1].Bid.IsPass) return false;
                     bod++;
                 }
                 else if (MemoryExtensions.Equals(keyWordSpan, "PASS*".AsSpan(), StringComparison.OrdinalIgnoreCase) || MemoryExtensions.Equals(keyWordSpan, "PAS*".AsSpan(), StringComparison.OrdinalIgnoreCase))
                 {
                     while (bod <= this.Bids.Count && this.Bids[bod - 1].Bid.IsPass) bod++;
                     int passCount = bod - 1;

                     while (true)
                     {
                         int tempPos = pos;
                         if (tempPos >= span.Length) break;
                         NextKeyWord(span, ref tempPos, out ReadOnlySpan<char> nextTokenFull, out ReadOnlySpan<char> nextTokenKey, out byte nextNumber);
                         // nextTokenFull contains the original token including digits; check for literal "00"
                         if (nextTokenFull.Length == 2 && nextTokenFull[0] == '0' && nextTokenFull[1] == '0')
                         {
                             pos = tempPos;
                             passCount--;
                         }
                         else break;
                     }

                    if (passCount < 0) return false;
                }
                else if (MemoryExtensions.Equals(keyWordSpan, "PASS0".AsSpan(), StringComparison.OrdinalIgnoreCase) || MemoryExtensions.Equals(keyWordSpan, "PAS0".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    while ((bod + 1 <= this.Bids.Count)
                            && (this.Bids[bod - 1].Bid.IsPass)
                            && (this.Bids[bod].Bid.IsPass)
                            ) bod += 2;
                    if (bod <= this.Bids.Count && this.Bids[bod - 1].Bid.IsPass) return false;
                }
                else if (MemoryExtensions.Equals(keyWordSpan, "PASS1".AsSpan(), StringComparison.OrdinalIgnoreCase) || MemoryExtensions.Equals(keyWordSpan, "PAS1".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    if (bod <= this.Bids.Count && this.Bids[bod - 1].Bid.IsPass)
                    {
                        bod++;
                        while ((bod + 1 <= this.Bids.Count)
                                && (this.Bids[bod - 1].Bid.IsPass)
                                && (this.Bids[bod].Bid.IsPass)) bod += 2;
                        if (bod <= this.Bids.Count && this.Bids[bod - 1].Bid.IsPass) return false;
                    }
                    else return false;
                }
                else if (MemoryExtensions.Equals(keyWordSpan, "PASS2".AsSpan(), StringComparison.OrdinalIgnoreCase) || MemoryExtensions.Equals(keyWordSpan, "PAS2".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    if (bod + 1 <= this.Bids.Count && this.Bids[bod - 1 + 0].Bid.IsPass && this.Bids[bod - 1 + 1].Bid.IsPass)
                    {
                        bod += 2;
                        if (bod <= this.Bids.Count && this.Bids[bod - 1].Bid.IsPass) bod++;
                    }
                    else return false;
                }
                else if (MemoryExtensions.Equals(keyWordSpan, "PASS0OR1".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    if (bod <= this.Bids.Count && this.Bids[bod - 1].Bid.IsPass)
                    {
                        bod++;
                        if (bod <= this.Bids.Count && this.Bids[bod - 1].Bid.IsPass) return false;
                    }
                }
                else if (MemoryExtensions.Equals(keyWordSpan, "PASS012".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    if (bod <= this.Bids.Count && this.Bids[bod - 1].Bid.IsPass)
                    {
                        bod++;
                        if (bod <= this.Bids.Count && this.Bids[bod - 1].Bid.IsPass)
                        {
                            bod++;
                            if (bod <= this.Bids.Count && this.Bids[bod - 1].Bid.IsPass) return false;
                        }
                    }
                }
                else if ((keyWordSpan.Length == 0) && (number >= 0) && (bod <= this.Bids.Count))
                {
                    if (this.Bids[bod - 1].Bid.Index != number) return false;
                    bod++;
                }
                else if (keyWordSpan.Length > 0 && specialKeywords.IndexOf(char.ToUpperInvariant(keyWordSpan[0])) >= 0 && bod <= this.Bids.Count)
                {
                    if ((this.Bids[bod - 1].Bid.IsRegular)
                        && (this.Bids[bod - 1].Bid.Level == (BidLevels)number)
                        && (this.Bids[bod - 1].Bid.Suit != Suits.NoTrump))
                    {
                        char c = char.ToUpperInvariant(keyWordSpan[0]);
                        switch (c)
                        {
                            case 'W':
                                if (w == Suits.NoTrump)
                                {
                                    w = this.Bids[bod - 1].Bid.Suit;
                                    if (w == x || w == y || w == z) return false;
                                }
                                else if (w != this.Bids[bod - 1].Bid.Suit) return false;
                                break;

                            case 'X':
                                if (x == Suits.NoTrump)
                                {
                                    x = this.Bids[bod - 1].Bid.Suit;
                                    if (x == y || x == z || x == w) return false;
                                }
                                else if (x != this.Bids[bod - 1].Bid.Suit) return false;
                                break;

                            case 'Y':
                                if (y == Suits.NoTrump)
                                {
                                    y = this.Bids[bod - 1].Bid.Suit;
                                    if (y == x || y == z || y == w) return false;
                                }
                                else if (y != this.Bids[bod - 1].Bid.Suit) return false;
                                break;

                            case 'Z':
                                if (z == Suits.NoTrump)
                                {
                                    z = this.Bids[bod - 1].Bid.Suit;
                                    if (z == x || z == y || z == w) return false;
                                }
                                else if (z != this.Bids[bod - 1].Bid.Suit) return false;
                                break;

                            case 'M':
                                if (!(this.Bids[bod - 1].Bid.Suit == Suits.Hearts || this.Bids[bod - 1].Bid.Suit == Suits.Spades)) return false;
                                break;
                            case 'N':
                                if (!(this.Bids[bod - 1].Bid.Suit == Suits.Clubs || this.Bids[bod - 1].Bid.Suit == Suits.Diamonds)) return false;
                                break;
                        }
                        bod++;
                    }
                    else return false;
                }
                else if (bod <= this.Bids.Count)
                {
                    // produce diagnostic similar to previous behavior
                    throw new FatalBridgeException("Syntax error in Vergelijkbaar: " + new string(keyWordSpan));
                }
                else return false;
            }

            if (muv == -1) return true;
            if (bod <= this.Bids.Count - muv) return false;    // vergelijkingen moeten uitputtend zijn Met Uitzondering Van de laatste x biedingen
            if (pos < span.Length)
            {
                NextKeyWord(span, ref pos, out ReadOnlySpan<char> keyTokenFull2, out ReadOnlySpan<char> keyWordSpan2, out number);
                if ((pos < span.Length)
                  || (!(MemoryExtensions.Equals(keyWordSpan2, "PAS*".AsSpan(), StringComparison.OrdinalIgnoreCase) || MemoryExtensions.Equals(keyWordSpan2, "PAS0".AsSpan(), StringComparison.OrdinalIgnoreCase) || MemoryExtensions.Equals(keyWordSpan2, "**".AsSpan(), StringComparison.OrdinalIgnoreCase))))
                {
                    return false;  // vergelijkingen moeten uitputtend zijn
                }
            }
            return true;
        }

        [DebuggerStepThrough]
        public AuctionBid Terug(int bidMoment)
        {
#if DEBUG
            if (bidMoment > this.Bids.Count) throw new AuctionException("Before first bid");
            if (bidMoment < 1) throw new AuctionException("After last bid");
#endif
            return this.Bids[^bidMoment];
        }

        public int WanneerGeboden(Bid bidToFind)
        {
            int moment = this.Bids.Count;
            byte result = 0;
            while (moment > 0 && this.Bids[moment - 1].Bid != bidToFind)
            {
                moment--;
            }

            if (moment > 0) result = (byte)(this.Bids.Count + 1 - moment);
            return result;
        }

        public int WanneerGeboden(string bidToFind)
        {
            return WanneerGeboden(Bid.Parse(bidToFind));
        }

        public int WanneerGeboden(int level, Suits suit)
        {
            return this.WanneerGeboden(Bid.Get(level, suit));
        }

        public Seats WhoBid(int bidMoment)
        {
#if DEBUG
            if (bidMoment > this.Bids.Count) throw new AuctionException("Before first bid");
            if (bidMoment < 1) throw new AuctionException("After last bid");
#endif
            var who = this.Dealer;
            var shifts = (this.Bids.Count - bidMoment) % 4;
            for (int i = 0; i < shifts; i++) who = who.Next();
            return who;
        }

        public Seats WhoBid0(int bidMoment)
        {
#if DEBUG
            if (bidMoment >= this.Bids.Count) throw new AuctionException("After last bid");
            if (bidMoment < 0) throw new AuctionException("Before first bid");
#endif
            var who = this.Dealer;
            var shifts = (bidMoment) % 4;
            for (int i = 0; i < shifts; i++) who = who.Next();
            return who;
        }

        public bool HasBid(Suits suit, int bidMoment)
        {
            while (bidMoment <= this.Bids.Count)
            {
                var b = this.Bids[this.Bids.Count - bidMoment].Bid;
                if (b.IsRegular && b.Suit == suit) return true;
                bidMoment += 4;
            }

            return false;
        }

        public bool Opened
        {
            get
            {
                for (int b = 0; b < this.Bids.Count; b++)
                {
                    if (this.Bids[b].Bid.IsRegular)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public AuctionBid OpeningBid
        {
            get
            {
                for (int b = 0; b < this.Bids.Count; b++)
                {
                    if (this.Bids[b].Bid.IsRegular)
                    {
                        return this.Bids[b];
                    }
                }

                throw new InvalidOperationException("No opening bid yet");
            }
        }

        public Seats Opener
        {
            get
            {
                for (int b = 0; b < this.Bids.Count; b++)
                {
                    if (this.Bids[b].Bid.IsRegular)
                    {
                        return this.WhoBid0(b);
                    }
                }

                throw new InvalidOperationException("No opening bid yet");
            }
        }

        private static void NextKeyWord(ReadOnlySpan<char> s, ref int pos, out ReadOnlySpan<char> token, out ReadOnlySpan<char> keyWord, out byte number)
         {
             // skip whitespace
             while (pos < s.Length && char.IsWhiteSpace(s[pos])) pos++;

             int start = pos;
             while (pos < s.Length && !char.IsWhiteSpace(s[pos])) pos++;

            token = s.Slice(start, pos - start);

            number = 0;
            int idx = 0;
            while (idx < token.Length && char.IsDigit(token[idx]))
            {
                number = (byte)(number * 10 + (token[idx] - '0'));
                idx++;
            }

            keyWord = token.Slice(idx);
         }

        public override string ToString()
        {
            StringBuilder result = new();
            result.AppendLine("West  North East  South");
            Seats skip = this.Dealer;
            while (skip != Seats.West)
            {
                result.Append("-     ");
                skip = skip.Previous();
            }

            var who = this.Dealer;
            foreach (var item in this.Bids)
            {
                result.Append(item.ToString().PadRight(6));
                if (who == Seats.South)
                {
                    result.AppendLine();
                }

                who = who.Next();
            }

            return result.ToString();
        }

        public Seats FirstBid(Suits trump)
        {
            Seats result = this.WhoseTurn;
            int back = 2;
            while (back <= this.Bids.Count)
            {
                if (this.Terug(back).Bid.IsRegular && this.Terug(back).Bid.Suit == trump) result = (back % 4 == 0 ? this.WhoseTurn : this.WhoseTurn.Partner());
                back += 2;
            }

            return result;
        }

        public Seats FirstToBid(Suits trump, Directions partnership)
        {
            Seats result = this.WhoseTurn;
            int back = this.AantalBiedingen;
            while (back > 0)
            {
                if (this.Terug(back).Bid.IsRegular && this.WhoBid(back).Direction() == partnership && this.Terug(back).Bid.Suit == trump)
                {
                    return this.WhoBid(back);
                }

                back--;
            }

            return result;
        }

        internal void BoardChanged(Board2 p)
        {
            this.theDealer = p.Dealer;
            this.theVulnerability = p.Vulnerable;
            this.contract = null;
        }

        internal void BoardChanged(BoardResult p)
        {
            this.parent = p;
            this.contract?.Vulnerability = p.Board.Vulnerable;
        }

        public bool WordtVierdeKleur(Suits nieuweKleur)
        {
            SuitCollection<bool> genoemd = new(false);
            genoemd[nieuweKleur] = true;
            byte nr = 2;
            while (nr <= this.AantalBiedingen)
            {
                if (this.Terug(nr).Bid.IsRegular)
                {
                    Suits cl = this.Terug(nr).Bid.Suit;
                    if (cl != Suits.NoTrump) genoemd[cl] = true;
                }
                nr += 2;
            }
            bool result = true;
            foreach (Suits s in SuitHelper.StandardSuitsAscending)
                if (!genoemd[s]) result = false;
            return result;
        }
    }

    public class AuctionException(string format, params object[] args) : FatalBridgeException(format, args)
    {
    }
}
