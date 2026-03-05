using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Bridge
{
    /// <summary>
    /// Summary description for CardPlay.
    /// </summary>

    public enum VirtualRanks { Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public enum Ranks : sbyte
    {
        [EnumMember]
        Two = 0,
        [EnumMember]
        Three,
        [EnumMember]
        Four,
        [EnumMember]
        Five,
        [EnumMember]
        Six,
        [EnumMember]
        Seven,
        [EnumMember]
        Eight,
        [EnumMember]
        Nine,
        [EnumMember]
        Ten,
        [EnumMember]
        Jack,
        [EnumMember]
        Queen,
        [EnumMember]
        King,
        [EnumMember]
        Ace,
        Null = -21,
        Unassigned = -22
    }

    public static class RankHelper
    {
        public const int Ace = (int)Ranks.Ace;
        public const int King = (int)Ranks.King;
        public const int Queen = (int)Ranks.Queen;
        public const int Jack = (int)Ranks.Jack;
        public const int Ten = (int)Ranks.Ten;
        public const int Nine = (int)Ranks.Nine;
        public const int Eight = (int)Ranks.Eight;
        public const int Seven = (int)Ranks.Seven;
        public const int Six = (int)Ranks.Six;
        public const int Five = (int)Ranks.Five;
        public const int Four = (int)Ranks.Four;
        public const int Three = (int)Ranks.Three;
        public const int Two = (int)Ranks.Two;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Ranks From(char value)
        {
            return value switch
            {
                '2' => Ranks.Two,
                '3' => Ranks.Three,
                '4' => Ranks.Four,
                '5' => Ranks.Five,
                '6' => Ranks.Six,
                '7' => Ranks.Seven,
                '8' => Ranks.Eight,
                '9' => Ranks.Nine,
                't' or 'T' => Ranks.Ten,
                'b' or 'j' or 'B' or 'J' => Ranks.Jack,
                'q' or 'v' or 'Q' or 'V' => Ranks.Queen,
                'h' or 'k' or 'H' or 'K' => Ranks.King,
                'a' or 'A' => Ranks.Ace,
                _ => throw new FatalBridgeException(string.Format("RankConverter.From(char): unknown rank: {0}", value)),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Ranks From(string value)
        {
            return value switch
            {
                "Two" => Ranks.Two,
                "Three" => Ranks.Three,
                "Four" => Ranks.Four,
                "Five" => Ranks.Five,
                "Six" => Ranks.Six,
                "Seven" => Ranks.Seven,
                "Eight" => Ranks.Eight,
                "Nine" => Ranks.Nine,
                "Ten" => Ranks.Ten,
                "Jack" => Ranks.Jack,
                "Queen" => Ranks.Queen,
                "King" => Ranks.King,
                "Ace" => Ranks.Ace,
                _ => From(value.Trim()[0]),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToXML(this Ranks value)
        {
            return value switch
            {
                Ranks.Two => "2",
                Ranks.Three => "3",
                Ranks.Four => "4",
                Ranks.Five => "5",
                Ranks.Six => "6",
                Ranks.Seven => "7",
                Ranks.Eight => "8",
                Ranks.Nine => "9",
                Ranks.Ten => "T",
                Ranks.Jack => "J",
                Ranks.Queen => "Q",
                Ranks.King => "K",
                Ranks.Ace => "A",
                _ => throw new FatalBridgeException(string.Format("RankConverter.ToXML: unknown rank: {0}", value)),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToXML(this VirtualRanks value)
        {
            return ToXML((Ranks)value);
        }

        /// <summary>
        /// Localized representation
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Localized representation</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToText(this Ranks value)
        {
            return value switch
            {
                Ranks.Two => "2",
                Ranks.Three => "3",
                Ranks.Four => "4",
                Ranks.Five => "5",
                Ranks.Six => "6",
                Ranks.Seven => "7",
                Ranks.Eight => "8",
                Ranks.Nine => "9",
                Ranks.Ten => LocalizationResources.Ten[..1],
                Ranks.Jack => LocalizationResources.Jack[..1],
                Ranks.Queen => LocalizationResources.Queen[..1],
                Ranks.King => LocalizationResources.King[..1],
                Ranks.Ace => LocalizationResources.Ace[..1],
                _ => throw new FatalBridgeException(string.Format("RankConverter.ToText: unknown rank: {0}", value)),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int HCP(this Ranks value)
        {
            int rank = (int)value;
            int isHonor = (rank - 9) >> 31;  // -1 if rank < 9, 0 if rank >= 9
            return (rank - 8) & ~isHonor;     // (rank - 8) if rank >= 9, else 0
        }

        public static ReadOnlySpan<Ranks> RanksAscending => _ranksAscending;

        public static ReadOnlySpan<Ranks> RanksDescending => _ranksDescending;

        private static readonly Ranks[] _ranksAscending =
            [
                Ranks.Two, Ranks.Three, Ranks.Four, Ranks.Five, Ranks.Six, Ranks.Seven, Ranks.Eight, Ranks.Nine, Ranks.Ten, Ranks.Jack, Ranks.Queen, Ranks.King, Ranks.Ace
            ];

        private static readonly Ranks[] _ranksDescending =
            [
                Ranks.Ace, Ranks.King, Ranks.Queen, Ranks.Jack, Ranks.Ten, Ranks.Nine, Ranks.Eight, Ranks.Seven, Ranks.Six, Ranks.Five, Ranks.Four, Ranks.Three, Ranks.Two
            ];
    }

    public class RankCollection<T>
    {
        private readonly T[] x = new T[13];

        public RankCollection(T initialValue)
        {
            foreach (var rank in RankHelper.RanksAscending)
                this[rank] = initialValue;
        }

        public RankCollection(T[] initialValues)
        {
            foreach (var rank in RankHelper.RanksAscending)
                this[rank] = initialValues[(int)rank];
        }

        public T this[Ranks index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return x[(int)index];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                x[(int)index] = value;
            }
        }
    }
}
