using System;
using System.Runtime.Serialization;

namespace Bridge
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public enum Vulnerable
    {
        [EnumMember]
        Neither,
        [EnumMember]
        NS,
        [EnumMember]
        EW,
        [EnumMember]
        Both
    }

    public static class VulnerableConverter
    {
        public static Vulnerable FromXML(string value)
        {
            return value.Trim().ToLower() switch
            {
                "-" or "none" or "neither" => Vulnerable.Neither,
                "ns" => Vulnerable.NS,
                "ew" => Vulnerable.EW,
                "both" or "all" => Vulnerable.Both,
                _ => throw new ArgumentOutOfRangeException(value),
            };
        }
        public static Vulnerable FromBoardNumber(int value)
        {
            int board = 1 + ((((int)value) - 1) % 16);
            return board switch
            {
                1 or 8 or 11 or 14 => Vulnerable.Neither,
                2 or 5 or 12 or 15 => Vulnerable.NS,
                3 or 6 or 9 or 16 => Vulnerable.EW,
                _ => Vulnerable.Both,
            };
        }

        public static string ToXML(Vulnerable value)
        {
            return (Vulnerable)value switch
            {
                Vulnerable.Neither => "None",
                Vulnerable.NS => "NS",
                Vulnerable.EW => "EW",
                _ => "All",
            };
        }

        //public static string ToString(Vulnerable value)
        //{
        //    switch ((Vulnerable)value)
        //    {
        //        case Vulnerable.Neither: return LocalizationResources.Neither;
        //        case Vulnerable.NS: return LocalizationResources.NS;
        //        case Vulnerable.EW: return LocalizationResources.EW;
        //        default: return LocalizationResources.All;
        //    }
        //}

        /// <summary>
        /// Localized string
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Localized string</returns>
        public static string ToString2(this Vulnerable value)
        {
            return (Vulnerable)value switch
            {
                Vulnerable.Neither => LocalizationResources.Neither,
                Vulnerable.NS => LocalizationResources.NS,
                Vulnerable.EW => LocalizationResources.EW,
                _ => LocalizationResources.All,
            };
        }

        public static string ToBridgeProtocol(Vulnerable value)
        {
            return (Vulnerable)value switch
            {
                Vulnerable.Neither => "Neither",
                Vulnerable.NS => "N/S",
                Vulnerable.EW => "E/W",
                _ => "Both",
            };
        }

        public static string ToPbn(this Vulnerable v)
        {
            return v switch
            {
                Vulnerable.Neither => "None",
                Vulnerable.NS => "NS",
                Vulnerable.EW => "EW",
                Vulnerable.Both => "All",
                _ => throw new ArgumentOutOfRangeException(nameof(v)),
            };
        }

        public static Vulnerable Rotate(Vulnerable value)
        {
            return (Vulnerable)value switch
            {
                Vulnerable.Neither => Vulnerable.Neither,
                Vulnerable.NS => Vulnerable.EW,
                Vulnerable.EW => Vulnerable.NS,
                _ => Vulnerable.Both,
            };
        }
    }
}
