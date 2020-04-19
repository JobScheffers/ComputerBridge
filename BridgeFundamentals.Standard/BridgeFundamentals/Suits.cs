using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Bridge
{
    public class ParserString { }  // om in type convert voor suit te kunnen omzetten naar K,R,H,S

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
        public const int Clubs = (int)Suits.Clubs;
        public const int Diamonds = (int)Suits.Diamonds;
        public const int Hearts = (int)Suits.Hearts;
        public const int Spades = (int)Suits.Spades;
        public const int NoTrump = (int)Suits.NoTrump;

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
                    throw new FatalBridgeException(string.Format("SuitConverter.FromXML: unknown suit: {0}", value));
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
                    throw new FatalBridgeException(string.Format("SuitConverter.ToUnicode: unknown suit: {0}", value));
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
                    throw new FatalBridgeException(string.Format("SuitConverter.ToXML: unknown suit: {0}", value));
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
                    throw new FatalBridgeException(string.Format("SuitConverter.ToParser: unknown suit: {0}", value));
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
                    throw new FatalBridgeException($"SuitConverter.ToString: unknown suit: {value}");
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
                    throw new FatalBridgeException(string.Format("Suits.Next: unknown suit: {0}", value));
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
                    throw new FatalBridgeException(string.Format("Suits.Previous: unknown suit: {0}", value));
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
            for (Suits s = Suits.Clubs; s <= Suits.Spades; s++)
            {
                toDo(s);
            }
        }

        [DebuggerStepThrough]
        public static void ForEachTrump(Action<Suits> toDo)
        {
            for (Suits s = Suits.Clubs; s <= Suits.NoTrump; s++)
            {
                toDo(s);
            }
        }

        [DebuggerStepThrough]
        public static void ForEachMajor(Action<Suits> toDo)
        {
            for (Suits s = Suits.Hearts; s <= Suits.Spades; s++)
            {
                toDo(s);
            }
        }

        [DebuggerStepThrough]
        public static void ForEachMinor(Action<Suits> toDo)
        {
            for (Suits s = Suits.Clubs; s.IsMinor(); s++)
            {
                toDo(s);
            }
        }

        /// <summary>
        /// Shortcut for long boolean expression that tries 4 suits 
        /// </summary>
        /// <param name="isValid">the condition for a suit</param>
        /// <returns>true if one suit complies</returns>
        public static bool AnySuit(Func<Suits, bool> isValid)
        {
            for (Suits s = Suits.Clubs; s <= Suits.Spades; s++)
            {
                if (isValid(s)) return true;
            }

            return false;
        }

        /// <summary>
        /// Shortcut for long boolean expression that tries 4 suits 
        /// </summary>
        /// <param name="isValid">the condition for a suit</param>
        /// <returns>true if one suit complies</returns>
        public static bool AllSuits(Func<Suits, bool> isValid)
        {
            for (Suits s = Suits.Clubs; s <= Suits.Spades; s++)
            {
                if (!isValid(s)) return false;
            }

            return true;
        }
    }
}
