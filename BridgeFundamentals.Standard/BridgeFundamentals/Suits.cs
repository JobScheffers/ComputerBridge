using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Bridge
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public enum Suits
    {
        [EnumMember]
        Clubs,
        [EnumMember]
        Diamonds,
        [EnumMember]
        Hearts,
        [EnumMember]
        Spades,
        [EnumMember]
        NoTrump
    }

    public static class SuitHelper
    {
        [DebuggerStepThrough]
        public static Suits FromXML(string value)
        {
            return FromXML(value[0]);
        }

        [DebuggerStepThrough]
        public static Suits FromXML(char value)
        {
            switch (value)
            {
                case 'C':
                case 'c':
                case 'K':
                case 'k':
                    return Suits.Clubs;
                case 'D':
                case 'd':
                case 'R':
                case 'r':
                    return Suits.Diamonds;
                case 'H':
                case 'h':
                    return Suits.Hearts;
                case 'S':
                case 's':
                    return Suits.Spades;
                case 'N':
                case 'n':
                case 'Z':
                case 'z':
                    return Suits.NoTrump;
                default:
                    throw new FatalBridgeException(string.Format("SuitConverter.FromXML: unknown suit: {0}", value.ToString()));
            }
        }

        /// <summary>
        /// Convert to special Unicode characters that represent the symbols for clubs, diamonds, hearts and spades
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Unicode chacracter</returns>
        [DebuggerStepThrough]
        public static Char ToUnicode(this Suits value)
        {
            switch (value)
            {
                case Suits.Clubs:
                    return System.Convert.ToChar(9827);
                case Suits.Diamonds:
                    return System.Convert.ToChar(9830);
                case Suits.Hearts:
                    return System.Convert.ToChar(9829);
                case Suits.Spades:
                    return System.Convert.ToChar(9824);
                case Suits.NoTrump:
                    return 'N';
                default:
                    throw new FatalBridgeException($"SuitConverter.ToUnicode: unknown suit: {value.ToLocalizedString()}");
            }
        }

        /// <summary>
        /// Convert to XML representation of suit
        /// </summary>
        /// <param name="value"></param>
        /// <returns>"C", "D", "H", "S" or "NT"</returns>
        [DebuggerStepThrough]
        public static string ToXML(this Suits value)
        {
            switch (value)
            {
                case Suits.Clubs: return "C";
                case Suits.Diamonds: return "D";
                case Suits.Hearts: return "H";
                case Suits.Spades: return "S";
                case Suits.NoTrump: return "NT";
                default:
                    throw new FatalBridgeException($"SuitConverter.ToXML: unknown suit: {value.ToLocalizedString()}");
            }
        }

        /// <summary>
        /// Convert to character that will be recognized by the rule parser.
        /// Exception for NT
        /// </summary>
        /// <param name="value"></param>
        /// <returns>"C", "D", "H" or "S"</returns>
        [DebuggerStepThrough]
        public static string ToParser(this Suits value)
        {
            switch (value)
            {
                case Suits.Clubs: return "C";
                case Suits.Diamonds: return "D";
                case Suits.Hearts: return "H";
                case Suits.Spades: return "S";
                case Suits.NoTrump: return "N";
                default:
                    throw new FatalBridgeException($"SuitConverter.ToParser: unknown suit: {value.ToLocalizedString()}");
            }
        }

        /// <summary>
        /// Convert to localized representation of suit
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Localized representation</returns>
        [DebuggerStepThrough]
        public static string ToLocalizedString(this Suits value)
        {
            switch (value)
            {
                case Suits.Clubs: return LocalizationResources.Clubs;
                case Suits.Diamonds: return LocalizationResources.Diamonds;
                case Suits.Hearts: return LocalizationResources.Hearts;
                case Suits.Spades: return LocalizationResources.Spades;
                case Suits.NoTrump: return LocalizationResources.NoTrump;
                default:
                    throw new FatalBridgeException($"ToLocalizedString: unknown suit: {value.ToLocalizedString()}");
            }
        }

        /// <summary>
        /// next suit
        /// </summary>
        [DebuggerStepThrough]
        public static Suits Next(this Suits value)
        {
            switch (value)
            {
                case Suits.Clubs: return Suits.Diamonds;
                case Suits.Diamonds: return Suits.Hearts;
                case Suits.Hearts: return Suits.Spades;
                case Suits.Spades: return Suits.Clubs;
                default:
                    throw new FatalBridgeException($"Suits.Next: unknown suit: {value.ToLocalizedString()}");
            }
        }

        /// <summary>
        /// Previous suit
        /// </summary>
        [DebuggerStepThrough]
        public static Suits Previous(this Suits value)
        {
            switch (value)
            {
                case Suits.Clubs: return Suits.Spades;
                case Suits.Diamonds: return Suits.Clubs;
                case Suits.Hearts: return Suits.Diamonds;
                case Suits.Spades: return Suits.Hearts;
                default:
                    throw new FatalBridgeException($"Suits.Previous: unknown suit: {value.ToLocalizedString()}");
            }
        }

        /// <summary>
        /// Is it hearts or spades?
        /// </summary>
        [DebuggerStepThrough]
        public static bool IsMajor(this Suits value)
        {
            return value == Suits.Hearts || value == Suits.Spades;
        }

        /// <summary>
        /// Is it diamonds or clubs?
        /// </summary>
        [DebuggerStepThrough]
        public static bool IsMinor(this Suits value)
        {
            return value == Suits.Clubs || value == Suits.Diamonds;
        }

        [DebuggerStepThrough]
        public static void ForEachSuit(Action<Suits> toDo)
        {
            toDo(Suits.Clubs);
            toDo(Suits.Diamonds);
            toDo(Suits.Hearts);
            toDo(Suits.Spades);
        }

        [DebuggerStepThrough]
        public static void ForEachTrump(Action<Suits> toDo)
        {
            toDo(Suits.Clubs);
            toDo(Suits.Diamonds);
            toDo(Suits.Hearts);
            toDo(Suits.Spades);
            toDo(Suits.NoTrump);
        }

        [DebuggerStepThrough]
        public static void ForEachMajor(Action<Suits> toDo)
        {
            toDo(Suits.Hearts);
            toDo(Suits.Spades);
        }

        [DebuggerStepThrough]
        public static void ForEachMinor(Action<Suits> toDo)
        {
            toDo(Suits.Clubs);
            toDo(Suits.Diamonds);
        }

        /// <summary>
        /// Shortcut for long boolean expression that tries 4 suits 
        /// </summary>
        /// <param name="isValid">the condition for a suit</param>
        /// <returns>true if one suit complies</returns>
        public static unsafe bool AnySuit(Func<Suits, bool> isValid)
        {
            if (isValid(Suits.Clubs)) return true;
            if (isValid(Suits.Diamonds)) return true;
            if (isValid(Suits.Hearts)) return true;
            if (isValid(Suits.Spades)) return true;
            return false;
        }

        /// <summary>
        /// Shortcut for long boolean expression that tries 4 suits 
        /// </summary>
        /// <param name="isValid">the condition for a suit</param>
        /// <returns>true if all suits comply</returns>
        public static bool AllSuits(Func<Suits, bool> isValid)
        {
            if (!isValid(Suits.Clubs)) return false;
            if (!isValid(Suits.Diamonds)) return false;
            if (!isValid(Suits.Hearts)) return false;
            if (!isValid(Suits.Spades)) return false;
            return true;
        }
    }
}
