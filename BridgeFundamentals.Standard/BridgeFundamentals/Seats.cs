using System.Collections.Generic;   // IEnumerator<T>
using System.Diagnostics;
using System.Runtime.Serialization;
using System;
using System.Threading.Tasks;

namespace Bridge
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public enum Seats
    {
        [EnumMember]
        North,
        [EnumMember]
        East,
        [EnumMember]
        South,
        [EnumMember]
        West
    }

    public static class SeatsExtensions
    {
        /// <summary>Seat that comes next to specified seat</summary>
        /// <param name="x">Seat for which to find the next seat</param>
        /// <returns>The next seat</returns>
        [DebuggerStepThrough]
        public static Seats Next(this Seats x)
        {
            return (Seats)((1 + (int)x) % 4);
            // above line is 1.4x faster than return x == Seats.West ? Seats.North : x + 1;
            // no boxing
        }

        /// <summary>Seat that comes before the specified seat</summary>
        /// <param name="x">Seat for which to find the previous seat</param>
        /// <returns>The previous seat</returns>
        [DebuggerStepThrough]
        public static Seats Previous(this Seats x)
        {
            return (Seats)((3 + (int)x) % 4);
        }

        /// <summary>Seat that partners with the specified seat</summary>
        /// <param name="x">Seat for which to find the partner</param>
        /// <returns>The partner seat</returns>
        [DebuggerStepThrough]
        public static Seats Partner(this Seats x)
        {
            return (Seats)((2 + (int)x) % 4);
        }

        [DebuggerStepThrough]
        public static Seats FromXML(char value)
        {
            return value switch
            {
                'N' or 'n' => Seats.North,
                'E' or 'O' or 'e' or 'o' => Seats.East,
                'S' or 'Z' or 's' or 'z' => Seats.South,
                'W' or 'w' => Seats.West,
                _ => throw new FatalBridgeException("Unknown seat: " + value),
            };
        }

        [DebuggerStepThrough]
        public static Seats FromXML(string value)
        {
            return FromXML(value[0]);
        }

        [DebuggerStepThrough]
        public static Seats DealerFromBoardNumber(int boardNumber)
        {
            int board = ((boardNumber - 1) % 4);
            return (Seats)board;
        }

        [DebuggerStepThrough]
        public static string ToXML(this Seats value)
        {
            return value switch
            {
                Seats.North => "N",
                Seats.East => "E",
                Seats.South => "S",
                _ => "W",
            };
        }

        [DebuggerStepThrough]
        public static string ToXMLFull(this Seats value)
        {
            return value switch
            {
                Seats.North => "North",
                Seats.East => "East",
                Seats.South => "South",
                _ => "West",
            };
        }

        /// <summary>
        /// Localized string
        /// </summary>
        [DebuggerStepThrough]
        public static string ToString(this Seats value)
        {
            return ToString2(value);
        }

        /// <summary>
        /// Localized string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static string ToString2(this Seats value)
        {
            return value switch
            {
                Seats.North => LocalizationResources.North,
                Seats.East => LocalizationResources.East,
                Seats.South => LocalizationResources.South,
                _ => LocalizationResources.West,
            };
        }


        [DebuggerStepThrough]
        public static Directions Direction(this Seats x)
        {
            return x switch
            {
                Seats.North or Seats.South => Directions.NorthSouth,
                Seats.East or Seats.West => Directions.EastWest,
                _ => Directions.NorthSouth,// voor de compiler
            };
        }

        [DebuggerStepThrough]
        public static bool IsSameDirection(this Seats s1, Seats s2)
        {
            return s1.Direction() == s2.Direction();
        }

        [DebuggerStepThrough]
        public static void ForEachSeat(Action<Seats> toDo)
        {
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                toDo(s);
            }
        }

        [DebuggerStepThrough]
        public static async Task ForEachSeatAsync(Func<Seats, Task> toDo)
        {
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                await toDo(s).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Shortcut for long boolean expression that tries 4 seats 
        /// </summary>
        /// <param name="isValid">the condition for a seat</param>
        /// <returns>true if one seat complies</returns>
        public static bool AnySeat(Func<Seats, bool> isValid)
        {
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                if (isValid(s)) return true;
            }

            return false;
        }

        /// <summary>
        /// Shortcut for long boolean expression that tries 4 seats 
        /// </summary>
        /// <param name="isValid">the condition for a seat</param>
        /// <returns>true if all seats comply</returns>
        public static bool AllSeats(Func<Seats, bool> isValid)
        {
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                if (!isValid(s)) return false;
            }

            return true;
        }

        public static readonly Seats Null = (Seats)(-1);
    }

    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public enum Directions { NorthSouth, EastWest }

    [DebuggerDisplay("{values}")]
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public class SeatCollection<T>
    {
        [DataMember]
        private Dictionary<Seats, T> values = [];

        public SeatCollection()
        {
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                this[s] = default;
            }
        }

        public SeatCollection(T[] initialValue)
        {
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                this[s] = initialValue[(int)s];
            }
        }

        [IgnoreDataMember]
        public T this[Seats index]
        {
            get
            {
                return values[index];
            }
            set
            {
                values[index] = value;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (Seats s = Seats.North; s <= Seats.West; s++)
                if (this[s] != null)
                    yield return this[s];
        }
    }

    public class DirectionDictionary<T> : Dictionary<Directions, T>
    {
        public DirectionDictionary(T valueNorthSouth, T valueEastWest)
        {
            this[Directions.NorthSouth] = valueNorthSouth;
            this[Directions.EastWest] = valueEastWest;
        }
    }
}
