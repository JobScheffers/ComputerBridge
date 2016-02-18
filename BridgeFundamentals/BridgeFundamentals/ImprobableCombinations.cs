using System.Collections.Generic;

namespace Sodes.Bridge.Base
{
	public class ImprobableCombinations
	{
		SeatCollection<SuitCollection<List<ImprobableCombination2>>> combinations;

		public ImprobableCombinations()
		{
			this.combinations = new SeatCollection<SuitCollection<List<ImprobableCombination2>>>();
			for (Seats p = Seats.North; p <= Seats.West; p++) this.combinations[p] = new SuitCollection<List<ImprobableCombination2>>();
		}

		public void Add(Seats p, Suits s, string newCombinations, double probabilty, string exceptions)
		{
			if (this.combinations[p][s] == null)
				this.combinations[p][s] = new List<ImprobableCombination2>();
			foreach (var item in newCombinations.Split(','))
			{
				bool found = false;
				foreach (var existingCombination in this.combinations[p][s])
				{
					if (existingCombination.Combination == item)
					{
						found = true;
                        if (probabilty > existingCombination.Probability || (existingCombination.Exceptions.Count > 0 && existingCombination.Exceptions[0].Length > 0 && exceptions.Length == 0))
						{
                            if (exceptions.Length == 0) existingCombination.Exceptions.Clear();
							bool existingExceptions = false;
							foreach (var exc in existingCombination.Exceptions)
							{
								if (exc.Length > 0) existingExceptions = true;
							}
							if (existingExceptions || exceptions.Length == 0)
							{
								existingCombination.Probability = probabilty;
								existingCombination.Exceptions.AddRange(exceptions.Split(','));
							}
						}
						break;
					}
				}

				if (!found)
				{
					this.combinations[p][s].Add(new ImprobableCombination2(item, probabilty, exceptions));
				}
			}
		}

		public double Improbability(Seats p, Suits s, string holding)
		{
			/// sample: 
			/// holding    = KQ94
			/// improbable = A,K,Q,J
			/// exception  = Q9
			double prb = 0;
			if (this.combinations[p][s] != null)
			{
				//if (p == Seats.South && holding.Contains("A")) System.Diagnostics.Debugger.Break();
				foreach (var item in this.combinations[p][s])
				{
					if (item.Combination.Substring(0, 1) == "-"
							&& holding.Length == 1
							&& item.Combination.IndexOf(holding) >= 0
						 )
					{
						if (item.Probability > prb) prb = item.Probability;
					}
					else
					{
						if (holding.IndexOf(item.Combination) >= 0
								|| (item.Combination == "x"
										&& holding.Length == 1
										&& holding[0] <= '9'
										&& holding[0] >= '2'
									 )
							 )
						{   // K is found in combination
							bool exceptionFound = false;
							foreach (var exception in item.Exceptions)
							{
								if (holding.Length == 1)
								{		// checking for improbable card
									/// improbable card found in improbable combinations
									/// but if this card exists in one of the exceptions, the card still could be valid
									if (exception.Contains(holding) && !exception.Contains("*"))
									{
										exceptionFound = true;
									}
								}
								else
								{
									string ex = exception.Contains("*") ? exception.Replace("*", "") : exception;
									if (holding.IndexOf(ex) >= 0
											&& ex.IndexOf(item.Combination) >= 0
										 )
									// because exception Q9 is also in combinations, check if K is found in exception
									{
										exceptionFound = true;
										break;
									}
								}
							}

							if (!exceptionFound)
							{
								// how probable is this improbable combination?
								if (item.Probability > prb) prb = item.Probability;
							}
						}
					}
				}
			}

			return prb;
		}

		//public bool Contains(Seats p, Suits s, string holding)
		//{
		//  /// sample: 
		//  /// holding    = KQ94
		//  /// improbable = A,K,Q,J
		//  /// exception  = Q9
		//  if (this.combinations[p][s] != null)
		//  {
		//    //if (p == Seats.South && holding.Contains("A")) System.Diagnostics.Debugger.Break();
		//    foreach (var item in this.combinations[p][s])
		//    {
		//      if (holding.IndexOf(item.Combination) >= 0)
		//      {   // K is found in combination
		//        bool exceptionFound = false;
		//        foreach (var exception in item.Exceptions)
		//        {
		//          if (holding.Length == 1)
		//          {		// checking for improbable card
		//            /// improbable card found in improbable combinations
		//            /// but if this card exists in one of the exceptions, the card still could be valid
		//            if (exception.Contains(holding))
		//            {
		//              exceptionFound = true;
		//            }
		//          }
		//          else
		//          {
		//            if (holding.IndexOf(exception) >= 0
		//                && exception.IndexOf(item.Combination) >= 0
		//               )
		//            // because exception Q9 is also in combinations, check if K is found in exception
		//            {
		//              exceptionFound = true;
		//            }
		//          }
		//        }

		//        if (!exceptionFound)
		//        {
		//          // how probable is this improbable combination?
		//          double probability = item.Probability;
		//          double random = RandomGenerator.Percentage();
		//          bool likely = random <= probability;
		//          return likely;
		//        }
		//      }
		//    }
		//  }

		//  return false;
		//}

		public void Remove(Seats p, Suits s, Ranks r)
		{
			/// sample: 
			/// improbable = AK,KQ
			/// after play of the K:
			/// improbable = A,Q
			string rank = Rank.ToXML(r);
			if (this.combinations[p][s] != null)
			{
				foreach (var item in this.combinations[p][s])
				{
					string combination = item.Combination;
					if (combination.Contains(rank))
					{
						combination = combination.Replace(rank, "");
						if (combination.Length == 0)
						{
							combination = "x";
						}

						item.Combination = combination;
					}
					else
					{
						//if (s == Suits.Diamonds && p == Seats.West) System.Diagnostics.Debugger.Break();
						if (item.Exceptions.Count == 2
								&& item.Exceptions[0].Contains("*")
								&& item.Exceptions[1].Contains("*")
								&& item.Combination != "x"
							 )
						{		// T* (indicates that T cannot be singleton), so if another card has been played, the T was not singleton
							//TODO: hoe te fixen: start 7, dus improbale T tenzij T*, nu niet bij de 7 al direct de boel leegmaken
							//item.Combination = string.Empty;
							if (item.Exceptions[0].Contains(rank))
							{
								item.Exceptions[0] = item.Exceptions[0].Replace(rank, "");
								item.Exceptions[1] = item.Exceptions[1].Replace(rank, "");
							}
							else
							{
								item.Combination = "x";
							}
						}
					}
				}
			}
		}

		public void Clear(Seats p, Suits s)
		{
			/// after not following the requested suit all improbable combinations for the requested suit can be removed. That suit is now 100% known.
			this.combinations[p][s] = null;
		}

		public void Clear()
		{
			for (Seats p = Seats.North; p <= Seats.West; p++)
			{
				for (Suits s = Suits.Clubs; s <= Suits.Spades; s++)
				{
					this.Clear(p, s);
				}
			}
		}

		private class ImprobableCombination2
		{
			public string Combination { get; set; }
			public double Probability { get; set; }
			public List<string> Exceptions { get; set; }

			public ImprobableCombination2(string c, double p, string e)
			{
				this.Combination = c;
				this.Probability = p;
				this.Exceptions = new List<string>();
				this.Exceptions.AddRange(e.Split(','));
			}
		}
	}
}
