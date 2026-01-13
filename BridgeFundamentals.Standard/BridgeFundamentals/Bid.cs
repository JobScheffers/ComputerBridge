namespace Bridge
{
    using System;
    using System.Collections.Generic;        // List
    using System.Diagnostics;
    using System.Runtime.Serialization;

    /// <summary>Levels of a bid; ranging from pass to 7</summary>
    public enum BidLevels
    {
        /// <summary>?</summary>
        Pass,

        /// <summary>?</summary>
        Level1,

        /// <summary>?</summary>
        Level2,

        /// <summary>?</summary>
        Level3,

        /// <summary>?</summary>
        Level4,

        /// <summary>?</summary>
        Level5,

        /// <summary>?</summary>
        Level6,

        /// <summary>?</summary>
        Level7
    }

    /// <summary>Type of a bid</summary>
    public enum SpecialBids
    {
        /// <summary>?</summary>
        NormalBid,

        /// <summary>?</summary>
        Pass,

        /// <summary>?</summary>
        Double,

        /// <summary>?</summary>
        Redouble
    }

    /// <summary>Levels of a bid; ranging from pass to 7</summary>
    public enum BodOperatoren
    {
        /// <summary>?</summary>
        Gelijk,

        /// <summary>?</summary>
        Ongelijk,

        /// <summary>?</summary>
        Groter,

        /// <summary>?</summary>
        Kleiner,

        /// <summary>?</summary>
        GroterGelijk,

        /// <summary>?</summary>
        KleinerGelijk
    }

    /// <summary>Bid</summary>
    //[DebuggerStepThrough]
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public class Bid
    {
        private Suits suit;
        private BidLevels level;
        private SpecialBids special;
        private string explanation;
        private bool alert;
        private string humanExplanation;

        [IgnoreDataMember]
        public bool Hint { get; set; }

        #region constructors
        /// <summary>Constructor</summary>
        /// <param name="s">Suit of the bid</param>
        /// <param name="l">Level of the bid</param>
        [DebuggerStepThrough]
        public Bid(Suits s, BidLevels l)
        {
            //if (l > BidLevels.Level7) throw new ArgumentOutOfRangeException("l", l.ToString());
            //if (s > Suits.NoTrump) throw new ArgumentOutOfRangeException("s", s.ToString());
            this.suit = s;
            this.level = l;
            this.special = SpecialBids.NormalBid;
            this.explanation = "";
            this.alert = false;
            this.Hint = false;
        }

        /// <summary>Constructor</summary>
        /// <param name="l">Level of the bid</param>
        /// <param name="s">Suit of the bid</param>
        [DebuggerStepThrough]
        public Bid(int l, Suits s) : this(s, (BidLevels)l) { }

        /// <summary>Constructor</summary>
        /// <param name="specialBid">Special bid</param>
        [DebuggerStepThrough]
        public Bid(SpecialBids specialBid)
        {
            this.special = specialBid;
            this.level = BidLevels.Pass;
            this.explanation = "";
        }

        /// <summary>Constructor</summary>
        /// <param name="index">Index of the bid</param>
        [DebuggerStepThrough]
        public Bid(int index) : this(index, "", false, "") { }

        /// <summary>Constructor</summary>
        /// <param name="index">Index of the bid</param>
        /// <param name="newExplanation">Explanation of the bid</param>
        [DebuggerStepThrough]
        public Bid(int index, string newExplanation, bool alert, string newHumanExplanation)
        {
            switch (index)
            {
                case 0:
                    this.level = BidLevels.Pass;
                    this.special = SpecialBids.Pass; break;
                case 36:
                    this.level = BidLevels.Pass;
                    this.special = SpecialBids.Double; break;
                case 37:
                    this.level = BidLevels.Pass;
                    this.special = SpecialBids.Redouble; break;
                default:
                    {
                        //if (index > 35) throw new FatalBridgeException("invalid index: {0}", index);
                        this.special = SpecialBids.NormalBid;
                        this.suit = (Suits)((index - 1) % 5);
                        this.level = (BidLevels)(((index - 1) / 5) + 1);
                        break;
                    }
            }

            //if (this.level > BidLevels.Level7) throw new InvalidCastException("level " + this.level.ToString());
            //if (this.suit > Suits.NoTrump) throw new InvalidCastException("suit " + this.suit.ToString());
            this.explanation = newExplanation;
            this.alert = alert;
            this.humanExplanation = newHumanExplanation;
        }

        /// <summary>Constructor</summary>
        /// <param name="fromXML">XML describing the bid</param>
        [DebuggerStepThrough]
        public Bid(string fromXML)
        {
            //if (fromXML == null) throw new ArgumentNullException("fromXML");
            this.explanation = "";
            if (fromXML.Contains(';'))
            {
                string[] parts = fromXML.Split(';');
                if (parts.Length >= 2) this.explanation = parts[1];
                if (parts.Length >= 3) this.humanExplanation = parts[2];
                fromXML = parts[0];
            }

            int pAlert = fromXML.IndexOf('!');
            int pInfo = fromXML.IndexOf('?');
            if (pInfo >= 0 && (pInfo < pAlert || pAlert < 0))
            {
                pAlert = pInfo;
                this.UnAlert();
            }
            else
            {
                if (pAlert >= 0)
                {
                    this.NeedsAlert();
                }
            }

            if (pAlert >= 0)
            {
                this.explanation = fromXML[(pAlert + 1)..];
                fromXML = fromXML[..pAlert];
            }

            switch (fromXML.ToLowerInvariant())
            {
                case "p":
                case "pass":
                case "passes":
                    this.level = BidLevels.Pass;
                    this.special = SpecialBids.Pass;
                    break;
                case "x":
                case "dbl":
                case "double":
                case "doubles":
                case "36":
                    this.level = BidLevels.Pass;
                    this.special = SpecialBids.Double;
                    break;
                case "xx":
                case "rdbl":
                case "redouble":
                case "redoubles":
                case "37":
                    this.level = BidLevels.Pass;
                    this.special = SpecialBids.Redouble;
                    break;
                default:
                    this.special = SpecialBids.NormalBid;
                    this.level = (BidLevels)(Convert.ToByte(fromXML[0]) - 48);
                    this.suit = SuitHelper.FromXML(fromXML[1..]);
                    break;
            }
        }

        /// <summary>
        /// Static constructor for use in comparing a bid to a constant
        /// </summary>
        /// <param name="fromXML">string representation of the bid (like 5S)</param>
        /// <returns>A bid</returns>
        public static Bid C(string fromXML)
        {
            return new Bid(fromXML);
        }

        /// <summary>
        /// Constructor of a bid that is based on a xml representation of the bid
        /// </summary>
        /// <param name="fromXML">The XML that contains the bid</param>
        /// <param name="explanation">An explanation of the bid that has been provided by the bidder</param>
        public Bid(string fromXML, string explanation)
            : this(fromXML)
        {
            this.explanation = explanation;
        }

        /// <summary>
        /// Just for serializer
        /// </summary>
        public Bid()
        {
        }

        #endregion

        #region properties

        /// <summary>Suit</summary>
        /// <value>?</value>
        public Suits Suit
        {
            get
            {
#if DEBUG
                if (!this.IsRegular) throw new InvalidOperationException("Suit of pass, double or redouble");
#endif
                return this.suit;
            }
        }

        /// <summary>Level</summary>
        /// <value>?</value>
        public BidLevels Level { get { return this.level; } }

        /// <summary>What kind of bid?</summary>
        /// <value>?</value>
        public SpecialBids Special { get { return this.special; } }

        /// <summary>Robot readable explanation of the bid</summary>
        /// <value></value>
        [IgnoreDataMember]
        public string Explanation
        {
            get { return this.explanation; }
            set { this.explanation = value; }
        }

        /// <summary>Human readable explanation of the bid</summary>
        /// <value></value>
        [IgnoreDataMember]
        public string HumanExplanation
        {
            get { return this.humanExplanation; }
            set { this.humanExplanation = value; }
        }

        /// <summary>Is the bid regular (can it become a contract)?</summary>
        /// <value>?</value>
        public bool IsRegular { get { return (this.special == SpecialBids.NormalBid); } }

        /// <summary>Is the bid a pass?</summary>
        /// <value>?</value>
        public bool IsPass { get { return (this.special == SpecialBids.Pass); } }

        /// <summary>Is the bid a double?</summary>
        /// <value>?</value>
        public bool IsDouble { get { return (this.special == SpecialBids.Double); } }

        /// <summary>Is the bid a redouble?</summary>
        /// <value>?</value>
        public bool IsRedouble { get { return (this.special == SpecialBids.Redouble); } }

        /// <summary>Constructor</summary>
        /// <value>?</value>
        public byte Hoogte { get { return (byte)this.level; } }

        /// <summary>Integer representation of a bid</summary>
        /// <value>int: 0=pass, 36=double, 37=redouble</value>
        [DataMember]
        public int Index
        {
            get
            {
                return this.Special switch
                {
                    SpecialBids.Pass => 0,
                    SpecialBids.Double => 36,
                    SpecialBids.Redouble => 37,
                    SpecialBids.NormalBid => ToIndex(this.level, this.suit),
                    _ => 0,
                };
            }

            set
            {
                switch (value)
                {
                    case 0: this.SetPass(); break;
                    case 36: this.SetDoublet(); break;
                    case 37: this.SetRedoublet(); break;
                    default:
                        {
                            this.suit = ToSuit(value);
                            this.level = ToLevel(value);
                            this.special = SpecialBids.NormalBid;
                        }
                        break;
                }
            }
        }

        public static BidLevels ToLevel(int bidIndex)
        {
            return (BidLevels)(1 + (bidIndex - 1) / 5);
        }

        public static Suits ToSuit(int bidIndex)
        {
            return (Suits)((bidIndex - 1) % 5);
        }

        public bool Alert { get { return this.alert; } }

        #endregion

        /// <summary>
        /// Static constructor for use in comparing a bid to a constant
        /// </summary>
        /// <param name="fromXML">string representation of the bid (like 5S)</param>
        /// <returns>The index of a bid</returns>
        public static int ToIndex(string fromXML)
        {
            if (fromXML.Contains(';'))
            {
                string[] parts = fromXML.Split(';');
                fromXML = parts[0];
            }

            if (fromXML.Contains('!'))
            {
                int p = fromXML.IndexOf('!');
                fromXML = fromXML[..p];
            }

            return fromXML.ToLowerInvariant() switch
            {
                "p" or "pass" or "passes" => 0,
                "x" or "dbl" or "double" or "doubles" or "36" => 36,
                "xx" or "rdbl" or "redouble" or "redoubles" or "37" => 37,
                _ => ToIndex(Convert.ToByte(fromXML[0]) - 48, SuitHelper.FromXML(fromXML[1..])),
            };
        }

        /// <summary>
        /// Static constructor for use in comparing a bid to a constant
        /// </summary>
        /// <param name="fromXML">string representation of the bid (like 5S)</param>
        /// <returns>The index of a bid</returns>
        public static int ToIndex(int level, Suits suit)
        {
            return 5 * (level - 1) + (int)suit + 1;
        }

        /// <summary>
        /// Static constructor for use in comparing a bid to a constant
        /// </summary>
        /// <param name="fromXML">string representation of the bid (like 5S)</param>
        /// <returns>The index of a bid</returns>
        public static int ToIndex(BidLevels level, Suits suit)
        {
            return ToIndex((int)level, suit);
        }

        /// <summary>'Larger Than' operator</summary>
        /// <param name="b1">First bid to compare</param>
        /// <param name="b2">Second bid to compare</param>
        /// <returns>boolean</returns>
        public static bool operator >(Bid b1, Bid b2)
        {
            ArgumentNullException.ThrowIfNull(b1);
            ArgumentNullException.ThrowIfNull(b2);
            return b1.Index > b2.Index;
        }
        public static bool operator >(Bid b1, string b2)
        {
            ArgumentNullException.ThrowIfNull(b1);
            ArgumentNullException.ThrowIfNull(b2);
            return b1.Index > ToIndex(b2);
        }
        public static bool operator >(Bid b1, int b2)
        {
            ArgumentNullException.ThrowIfNull(b1);
            return b1.Index > b2;
        }

        /// <summary>Operator</summary>
        /// <param name="b1">First bid to compare</param>
        /// <param name="b2">Second bid to compare</param>
        /// <returns>boolean</returns>
        public static bool operator <(Bid b1, Bid b2)
        {
            ArgumentNullException.ThrowIfNull(b1);
            ArgumentNullException.ThrowIfNull(b2);
            return b1.Index < b2.Index;
        }
        public static bool operator <(Bid b1, string b2)
        {
            ArgumentNullException.ThrowIfNull(b1);
            ArgumentNullException.ThrowIfNull(b2);
            return b1.Index < ToIndex(b2);
        }
        public static bool operator <(Bid b1, int b2)
        {
            ArgumentNullException.ThrowIfNull(b1);
            return b1.Index < b2;
        }

        /// <summary>Operator</summary>
        /// <param name="b1">First bid to compare</param>
        /// <param name="b2">Second bid to compare</param>
        /// <returns>boolean</returns>
        public static bool operator >=(Bid b1, Bid b2)
        {
            ArgumentNullException.ThrowIfNull(b1);
            ArgumentNullException.ThrowIfNull(b2);
            return b1.Index >= b2.Index;
        }
        public static bool operator >=(Bid b1, string b2)
        {
            ArgumentNullException.ThrowIfNull(b1);
            ArgumentNullException.ThrowIfNull(b2);
            return b1.Index >= ToIndex(b2);
        }
        public static bool operator >=(Bid b1, int b2)
        {
            ArgumentNullException.ThrowIfNull(b1);
            return b1.Index >= b2;
        }

        /// <summary>Operator</summary>
        /// <param name="b1">First bid to compare</param>
        /// <param name="b2">Second bid to compare</param>
        /// <returns>boolean</returns>
        public static bool operator <=(Bid b1, Bid b2)
        {
            ArgumentNullException.ThrowIfNull(b1);
            ArgumentNullException.ThrowIfNull(b2);
            return b1.Index <= b2.Index;
        }
        public static bool operator <=(Bid b1, string b2)
        {
            ArgumentNullException.ThrowIfNull(b1);
            ArgumentNullException.ThrowIfNull(b2);
            return b1.Index <= ToIndex(b2);
        }
        public static bool operator <=(Bid b1, int b2)
        {
            ArgumentNullException.ThrowIfNull(b1);
            return b1.Index <= b2;
        }

        /// <summary>Operator</summary>
        /// <param name="b1">First bid to compare</param>
        /// <param name="b2">Second bid to compare</param>
        /// <returns>boolean</returns>
        public static bool operator ==(Bid b1, Bid b2)
        {
            return (Object.Equals(b1, null) && Object.Equals(b2, null))
              || (!Object.Equals(b2, null) && !Object.Equals(b1, null) && b1.Index == b2.Index);
        }

        /// <summary>Operator</summary>
        /// <param name="b1">First bid to compare</param>
        /// <param name="b2">Second bid to compare</param>
        /// <returns>boolean</returns>
        public static bool operator !=(Bid b1, Bid b2)
        {
            return !(b1 == b2);
        }

        /// <summary>Convert the bid to a string</summary>
        /// <returns>String</returns>
        public static string ToString(int bidIndex)
        {
            return bidIndex switch
            {
                0 => "Pass",
                36 => "x",
                37 => "xx",
                _ => ((int)ToLevel(bidIndex)).ToString() + SuitHelper.ToXML(ToSuit(bidIndex)),
            };
        }

        /// <summary>Convert the bid to a string</summary>
        /// <returns>String</returns>
        public override string ToString()
        {
            return this.special switch
            {
                SpecialBids.Pass => "Pass",
                SpecialBids.Double => "x",
                SpecialBids.Redouble => "xx",
                SpecialBids.NormalBid => ((int)this.level).ToString() + SuitHelper.ToXML(this.suit),
                _ => "?",
            };
        }

        /// <summary>Convert the bid to a digit and a special suit character</summary>
        /// <returns>String</returns>
        public string ToSymbol()
        {
            string result = this.special switch
            {
                SpecialBids.Pass => LocalizationResources.Pass,
                SpecialBids.Double => "x",
                SpecialBids.Redouble => "xx",
                SpecialBids.NormalBid => ((int)this.level).ToString() + (this.suit == Suits.NoTrump
                            ? LocalizationResources.NoTrump
                            : "" + SuitHelper.ToUnicode(this.suit)),
                _ => "?",
            };
            if (this.alert)
            {
                result += "!";
            }

            return result;
        }

        /// <summary>Convert the bid to a XML string</summary>
        /// <returns>String</returns>
        public string ToXML()
        {
            string s;
            switch (this.special)
            {
                case SpecialBids.Pass:
                    s = "Pass";
                    break;
                case SpecialBids.Double:
                    s = "X";
                    break;
                case SpecialBids.Redouble:
                    s = "XX";
                    break;
                case SpecialBids.NormalBid:
                    s = ((int)this.level).ToString() + SuitHelper.ToXML(this.suit);
                    break;
                default:
                    return "?";
            }

            //if (this.alert) s += "!";
            return s;
        }

        /// <summary>Convert the bid to a localized string</summary>
        /// <returns>String</returns>
        public string ToText()
        {
            return this.special switch
            {
                SpecialBids.Pass => LocalizationResources.Pass,
                SpecialBids.Double => "x",
                SpecialBids.Redouble => "xx",
                SpecialBids.NormalBid => string.Concat(((int)this.level).ToString(), SuitHelper.ToLocalizedString(this.suit).AsSpan(0, (this.suit == Suits.NoTrump ? 2 : 1))),
                _ => "?",
            };
        }

        /// <summary>Is bid equal to provided object?</summary>
        /// <param name="obj">Object to compare to</param>
        /// <returns>boolean</returns>
        public override bool Equals(Object obj)
        {
            if (obj == null) return false;
            Bid b = obj as Bid;
            return this == b;
        }

        /// <summary>Is bid equal to provided object?</summary>
        /// <param name="obj">Object to compare to</param>
        /// <returns>boolean</returns>
        public bool Equals(Suits s, BidLevels height)
        {
            return this.level == height && this.suit == s;
        }

        /// <summary>Is bid equal to provided object?</summary>
        /// <param name="obj">Object to compare to</param>
        /// <returns>boolean</returns>
        public bool Equals(int height, Suits s)
        {
            return this.Hoogte == height && this.suit == s;
        }

        /// <summary>Returns a hashcode (always 0)</summary>
        /// <returns>0</returns>
        public override int GetHashCode()
        {
            return 0;    //re.GetHashCode()  im.GetHashCode();
        }

        /// <summary>Raise the level of the current bid by 1</summary>
        public void EenNiveauHoger()
        {
            if (this.level >= BidLevels.Level7) throw new FatalBridgeException("8-level not allowed");
            this.level++;
        }

        /// <summary>Lower the level of the current bid by 1</summary>
        public void EenNiveauLager()
        {
            if (this.level <= BidLevels.Level1) throw new FatalBridgeException("0-level not allowed");
            this.level--;
        }

        /// <summary>Constructor</summary>
        public void SetPass() { this.special = SpecialBids.Pass; this.level = BidLevels.Pass; this.alert = false; }

        /// <summary>Constructor</summary>
        public void SetDoublet() { this.special = SpecialBids.Double; this.level = BidLevels.Pass; this.alert = false; }

        /// <summary>Constructor</summary>
        public void SetRedoublet() { this.special = SpecialBids.Redouble; this.level = BidLevels.Pass; this.alert = false; }

        /// <summary>Alter the bid</summary>
        /// <param name="l">Level of the bid</param>
        /// <param name="s">Suit of the bid</param>
        public void Set(int l, Suits s)
        {
            this.Set(l, s, false);
        }

        /// <summary>Alter the bid</summary>
        /// <param name="l">Level of the bid</param>
        /// <param name="s">Suit of the bid</param>
        /// <param name="alert">Alert the bid</param>
        public void Set(int l, Suits s, bool alert)
        {
            //if (l > 7 || l < 0) throw new FatalBridgeException("level must be 1..7: {0}", l);
            this.special = SpecialBids.NormalBid;
            this.suit = s;
            this.level = (BidLevels)l;
            this.alert = alert;
        }

        /// <summary>Difference between two bids</summary>
        /// <param name="anderBod">The other bid</param>
        /// <remarks>The second bid must be lower to get a positive number.
        /// 1S.Verschil(1H) == 1
        /// 1H.Verschil(1S) == -1
        /// </remarks>
        /// <returns>byte</returns>
        public int Verschil(Bid anderBod)
        {
            //return (byte)Math.Abs(this.Index - anderBod.Index);
            //19-05-06 minimum_bod.Verschil(4,H) <= 5 met minimum_bod=5S leverde true op
            //TODO: waar gaat het nu mis? waar reken ik op die abs()?
            ArgumentNullException.ThrowIfNull(anderBod);
            return this.Index - anderBod.Index;
        }

        /// <summary>Difference between two bids</summary>
        /// <param name="h">Height</param>
        /// <param name="s">Suit</param>
        /// <returns>byte</returns>
        public int Verschil(int h, Suits s)
        {
            return this.Index - ToIndex(h, s);
        }

        /// <summary>Copy a bid</summary>
        /// <returns>The copied bid</returns>
        public Bid Clone()
        {
            return new Bid(this.Index, this.explanation, this.alert, this.humanExplanation);
        }

        /// <summary>Alter a bid</summary>
        /// <param name="b">The bid to change to</param>
        /// <param name="increase">The increment to add to the first parameter</param>
        public void Assign(Bid b, int increase)
        {
            ArgumentNullException.ThrowIfNull(b);
            this.Index = b.Index + increase;
            this.alert = false;
        }

        /// <summary>Some bid comparison</summary>
        /// <param name="anderBod">?</param>
        /// <param name="verhoging">?</param>
        /// <returns>?</returns>
        public bool IsOngeveer(Bid anderBod, int verhoging)
        {
            ArgumentNullException.ThrowIfNull(anderBod);
            return (this.Index == anderBod.Index + verhoging);
        }

        public void NeedsAlert()
        {
            this.alert = true;
        }

        public void UnAlert()
        {
            this.alert = false;
        }
    }

    /// <summary>Collection of bids. Sample usage: NietMeerBieden</summary>
    public class BiedingenSet
    {
        private readonly Dictionary<int, string> bids = [];

        /// <summary>Check if the set contains the call</summary>
        /// <param name="call">The bid that will be searched in the set</param>
        /// <returns>Boolean indicating whether the call was found in the set</returns>
        public bool Peek(Bid call, string caller)
        {
            ArgumentNullException.ThrowIfNull(call);
            var bidIndex = call.Index;
            if (bids.TryGetValue(bidIndex, out string value))
            {
                return (value == caller);
            }
            else
            {
                return false;
            }
        }

        /// <summary>Check if the set contains the call</summary>
        /// <param name="call">The bid that will be searched in the set</param>
        /// <returns>Boolean indicating whether the call was found in the set</returns>
        public bool Bevat(int bidIndex, string caller)
        {
            if (!bids.TryAdd(bidIndex, caller))
            {
                if (Exception(caller, bids[bidIndex])) return false;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>Check if the set contains the call</summary>
        /// <param name="call">The bid that will be searched in the set</param>
        /// <returns>Boolean indicating whether the call was found in the set</returns>
        public bool Bevat(Bid call, string caller)
        {
            ArgumentNullException.ThrowIfNull(call);
            return Bevat(call.Index, caller);
        }

        protected virtual bool Exception(string caller, string existing)
        {
            return false;
        }

        /// <summary>Check if the bid sequence contains a bid</summary>
        /// <param name="h">The height of the bid that will be searched in the bid sequence</param>
        /// <param name="s">The suit of the bid that will be searched in the bid sequence</param>
        /// <returns>Boolean indicating whether the bid was found in the bid sequence</returns>
        public bool BevatHK(int h, Suits s, string caller)
        {
            return this.Bevat(Bid.ToIndex(h, s), caller);
        }

        /// <summary>Check if the bid sequence contains any bid in this suit</summary>
        /// <param name="k">The suit that will be searched in the bid sequence</param>
        /// <returns>Boolean indicating whether the suit was found in the bid sequence</returns>
        public bool BevatKleur(Suits k, string caller)
        {
            for (int hoogteVanBod = 1; hoogteVanBod <= 7; hoogteVanBod++)
            {
                if (!(hoogteVanBod == 4 && k == Suits.NoTrump)
                        && this.BevatHK(hoogteVanBod, k, caller)
                    )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Check if the bid sequence contains an increment of a bid</summary>
        /// <param name="bod">The bid that will be incremented</param>
        /// <param name="verhoging">The increment that will be added to the bid</param>
        /// <returns>Boolean indicating whether the increment was found in the bid sequence</returns>
        public bool BevatVolgende(Bid bod, byte verhoging, string caller)
        {
            ArgumentNullException.ThrowIfNull(bod);
            return this.Bevat(bod.Index + verhoging, caller);
        }

        /// <summary>Check if a double has been tried before</summary>
        /// <returns>?</returns>
        public bool BevatDoublet(string caller)
        {
            return this.Bevat(36, caller);
        }

        /// <summary>?</summary>
        /// <returns>?</returns>
        public bool BevatRedoublet(string caller)
        {
            return this.Bevat(37, caller);
        }

        public override string ToString()
        {
            string result = "";
            foreach (var bid in bids)
            {
                result += $"[{Bid.ToString(bid.Key)}, {bid.Value}] ";
            }

            return result;
        }
    }
}
