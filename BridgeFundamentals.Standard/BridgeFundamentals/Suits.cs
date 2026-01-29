using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Bridge
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public enum Suits : byte
    {
        [EnumMember] Clubs = 0,
        [EnumMember] Diamonds = 1,
        [EnumMember] Hearts = 2,
        [EnumMember] Spades = 3,
        [EnumMember] NoTrump = 4
    }

    public static class SuitHelper
    {
        [DebuggerStepThrough]
        public static Suits FromXML(string value)
        {
            return FromXML(value[0]);
        }

        [DebuggerStepThrough]
        public static Suits FromXML(ReadOnlySpan<char> value)
        {
            if (value.IsEmpty)
                throw new ArgumentException("Suit span cannot be empty.", nameof(value));

            return FromXML(value[0]);
        }

        [DebuggerStepThrough]
        public static Suits FromXML(char value)
        {
            return value switch
            {
                'C' or 'c' or 'K' or 'k' => Suits.Clubs,
                'D' or 'd' or 'R' or 'r' => Suits.Diamonds,
                'H' or 'h' => Suits.Hearts,
                'S' or 's' => Suits.Spades,
                'N' or 'n' or 'Z' or 'z' => Suits.NoTrump,
                _ => throw new FatalBridgeException($"SuitConverter.FromXML: unknown suit: {value}"),
            };
        }

        /// <summary>
        /// Convert to special Unicode characters that represent the symbols for clubs, diamonds, hearts and spades
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Unicode chacracter</returns>
        [DebuggerStepThrough]
        public static Char ToUnicode(this Suits value)
        {
            return value switch
            {
                Suits.Clubs => '♣',
                Suits.Diamonds => '♦',
                Suits.Hearts => '♥',
                Suits.Spades => '♠',
                Suits.NoTrump => 'N',
                _ => throw new FatalBridgeException($"SuitConverter.ToUnicode: unknown suit: {value}"),
            };
        }

        /// <summary>
        /// Convert to XML representation of suit
        /// </summary>
        /// <param name="value"></param>
        /// <returns>"C", "D", "H", "S" or "NT"</returns>
        [DebuggerStepThrough]
        public static string ToXML(this Suits value)
        {
            return value switch
            {
                Suits.Clubs => "C",
                Suits.Diamonds => "D",
                Suits.Hearts => "H",
                Suits.Spades => "S",
                Suits.NoTrump => "NT",
                _ => throw new FatalBridgeException($"SuitConverter.ToXML: unknown suit: {value}"),
            };
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
            return value switch
            {
                Suits.Clubs => "C",
                Suits.Diamonds => "D",
                Suits.Hearts => "H",
                Suits.Spades => "S",
                Suits.NoTrump => "N",
                _ => throw new FatalBridgeException($"SuitConverter.ToParser: unknown suit: {value}"),
            };
        }

        /// <summary>
        /// Convert to localized representation of suit
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Localized representation</returns>
        [DebuggerStepThrough]
        public static string ToLocalizedString(this Suits value)
        {
            return value switch
            {
                Suits.Clubs => LocalizationResources.Clubs,
                Suits.Diamonds => LocalizationResources.Diamonds,
                Suits.Hearts => LocalizationResources.Hearts,
                Suits.Spades => LocalizationResources.Spades,
                Suits.NoTrump => LocalizationResources.NoTrump,
                _ => throw new FatalBridgeException($"ToLocalizedString: unknown suit: {value}"),
            };
        }

        public static Suits Next(this Suits value)
        {
            if (!IsStandardSuit(value))
                throw new FatalBridgeException($"Suits.Next: unknown suit: {(int)value}");

            // Clubs(0)->Diamonds(1)->Hearts(2)->Spades(3)->Clubs(0)
            return (Suits)(((int)value + 1) & 3);
        }

        public static Suits Previous(this Suits value)
        {
            if (!IsStandardSuit(value))
                throw new FatalBridgeException($"Suits.Previous: unknown suit: {(int)value}");

            return (Suits)(((int)value + 3) & 3);
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

        /// <summary>
        /// action called for Clubs, Diamonds, Hearts, Spades
        /// </summary>
        /// <param name="toDo"></param>
        [DebuggerStepThrough]
        public static void ForEachSuit(Action<Suits> toDo)
        {
            toDo(Suits.Clubs);
            toDo(Suits.Diamonds);
            toDo(Suits.Hearts);
            toDo(Suits.Spades);
        }

        /// <summary>
        /// action called for Clubs, Diamonds, Hearts, Spades, NoTrump
        /// </summary>
        /// <param name="toDo"></param>
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
        public static bool AnySuit(Func<Suits, bool> isValid)
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

        public static ReadOnlySpan<Suits> StandardSuitsAscending => _standardSuitsAscending;

        public static ReadOnlySpan<Suits> StandardSuitsDescending => _standardSuitsDescending;

        public static ReadOnlySpan<Suits> TrumpSuitsAscending => _trumpSuitsAscending;

        public static ReadOnlySpan<Suits> TrumpSuitsDescending => _trumpSuitsDescending;

        private static readonly Suits[] _standardSuitsAscending =
            [
                Suits.Clubs, Suits.Diamonds, Suits.Hearts, Suits.Spades
            ];

        private static readonly Suits[] _standardSuitsDescending =
            [
                Suits.Spades, Suits.Hearts, Suits.Diamonds, Suits.Clubs
            ];

        private static readonly Suits[] _trumpSuitsAscending =
            [
                Suits.Clubs, Suits.Diamonds, Suits.Hearts, Suits.Spades, Suits.NoTrump
            ];

        private static readonly Suits[] _trumpSuitsDescending =
            [
                Suits.NoTrump, Suits.Spades, Suits.Hearts, Suits.Diamonds, Suits.Clubs
            ];

        private static bool IsStandardSuit(Suits s) => s <= Suits.Spades;
    }
}
