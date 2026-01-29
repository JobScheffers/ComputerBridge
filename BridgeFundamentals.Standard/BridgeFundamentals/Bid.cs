namespace Bridge
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Text;

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

    [DataContract]
    public sealed class Bid : IEquatable<Bid>
    {
        private byte _index;     // 0..37
        private byte _level;     // 1..7 or 0
        private byte _suit;      // 0..4
        private Suits __suit;
        private SpecialBids _special;

        private Bid(byte index, byte level, byte suit, SpecialBids special)
        {
            _index = index;
            _level = level;
            _suit = suit;
            __suit = (Suits)suit;
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

                __suit = (Suits)_suit;
            }
        }

        public BidLevels Level => (BidLevels)_level;
        public Suits Suit => __suit;
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

        //public override bool Equals(object obj)
        //    => obj is Bid other && other.Index == _index;
        public override bool Equals(object obj)
            => obj is Bid other && Equals(other);

        public bool Equals(Bid other)
            => other is not null && other._index == _index;
        
        public bool Equals(int level, Suits suit)
            => _level == level && _suit == (byte)suit;

        public static bool operator ==(Bid a, Bid b) => a?.Index == b?.Index;
        public static bool operator !=(Bid a, Bid b) => a?.Index != b?.Index;
        public static bool operator >(Bid a, Bid b) => a?._index > b?._index;
        public static bool operator <(Bid a, Bid b) => a?._index < b?._index;
        public static bool operator >=(Bid a, Bid b) => a?._index >= b?._index;
        public static bool operator <=(Bid a, Bid b) => a?._index <= b?._index;

        /// <summary>Some bid comparison</summary>
        /// <param name="anderBod">?</param>
        /// <param name="verhoging">?</param>
        /// <returns>?</returns>
        public bool IsOngeveer(Bid anderBod, int verhoging)
        {
            ArgumentNullException.ThrowIfNull(anderBod);
            return (_index == anderBod.Index + verhoging);
        }

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

        //public static int ToIndex(string fromXML)
        //{
        //    if (fromXML.Contains(';'))
        //        fromXML = fromXML.Split(';')[0];

        //    if (fromXML.Contains('!'))
        //        fromXML = fromXML[..fromXML.IndexOf('!')];

        //    return fromXML.ToLowerInvariant() switch
        //    {
        //        "p" or "pass" or "passes" => 0,
        //        "x" or "dbl" or "double" or "doubles" or "36" => 36,
        //        "xx" or "rdbl" or "redouble" or "redoubles" or "37" => 37,
        //        _ => ToIndex(
        //                fromXML[0] - '0',
        //                SuitHelper.FromXML(fromXML[1..])),
        //    };
        //}

        public static int ToIndex(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Bid text is empty", nameof(text));

            ReadOnlySpan<char> s = text.AsSpan().Trim();

            int semi = s.IndexOf(';');
            if (semi >= 0) s = s[..semi];

            int excl = s.IndexOf('!');
            if (excl >= 0) s = s[..excl];

            s = s.Trim();

            // keywords (case-insensitive) without ToLowerInvariant allocation
            if (s.Equals("p".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                s.Equals("pass".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                s.Equals("passes".AsSpan(), StringComparison.OrdinalIgnoreCase))
                return 0;

            if (s.Equals("x".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                s.Equals("dbl".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                s.Equals("double".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                s.Equals("doubles".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                s.Equals("36".AsSpan(), StringComparison.OrdinalIgnoreCase))
                return 36;

            if (s.Equals("xx".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                s.Equals("rdbl".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                s.Equals("redouble".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                s.Equals("redoubles".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
                s.Equals("37".AsSpan(), StringComparison.OrdinalIgnoreCase))
                return 37;

            // regular bid: "1S", "7NT", etc.
            if (s.Length < 2 || s[0] < '1' || s[0] > '7')
                throw new FormatException($"Invalid bid: '{text}'");

            int level = s[0] - '0';
            Suits suit = SuitHelper.FromXML(s[1..].ToString()); // if SuitHelper has Span overload, use it
            return ToIndex(level, suit);
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
            Alert = false;
        }

        /// <summary>Constructor</summary>
        /// <param name="fromXML">XML describing the bid</param>
        [DebuggerStepThrough]
        public AuctionBid(string fromXML)
        {
            //if (fromXML == null) throw new ArgumentNullException("fromXML");
            Explanation = "";
            Alert = false;
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
                Explanation = fromXML[(pAlert + 1)..];
                fromXML = fromXML[..pAlert];
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
            string result = this.Special switch
            {
                SpecialBids.Pass => LocalizationResources.Pass,
                SpecialBids.Double => "x",
                SpecialBids.Redouble => "xx",
                SpecialBids.NormalBid => ((int)this.Level).ToString() + (this.Suit == Suits.NoTrump
                                                ? LocalizationResources.NoTrump
                                                : "" + SuitHelper.ToUnicode(this.Suit)),
                _ => "?",
            };
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
            return this.Special switch
            {
                SpecialBids.Pass => LocalizationResources.Pass,
                SpecialBids.Double => "x",
                SpecialBids.Redouble => "xx",
                SpecialBids.NormalBid => string.Concat(((int)this.Level).ToString(), SuitHelper.ToLocalizedString(this.Suit).AsSpan(0, (this.Suit == Suits.NoTrump ? 2 : 1))),
                _ => "?",
            };
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
        //private readonly Dictionary<int, string> bids = [];

        private readonly string[] bids = new string[38];

        /// <summary>Check if the set contains the call</summary>
        /// <param name="call">The bid that will be searched in the set</param>
        /// <returns>Boolean indicating whether the call was found in the set</returns>
        public bool Peek(Bid call, string caller)
        {
            ArgumentNullException.ThrowIfNull(call);
            return bids[call.Index] == caller;
        }

        /// <summary>Check if the set contains the call</summary>
        /// <param name="call">The bid that will be searched in the set</param>
        /// <returns>Boolean indicating whether the call was found in the set</returns>
        public bool Bevat(int bidIndex, string caller)
        {
            if (bidIndex < 0 || bidIndex > 37) throw new ArgumentOutOfRangeException(nameof(bidIndex), "invalid bid index: " + bidIndex);

            var existing = bids[bidIndex];
            if (existing is null)
            {
                bids[bidIndex] = caller;
                return false; // wasn't present
            }

            // already present
            if (Exception(caller, existing)) return false;
            return true;
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

        //public override string ToString()
        //{
        //    string result = "";
        //    foreach (var bid in bids)
        //    {
        //        result += $"[{Bid.Get(bid.Key)}, {bid.Value}] ";
        //    }

        //    return result;
        //}
        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 38; i++)
            {
                var c = bids[i];
                if (c is not null)
                    sb.Append('[').Append(Bid.Get(i)).Append(", ").Append(c).Append("] ");
            }
            return sb.ToString();
        }
    }
}
