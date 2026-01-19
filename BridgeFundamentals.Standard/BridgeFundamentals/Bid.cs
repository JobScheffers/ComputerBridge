namespace Bridge
{
    using System;
    using System.Collections.Generic;        // List
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.CompilerServices;
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
    //[DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    //    public class Bid
    //    {
    //        private Suits suit;
    //        private BidLevels level;
    //        private SpecialBids special;
    //        private string explanation;
    //        private bool alert;
    //        private string humanExplanation;

    //        [IgnoreDataMember]
    //        public bool Hint { get; set; }

    //        #region constructors
    //        /// <summary>Constructor</summary>
    //        /// <param name="s">Suit of the bid</param>
    //        /// <param name="l">Level of the bid</param>
    //        [DebuggerStepThrough]
    //        public Bid(Suits s, BidLevels l)
    //        {
    //            //if (l > BidLevels.Level7) throw new ArgumentOutOfRangeException("l", l.ToString());
    //            //if (s > Suits.NoTrump) throw new ArgumentOutOfRangeException("s", s.ToString());
    //            this.suit = s;
    //            this.level = l;
    //            this.special = SpecialBids.NormalBid;
    //            this.explanation = "";
    //            this.alert = false;
    //            this.Hint = false;
    //        }

    //        /// <summary>Constructor</summary>
    //        /// <param name="l">Level of the bid</param>
    //        /// <param name="s">Suit of the bid</param>
    //        [DebuggerStepThrough]
    //        public Bid(int l, Suits s) : this(s, (BidLevels)l) { }

    //        /// <summary>Constructor</summary>
    //        /// <param name="specialBid">Special bid</param>
    //        [DebuggerStepThrough]
    //        public Bid(SpecialBids specialBid)
    //        {
    //            this.special = specialBid;
    //            this.level = BidLevels.Pass;
    //            this.explanation = "";
    //        }

    //        /// <summary>Constructor</summary>
    //        /// <param name="index">Index of the bid</param>
    //        [DebuggerStepThrough]
    //        public Bid(int index) : this(index, "", false, "") { }

    //        /// <summary>Constructor</summary>
    //        /// <param name="index">Index of the bid</param>
    //        /// <param name="newExplanation">Explanation of the bid</param>
    //        [DebuggerStepThrough]
    //        public Bid(int index, string newExplanation, bool alert, string newHumanExplanation)
    //        {
    //            switch (index)
    //            {
    //                case 0:
    //                    this.level = BidLevels.Pass;
    //                    this.special = SpecialBids.Pass; break;
    //                case 36:
    //                    this.level = BidLevels.Pass;
    //                    this.special = SpecialBids.Double; break;
    //                case 37:
    //                    this.level = BidLevels.Pass;
    //                    this.special = SpecialBids.Redouble; break;
    //                default:
    //                    {
    //                        //if (index > 35) throw new FatalBridgeException("invalid index: {0}", index);
    //                        this.special = SpecialBids.NormalBid;
    //                        this.suit = (Suits)((index - 1) % 5);
    //                        this.level = (BidLevels)(((index - 1) / 5) + 1);
    //                        break;
    //                    }
    //            }

    //            //if (this.level > BidLevels.Level7) throw new InvalidCastException("level " + this.level.ToString());
    //            //if (this.suit > Suits.NoTrump) throw new InvalidCastException("suit " + this.suit.ToString());
    //            this.explanation = newExplanation;
    //            this.alert = alert;
    //            this.humanExplanation = newHumanExplanation;
    //        }

    //        /// <summary>Constructor</summary>
    //        /// <param name="fromXML">XML describing the bid</param>
    //        [DebuggerStepThrough]
    //        public Bid(string fromXML)
    //        {
    //            //if (fromXML == null) throw new ArgumentNullException("fromXML");
    //            this.explanation = "";
    //            if (fromXML.Contains(";"))
    //            {
    //#pragma warning disable HAA0101 // Array allocation for params parameter
    //                string[] parts = fromXML.Split(';');
    //#pragma warning restore HAA0101 // Array allocation for params parameter
    //                if (parts.Length >= 2) this.explanation = parts[1];
    //                if (parts.Length >= 3) this.humanExplanation = parts[2];
    //                fromXML = parts[0];
    //            }

    //            int pAlert = fromXML.IndexOf('!');
    //            int pInfo = fromXML.IndexOf('?');
    //            if (pInfo >= 0 && (pInfo < pAlert || pAlert < 0))
    //            {
    //                pAlert = pInfo;
    //                this.UnAlert();
    //            }
    //            else
    //            {
    //                if (pAlert >= 0)
    //                {
    //                    this.NeedsAlert();
    //                }
    //            }

    //            if (pAlert >= 0)
    //            {
    //                this.explanation = fromXML.Substring(pAlert + 1);
    //                fromXML = fromXML.Substring(0, pAlert);
    //            }

    //            switch (fromXML.ToLowerInvariant())
    //            {
    //                case "p":
    //                case "pass":
    //                case "passes":
    //                    this.level = BidLevels.Pass;
    //                    this.special = SpecialBids.Pass;
    //                    break;
    //                case "x":
    //                case "dbl":
    //                case "double":
    //                case "doubles":
    //                case "36":
    //                    this.level = BidLevels.Pass;
    //                    this.special = SpecialBids.Double;
    //                    break;
    //                case "xx":
    //                case "rdbl":
    //                case "redouble":
    //                case "redoubles":
    //                case "37":
    //                    this.level = BidLevels.Pass;
    //                    this.special = SpecialBids.Redouble;
    //                    break;
    //                default:
    //                    this.special = SpecialBids.NormalBid;
    //                    this.level = (BidLevels)(Convert.ToByte(fromXML[0]) - 48);
    //                    this.suit = SuitHelper.FromXML(fromXML.Substring(1));
    //                    break;
    //            }
    //        }

    //        /// <summary>
    //        /// Static constructor for use in comparing a bid to a constant
    //        /// </summary>
    //        /// <param name="fromXML">string representation of the bid (like 5S)</param>
    //        /// <returns>A bid</returns>
    //        public static Bid C(string fromXML)
    //        {
    //            return new Bid(fromXML);
    //        }

    //        /// <summary>
    //        /// Constructor of a bid that is based on a xml representation of the bid
    //        /// </summary>
    //        /// <param name="fromXML">The XML that contains the bid</param>
    //        /// <param name="explanation">An explanation of the bid that has been provided by the bidder</param>
    //        public Bid(string fromXML, string explanation)
    //            : this(fromXML)
    //        {
    //            this.explanation = explanation;
    //        }

    //        /// <summary>
    //        /// Just for serializer
    //        /// </summary>
    //        public Bid()
    //        {
    //        }

    //        #endregion

    //        #region properties

    //        /// <summary>Suit</summary>
    //        /// <value>?</value>
    //        public Suits Suit
    //        {
    //            get
    //            {
    //#if DEBUG
    //                if (!this.IsRegular) throw new InvalidOperationException("Suit of pass, double or redouble");
    //#endif
    //                return this.suit;
    //            }
    //        }

    //        /// <summary>Level</summary>
    //        /// <value>?</value>
    //        public BidLevels Level { get { return this.level; } }

    //        /// <summary>What kind of bid?</summary>
    //        /// <value>?</value>
    //        public SpecialBids Special { get { return this.special; } }

    //        /// <summary>Robot readable explanation of the bid</summary>
    //        /// <value></value>
    //        [IgnoreDataMember]
    //        public string Explanation
    //        {
    //            get { return this.explanation; }
    //            set { this.explanation = value; }
    //        }

    //        /// <summary>Human readable explanation of the bid</summary>
    //        /// <value></value>
    //        [IgnoreDataMember]
    //        public string HumanExplanation
    //        {
    //            get { return this.humanExplanation; }
    //            set { this.humanExplanation = value; }
    //        }

    //        /// <summary>Is the bid regular (can it become a contract)?</summary>
    //        /// <value>?</value>
    //        public bool IsRegular { get { return (this.special == SpecialBids.NormalBid); } }

    //        /// <summary>Is the bid a pass?</summary>
    //        /// <value>?</value>
    //        public bool IsPass { get { return (this.special == SpecialBids.Pass); } }

    //        /// <summary>Is the bid a double?</summary>
    //        /// <value>?</value>
    //        public bool IsDouble { get { return (this.special == SpecialBids.Double); } }

    //        /// <summary>Is the bid a redouble?</summary>
    //        /// <value>?</value>
    //        public bool IsRedouble { get { return (this.special == SpecialBids.Redouble); } }

    //        /// <summary>Constructor</summary>
    //        /// <value>?</value>
    //        public byte Hoogte { get { return (byte)this.level; } }

    //        /// <summary>Integer representation of a bid</summary>
    //        /// <value>int: 0=pass, 36=double, 37=redouble</value>
    //        [DataMember]
    //        public int Index
    //        {
    //            get
    //            {
    //                switch (this.Special)
    //                {
    //                    case SpecialBids.Double: return 36;
    //                    case SpecialBids.Redouble: return 37;
    //                    case SpecialBids.NormalBid: return ToIndex(this.level, this.suit);
    //                    default: return 0;
    //                }
    //            }

    //            set
    //            {
    //                switch (value)
    //                {
    //                    case 0: this.SetPass(); break;
    //                    case 36: this.SetDoublet(); break;
    //                    case 37: this.SetRedoublet(); break;
    //                    default:
    //                        {
    //                            this.suit = ToSuit(value);
    //                            this.level = ToLevel(value);
    //                            this.special = SpecialBids.NormalBid;
    //                        }
    //                        break;
    //                }
    //            }
    //        }

    //        public static BidLevels ToLevel(int bidIndex)
    //        {
    //            return (BidLevels)(1 + (bidIndex - 1) / 5);
    //        }

    //        public static Suits ToSuit(int bidIndex)
    //        {
    //            return (Suits)((bidIndex - 1) % 5);
    //        }

    //        public bool Alert { get { return this.alert; } }

    //        #endregion

    //        /// <summary>
    //        /// Static constructor for use in comparing a bid to a constant
    //        /// </summary>
    //        /// <param name="fromXML">string representation of the bid (like 5S)</param>
    //        /// <returns>The index of a bid</returns>
    //        public static int ToIndex(string fromXML)
    //        {
    //            if (fromXML.Contains(";"))
    //            {
    //                string[] parts = fromXML.Split(';');
    //                fromXML = parts[0];
    //            }

    //            if (fromXML.Contains("!"))
    //            {
    //                int p = fromXML.IndexOf('!');
    //                fromXML = fromXML.Substring(0, p);
    //            }

    //            switch (fromXML.ToLowerInvariant())
    //            {
    //                case "p":
    //                case "pass":
    //                case "passes":
    //                    return 0;
    //                case "x":
    //                case "dbl":
    //                case "double":
    //                case "doubles":
    //                case "36":
    //                    return 36;
    //                case "xx":
    //                case "rdbl":
    //                case "redouble":
    //                case "redoubles":
    //                case "37":
    //                    return 37;
    //                default:
    //                    return ToIndex(Convert.ToByte(fromXML[0]) - 48, SuitHelper.FromXML(fromXML.Substring(1)));
    //            }
    //        }

    //        /// <summary>
    //        /// Static constructor for use in comparing a bid to a constant
    //        /// </summary>
    //        /// <param name="fromXML">string representation of the bid (like 5S)</param>
    //        /// <returns>The index of a bid</returns>
    //        public static int ToIndex(int level, Suits suit)
    //        {
    //            return 5 * (level - 1) + (int)suit + 1;
    //        }

    //        /// <summary>
    //        /// Static constructor for use in comparing a bid to a constant
    //        /// </summary>
    //        /// <param name="fromXML">string representation of the bid (like 5S)</param>
    //        /// <returns>The index of a bid</returns>
    //        public static int ToIndex(BidLevels level, Suits suit)
    //        {
    //            return ToIndex((int)level, suit);
    //        }

    //        /// <summary>'Larger Than' operator</summary>
    //        /// <param name="b1">First bid to compare</param>
    //        /// <param name="b2">Second bid to compare</param>
    //        /// <returns>boolean</returns>
    //        public static bool operator >(Bid b1, Bid b2)
    //        {
    //            if (b1 == null) throw new ArgumentNullException("b1");
    //            if (b2 == null) throw new ArgumentNullException("b2");
    //            return b1.Index > b2.Index;
    //        }
    //        public static bool operator >(Bid b1, string b2)
    //        {
    //            if (b1 == null) throw new ArgumentNullException("b1");
    //            if (b2 == null) throw new ArgumentNullException("b2");
    //            return b1.Index > ToIndex(b2);
    //        }
    //        public static bool operator >(Bid b1, int b2)
    //        {
    //            if (b1 == null) throw new ArgumentNullException("b1");
    //            return b1.Index > b2;
    //        }

    //        /// <summary>Operator</summary>
    //        /// <param name="b1">First bid to compare</param>
    //        /// <param name="b2">Second bid to compare</param>
    //        /// <returns>boolean</returns>
    //        public static bool operator <(Bid b1, Bid b2)
    //        {
    //            if (b1 == null) throw new ArgumentNullException("b1");
    //            if (b2 == null) throw new ArgumentNullException("b2");
    //            return b1.Index < b2.Index;
    //        }
    //        public static bool operator <(Bid b1, string b2)
    //        {
    //            if (b1 == null) throw new ArgumentNullException("b1");
    //            if (b2 == null) throw new ArgumentNullException("b2");
    //            return b1.Index < ToIndex(b2);
    //        }
    //        public static bool operator <(Bid b1, int b2)
    //        {
    //            if (b1 == null) throw new ArgumentNullException("b1");
    //            return b1.Index < b2;
    //        }

    //        /// <summary>Operator</summary>
    //        /// <param name="b1">First bid to compare</param>
    //        /// <param name="b2">Second bid to compare</param>
    //        /// <returns>boolean</returns>
    //        public static bool operator >=(Bid b1, Bid b2)
    //        {
    //            if (b1 == null) throw new ArgumentNullException("b1");
    //            if (b2 == null) throw new ArgumentNullException("b2");
    //            return b1.Index >= b2.Index;
    //        }
    //        public static bool operator >=(Bid b1, string b2)
    //        {
    //            if (b1 == null) throw new ArgumentNullException("b1");
    //            if (b2 == null) throw new ArgumentNullException("b2");
    //            return b1.Index >= ToIndex(b2);
    //        }
    //        public static bool operator >=(Bid b1, int b2)
    //        {
    //            if (b1 == null) throw new ArgumentNullException("b1");
    //            return b1.Index >= b2;
    //        }

    //        /// <summary>Operator</summary>
    //        /// <param name="b1">First bid to compare</param>
    //        /// <param name="b2">Second bid to compare</param>
    //        /// <returns>boolean</returns>
    //        public static bool operator <=(Bid b1, Bid b2)
    //        {
    //            if (b1 == null) throw new ArgumentNullException("b1");
    //            if (b2 == null) throw new ArgumentNullException("b2");
    //            return b1.Index <= b2.Index;
    //        }
    //        public static bool operator <=(Bid b1, string b2)
    //        {
    //            if (b1 == null) throw new ArgumentNullException("b1");
    //            if (b2 == null) throw new ArgumentNullException("b2");
    //            return b1.Index <= ToIndex(b2);
    //        }
    //        public static bool operator <=(Bid b1, int b2)
    //        {
    //            if (b1 == null) throw new ArgumentNullException("b1");
    //            return b1.Index <= b2;
    //        }

    //        /// <summary>Operator</summary>
    //        /// <param name="b1">First bid to compare</param>
    //        /// <param name="b2">Second bid to compare</param>
    //        /// <returns>boolean</returns>
    //        public static bool operator ==(Bid b1, Bid b2)
    //        {
    //            return (Object.Equals(b1, null) && Object.Equals(b2, null))
    //              || (!Object.Equals(b2, null) && !Object.Equals(b1, null) && b1.Index == b2.Index);
    //        }

    //        /// <summary>Operator</summary>
    //        /// <param name="b1">First bid to compare</param>
    //        /// <param name="b2">Second bid to compare</param>
    //        /// <returns>boolean</returns>
    //        public static bool operator !=(Bid b1, Bid b2)
    //        {
    //            return !(b1 == b2);
    //        }

    //        /// <summary>Convert the bid to a string</summary>
    //        /// <returns>String</returns>
    //        public static string ToString(int bidIndex)
    //        {
    //            switch (bidIndex)
    //            {
    //                case 0: return "Pass";
    //                case 36: return "x";
    //                case 37: return "xx";
    //                default:
    //                    return ((int)ToLevel(bidIndex)).ToString() + SuitHelper.ToXML(ToSuit(bidIndex));
    //            }
    //        }

    //        /// <summary>Convert the bid to a string</summary>
    //        /// <returns>String</returns>
    //        public override string ToString()
    //        {
    //            switch (this.special)
    //            {
    //                case SpecialBids.Pass: return "Pass";
    //                case SpecialBids.Double: return "x";
    //                case SpecialBids.Redouble: return "xx";
    //                case SpecialBids.NormalBid:
    //                    return ((int)this.level).ToString() + SuitHelper.ToXML(this.suit);
    //                default: return "?";
    //            }
    //        }

    //        /// <summary>Convert the bid to a digit and a special suit character</summary>
    //        /// <returns>String</returns>
    //        public string ToSymbol()
    //        {
    //            string result;
    //            switch (this.special)
    //            {
    //                case SpecialBids.Pass:
    //                    result = LocalizationResources.Pass;
    //                    break;
    //                case SpecialBids.Double:
    //                    result = "x";
    //                    break;
    //                case SpecialBids.Redouble:
    //                    result = "xx";
    //                    break;
    //                case SpecialBids.NormalBid:
    //                    result = ((int)this.level).ToString() + (this.suit == Suits.NoTrump
    //            ? LocalizationResources.NoTrump
    //            : "" + SuitHelper.ToUnicode(this.suit));
    //                    break;
    //                default:
    //                    result = "?";
    //                    break;
    //            }

    //            if (this.alert)
    //            {
    //                result += "!";
    //            }

    //            return result;
    //        }

    //        /// <summary>Convert the bid to a XML string</summary>
    //        /// <returns>String</returns>
    //        public string ToXML()
    //        {
    //            string s;
    //            switch (this.special)
    //            {
    //                case SpecialBids.Pass:
    //                    s = "Pass";
    //                    break;
    //                case SpecialBids.Double:
    //                    s = "X";
    //                    break;
    //                case SpecialBids.Redouble:
    //                    s = "XX";
    //                    break;
    //                case SpecialBids.NormalBid:
    //                    s = ((int)this.level).ToString() + SuitHelper.ToXML(this.suit);
    //                    break;
    //                default:
    //                    return "?";
    //            }

    //            //if (this.alert) s += "!";
    //            return s;
    //        }

    //        /// <summary>Convert the bid to a localized string</summary>
    //        /// <returns>String</returns>
    //        public string ToText()
    //        {
    //            switch (this.special)
    //            {
    //                case SpecialBids.Pass: return LocalizationResources.Pass;
    //                case SpecialBids.Double: return "x";
    //                case SpecialBids.Redouble: return "xx";
    //                case SpecialBids.NormalBid:
    //                    return ((int)this.level).ToString() + SuitHelper.ToLocalizedString(this.suit).Substring(0, (this.suit == Suits.NoTrump ? 2 : 1));
    //                default: return "?";
    //            }
    //        }

    //        /// <summary>Is bid equal to provided object?</summary>
    //        /// <param name="obj">Object to compare to</param>
    //        /// <returns>boolean</returns>
    //        public override bool Equals(Object obj)
    //        {
    //            if (obj == null) return false;
    //            Bid b = obj as Bid;
    //            return this == b;
    //        }

    //        /// <summary>Is bid equal to provided object?</summary>
    //        /// <param name="obj">Object to compare to</param>
    //        /// <returns>boolean</returns>
    //        public bool Equals(Suits s, BidLevels height)
    //        {
    //            return this.level == height && this.suit == s;
    //        }

    //        /// <summary>Is bid equal to provided object?</summary>
    //        /// <param name="obj">Object to compare to</param>
    //        /// <returns>boolean</returns>
    //        public bool Equals(int height, Suits s)
    //        {
    //            return this.Hoogte == height && this.suit == s;
    //        }

    //        /// <summary>Returns a hashcode (always 0)</summary>
    //        /// <returns>0</returns>
    //        public override int GetHashCode()
    //        {
    //            return 0;    //re.GetHashCode()  im.GetHashCode();
    //        }

    //        /// <summary>Constructor</summary>
    //        public void SetPass() { this.special = SpecialBids.Pass; this.level = BidLevels.Pass; this.alert = false; }

    //        /// <summary>Constructor</summary>
    //        public void SetDoublet() { this.special = SpecialBids.Double; this.level = BidLevels.Pass; this.alert = false; }

    //        /// <summary>Constructor</summary>
    //        public void SetRedoublet() { this.special = SpecialBids.Redouble; this.level = BidLevels.Pass; this.alert = false; }

    //        /// <summary>Alter the bid</summary>
    //        /// <param name="l">Level of the bid</param>
    //        /// <param name="s">Suit of the bid</param>
    //        public void Set(int l, Suits s)
    //        {
    //            this.Set(l, s, false);
    //        }

    //        /// <summary>Alter the bid</summary>
    //        /// <param name="l">Level of the bid</param>
    //        /// <param name="s">Suit of the bid</param>
    //        /// <param name="alert">Alert the bid</param>
    //        public void Set(int l, Suits s, bool alert)
    //        {
    //            //if (l > 7 || l < 0) throw new FatalBridgeException("level must be 1..7: {0}", l);
    //            this.special = SpecialBids.NormalBid;
    //            this.suit = s;
    //            this.level = (BidLevels)l;
    //            this.alert = alert;
    //        }

    //        /// <summary>Difference between two bids</summary>
    //        /// <param name="anderBod">The other bid</param>
    //        /// <remarks>The second bid must be lower to get a positive number.
    //        /// 1S.Verschil(1H) == 1
    //        /// 1H.Verschil(1S) == -1
    //        /// </remarks>
    //        /// <returns>byte</returns>
    //        public int Verschil(Bid anderBod)
    //        {
    //            //return (byte)Math.Abs(this.Index - anderBod.Index);
    //            //19-05-06 minimum_bod.Verschil(4,H) <= 5 met minimum_bod=5S leverde true op
    //            //TODO: waar gaat het nu mis? waar reken ik op die abs()?
    //            if (anderBod == null) throw new ArgumentNullException("anderBod");
    //            return this.Index - anderBod.Index;
    //        }

    //        /// <summary>Difference between two bids</summary>
    //        /// <param name="h">Height</param>
    //        /// <param name="s">Suit</param>
    //        /// <returns>byte</returns>
    //        public int Verschil(int h, Suits s)
    //        {
    //            return this.Index - ToIndex(h, s);
    //        }

    //        /// <summary>Copy a bid</summary>
    //        /// <returns>The copied bid</returns>
    //        public Bid Clone()
    //        {
    //            return new Bid(this.Index, this.explanation, this.alert, this.humanExplanation);
    //        }

    //        /// <summary>Alter a bid</summary>
    //        /// <param name="b">The bid to change to</param>
    //        /// <param name="increase">The increment to add to the first parameter</param>
    //        public void Assign(Bid b, int increase)
    //        {
    //            if (b == null) throw new ArgumentNullException("b");
    //            this.Index = b.Index + increase;
    //            this.alert = false;
    //        }

    //        /// <summary>Some bid comparison</summary>
    //        /// <param name="anderBod">?</param>
    //        /// <param name="verhoging">?</param>
    //        /// <returns>?</returns>
    //        public bool IsOngeveer(Bid anderBod, int verhoging)
    //        {
    //            if (anderBod == null) throw new ArgumentNullException("anderBod");
    //            return (this.Index == anderBod.Index + verhoging);
    //        }

    //        public void NeedsAlert()
    //        {
    //            this.alert = true;
    //        }

    //        public void UnAlert()
    //        {
    //            this.alert = false;
    //        }
    //    }

    [DataContract]
    public sealed class Bid : IEquatable<Bid>
    {
        private byte _index;     // 0..37
        private byte _level;     // 1..7 or 0
        private byte _suit;      // 0..4
        private SpecialBids _special;

        private Bid(byte index, byte level, byte suit, SpecialBids special)
        {
            _index = index;
            _level = level;
            _suit = suit;
            _special = special;
        }

        [DataMember]
        public int Index
        {
            get
            {
                return _index;
            }

            private set
            {
                _index = (byte)value;
                if (_index == 0)
                {
                    _level = 0;
                    _suit = 0;
                    _special = SpecialBids.Pass;
                }
                else if (_index == 36)
                {
                    _level = 0;
                    _suit = 0;
                    _special = SpecialBids.Double;
                }
                else if (_index == 37)
                {
                    _level = 0;
                    _suit = 0;
                    _special = SpecialBids.Redouble;
                }
                else
                {
                    if (_index > 35) throw new ArgumentOutOfRangeException(nameof(value), "invalid index: " + _index);
                    _special = SpecialBids.NormalBid;
                    _suit = (byte)((_index - 1) % 5);
                    _level = (byte)(((_index - 1) / 5) + 1);
                }
            }
        }
        public BidLevels Level => (BidLevels)_level;
        public Suits Suit => (Suits)_suit;
        public SpecialBids Special => _special;

        [Obsolete("replace with Height")]
        public byte Hoogte => _level;

        public int Height => _level;

        public bool IsPass => _special == SpecialBids.Pass;
        public bool IsDouble => _special == SpecialBids.Double;
        public bool IsRedouble => _special == SpecialBids.Redouble;
        public bool IsRegular => _special == SpecialBids.NormalBid;

        public override string ToString()
        {
            return _special switch
            {
                SpecialBids.Pass => "Pass",
                SpecialBids.Double => "x",
                SpecialBids.Redouble => "xx",
                _ => _level.ToString() + SuitHelper.ToXML(Suit)
            };
        }

        /// <summary>Convert the bid to a XML string</summary>
        /// <returns>String</returns>
        public string ToXML()
        {
            string s;
            switch (_special)
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
                    s = ((int)_level).ToString() + SuitHelper.ToXML(Suit);
                    break;
                default:
                    return "?";
            }

            //if (this.alert) s += "!";
            return s;
        }

        public override int GetHashCode() => _index;

        public override bool Equals(object obj)
            => obj is Bid other && other.Index == _index;

        public bool Equals(Bid other)
            => other.Index == _index;

        public bool Equals(int level, Suits suit)
            => _level == level && _suit == (byte)suit;

        public static bool operator ==(Bid a, Bid b) => a.Index == b.Index;
        public static bool operator !=(Bid a, Bid b) => a.Index != b.Index;
        public static bool operator >(Bid a, Bid b) => a._index > b._index;
        public static bool operator <(Bid a, Bid b) => a._index < b._index;
        public static bool operator >=(Bid a, Bid b) => a._index >= b._index;
        public static bool operator <=(Bid a, Bid b) => a._index <= b._index;

        // ------------------------------------------------------------
        // Flyweight storage
        // ------------------------------------------------------------

        private static readonly Bid[] _all = CreateAll();

        private static Bid[] CreateAll()
        {
            var arr = new Bid[38];

            // 0 = pass
            arr[0] = new Bid(0, 0, 0, SpecialBids.Pass);

            // 1..35 = normal bids
            byte idx = 1;
            for (byte level = 1; level <= 7; level++)
            {
                for (byte suit = 0; suit <= 4; suit++)
                {
                    arr[idx] = new Bid(idx, level, suit, SpecialBids.NormalBid);
                    idx++;
                }
            }

            // 36 = double, 37 = redouble
            arr[36] = new Bid(36, 0, 0, SpecialBids.Double);
            arr[37] = new Bid(37, 0, 0, SpecialBids.Redouble);

            return arr;
        }

        // ------------------------------------------------------------
        // Factories
        // ------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bid Get(int index) => _all[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bid Get(BidLevels level, Suits suit) => Get((int)level, suit);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bid Get(int level, Suits suit)
        {
            if (level <= 0 || level > 7)
                throw new ArgumentOutOfRangeException(nameof(level), "Level must be between 1 and 7");
            return _all[ToIndex(level, suit)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bid GetPass()
            => _all[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bid GetDouble()
            => _all[36];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bid GetRedouble()
            => _all[37];

        public static int ToIndex(int level, Suits suit)
            => 5 * (level - 1) + (int)suit + 1;

        /// <summary>Difference between two bids</summary>
        /// <param name="anderBod">The other bid</param>
        /// <remarks>The second bid must be lower to get a positive number.
        /// 1S.Verschil(1H) == 1
        /// 1H.Verschil(1S) == -1
        /// </remarks>
        /// <returns>byte</returns>
        public int Verschil(Bid anderBod)
        {
            ArgumentNullException.ThrowIfNull(anderBod);
            return _index - anderBod.Index;
        }

        /// <summary>Difference between two bids</summary>
        /// <param name="h">Height</param>
        /// <param name="s">Suit</param>
        /// <returns>byte</returns>
        public int Verschil(int h, Suits s)
        {
            return _index - ToIndex(h, s);
        }

        /// <summary>Raise the level of the current bid by 1</summary>
        public Bid EenNiveauHoger()
        {
            if (_level >= 7) throw new FatalBridgeException("8-level not allowed");
            return Bid.Get(_index + 5);
        }

        /// <summary>Lower the level of the current bid by 1</summary>
        public Bid EenNiveauLager()
        {
            if (_level <= 1) throw new FatalBridgeException("0-level not allowed");
            return Bid.Get(_index - 5);
        }

        // ------------------------------------------------------------
        // Parsing
        // ------------------------------------------------------------

        public static Bid Parse(string text)
        {
            int idx = ToIndex(text);
            return _all[idx];
        }

        public static int ToIndex(string fromXML)
        {
            if (fromXML.Contains(';'))
                fromXML = fromXML.Split(';')[0];

            if (fromXML.Contains('!'))
                fromXML = fromXML[..fromXML.IndexOf('!')];

            return fromXML.ToLowerInvariant() switch
            {
                "p" or "pass" or "passes" => 0,
                "x" or "dbl" or "double" or "doubles" or "36" => 36,
                "xx" or "rdbl" or "redouble" or "redoubles" or "37" => 37,
                _ => ToIndex(
                        fromXML[0] - '0',
                        SuitHelper.FromXML(fromXML.Substring(1))),
            };
        }
    }

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public sealed class AuctionBid
    {
        public AuctionBid(Bid bid)
        {
            Bid = bid ?? throw new ArgumentNullException(nameof(bid));
            Explanation = "";
            HumanExplanation = "";
        }

        /// <summary>Constructor</summary>
        /// <param name="fromXML">XML describing the bid</param>
        [DebuggerStepThrough]
        public AuctionBid(string fromXML)
        {
            //if (fromXML == null) throw new ArgumentNullException("fromXML");
            Explanation = "";
            if (fromXML.Contains(';'))
            {
                string[] parts = fromXML.Split(';');
                if (parts.Length >= 2) Explanation = parts[1];
                if (parts.Length >= 3) HumanExplanation = parts[2];
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
                Explanation = fromXML.Substring(pAlert + 1);
                fromXML = fromXML.Substring(0, pAlert);
            }

            Bid = Bid.Parse(fromXML);
        }

        /// <summary>
        /// Constructor of a bid that is based on a xml representation of the bid
        /// </summary>
        /// <param name="fromXML">The XML that contains the bid</param>
        /// <param name="explanation">An explanation of the bid that has been provided by the bidder</param>
        public AuctionBid(string fromXML, string explanation)
            : this(fromXML)
        {
            Explanation = explanation;
        }

        public static AuctionBid Parse(string fromXML)
        {
            return new AuctionBid(fromXML);
        }

        [DataMember]
        public Bid Bid { get; set; }

        public bool Alert { get; set; }

        [IgnoreDataMember]
        public string Explanation { get; set; }

        [IgnoreDataMember]
        public string HumanExplanation { get; set; }

        [Obsolete("replace with Height")]
        public byte Hoogte => (byte)Bid.Height;

        public int Height => Bid.Height;

        [IgnoreDataMember]
        public bool Hint { get; set; }

        public override string ToString()
        {
            var s = Bid.ToString();
            if (Alert) s += "!";
            return s;
        }
        public BidLevels Level => Bid.Level;
        public Suits Suit => Bid.Suit;
        public SpecialBids Special => Bid.Special;

        public bool IsPass => Bid.IsPass;
        public bool IsDouble => Bid.IsDouble;
        public bool IsRedouble => Bid.IsRedouble;
        public bool IsRegular => Bid.IsRegular;

        public bool Equals(int level, Suits suit)
            => Bid.Equals(level, suit);

        public int Verschil(AuctionBid anderBod)
        {
            ArgumentNullException.ThrowIfNull(anderBod);
            return Bid.Verschil(anderBod.Bid);
        }

        public int Verschil(Bid anderBod)
        {
            ArgumentNullException.ThrowIfNull(anderBod);
            return Bid.Verschil(anderBod);
        }

        /// <summary>Difference between two bids</summary>
        /// <param name="h">Height</param>
        /// <param name="s">Suit</param>
        /// <returns>byte</returns>
        public int Verschil(int h, Suits s)
        {
            return Bid.Verschil(h, s);
        }

        /// <summary>Convert the bid to a XML string</summary>
        /// <returns>String</returns>
        public string ToXML() => Bid.ToXML();

        /// <summary>Convert the bid to a digit and a special suit character</summary>
        /// <returns>String</returns>
        public string ToSymbol()
        {
            string result;
            switch (this.Special)
            {
                case SpecialBids.Pass:
                    result = LocalizationResources.Pass;
                    break;
                case SpecialBids.Double:
                    result = "x";
                    break;
                case SpecialBids.Redouble:
                    result = "xx";
                    break;
                case SpecialBids.NormalBid:
                    result = ((int)this.Level).ToString() + (this.Suit == Suits.NoTrump
                                ? LocalizationResources.NoTrump
                                : "" + SuitHelper.ToUnicode(this.Suit));
                    break;
                default:
                    result = "?";
                    break;
            }

            if (this.Alert)
            {
                result += "!";
            }

            return result;
        }

        /// <summary>Convert the bid to a localized string</summary>
        /// <returns>String</returns>
        public string ToText()
        {
            switch (this.Special)
            {
                case SpecialBids.Pass: return LocalizationResources.Pass;
                case SpecialBids.Double: return "x";
                case SpecialBids.Redouble: return "xx";
                case SpecialBids.NormalBid:
                    return ((int)this.Level).ToString() + SuitHelper.ToLocalizedString(this.Suit).Substring(0, (this.Suit == Suits.NoTrump ? 2 : 1));
                default: return "?";
            }
        }

        public void NeedsAlert()
        {
            this.Alert = true;
        }

        public void UnAlert()
        {
            this.Alert = false;
        }

        /// <summary>Copy a bid</summary>
        /// <returns>The copied bid</returns>
        public AuctionBid Clone()
        {
            return new AuctionBid(this.Bid) { Explanation = this.Explanation, Alert = this.Alert, HumanExplanation = this.HumanExplanation };
        }

        /// <summary>
        /// Just for serializer
        /// </summary>
        public AuctionBid()
        {
            Explanation = "";
            HumanExplanation = "";
            Alert = false;
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

        public bool Bevat(AuctionBid call, string caller)
        {
            ArgumentNullException.ThrowIfNull(call);
            return Bevat(call.Bid.Index, caller);
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
        public bool BevatVolgende(AuctionBid bod, byte verhoging, string caller)
        {
            ArgumentNullException.ThrowIfNull(bod);
            return this.Bevat(bod.Bid.Index + verhoging, caller);
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
                result += $"[{Bid.Get(bid.Key)}, {bid.Value}] ";
            }

            return result;
        }
    }
}
