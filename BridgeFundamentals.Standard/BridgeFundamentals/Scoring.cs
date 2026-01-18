
using System.Runtime.Serialization;

namespace Bridge
{
    /// <summary>Enumeration of all possible scoring methodologies</summary>
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public enum Scorings
	{
        /// <summary>Only for looping purposes</summary>
        [EnumMember]
        scFirst,

		/// <summary>Cavendish scoring</summary>
        [EnumMember]
		scCavendish,

        /// <summary>Chicago scoring</summary>
        [EnumMember]
        scChicago,

        /// <summary>Rubber scoring</summary>
        [EnumMember]
        scRubber,

        /// <summary>European Match Points</summary>
        [EnumMember]
        scEMP,

        /// <summary>IMP scoring used between 1948 and 1960</summary>
        [EnumMember]
        scIMP_1948,

        /// <summary>IMP scoring revised in 1961</summary>
        [EnumMember]
        scIMP_1961,

        /// <summary>current IMP scoring since 1962</summary>
        [EnumMember]
        scIMP,

        /// <summary>Board-A-Match</summary>
        [EnumMember]
        scBAM,

        /// <summary>MatchPoint scoring</summary>
        [EnumMember]
        scMP,

        /// <summary>apply InstantScoreTable</summary>
        [EnumMember]
        scInstant,

        /// <summary>the trick point score is IMPed against the average value of all scores</summary>
        [EnumMember]
        scButler,

        /// <summary>as "Butler", but the 2 extreme scores are not used in computing the average value</summary>
        [EnumMember]
        scButler2,

        /// <summary>the trick point score is IMPed against a datum score determined by experts</summary>
        [EnumMember]
        scExperts,

        /// <summary>the trick point score is IMPed against every other trick point score, and summed</summary>
        [EnumMember]
        scCross,

        /// <summary>value of "Cross" , divided by number of scores</summary>
        [EnumMember]
        scCross1,

        /// <summary>value of "Cross" , divided by number of comparisons</summary>
        [EnumMember]
        scCross2,

        /// <summary>MatchPoints are computed as:  the sum of points, constructed by earning 2 points for each lower score, 1 point for each equal score, and 0 points for each higher score.</summary>
        [EnumMember]
        scMP1,

        /// <summary>MatchPoints are computed as:  the sum of points, constructed by earning 1 point for each lower score, 0.5 points for each equal score, and 0 points for each higher score.</summary>
        [EnumMember]
        scMP2,

        /// <summary>NO bonus of 100 (Doubled) or 200 (Redoubled) for the fourth and each subsequent undertrick, when not vulnerable</summary>
        [EnumMember]
        scOldMP,

        /// <summary>see http://www.gallery.uunet.be/hermandw/bridge/hermtd.html</summary>
        [EnumMember]
        scMitchell2,

        /// <summary>idem</summary>
        [EnumMember]
        scMitchell3,

        /// <summary>idem</summary>
        [EnumMember]
        scMitchell4,

        /// <summary>idem</summary>
        [EnumMember]
        scAscherman,

        /// <summary>idem</summary>
        [EnumMember]
        scBastille,

        /// <summary>?</summary>
        [EnumMember]
        scPairs,

        /// <summary>?</summary>
        [EnumMember]
        scMiniBridge,

        /// <summary>Only for looping purpose</summary>
        [EnumMember]
        scLast
    }

	public static class Scoring
	{
		public static int ToImp(int matchPoints)
		{
			int sign = matchPoints < 0 ? -1 : 1;
			if (matchPoints < 0) matchPoints *= -1;
			
			if (matchPoints < 0020) return 00 * sign;
			if (matchPoints < 0050) return 01 * sign;
			if (matchPoints < 0090) return 02 * sign;
			if (matchPoints < 0130) return 03 * sign;
			if (matchPoints < 0170) return 04 * sign;
			if (matchPoints < 0220) return 05 * sign;
			if (matchPoints < 0270) return 06 * sign;
			if (matchPoints < 0320) return 07 * sign;
			if (matchPoints < 0370) return 08* sign;
			if (matchPoints < 0430) return 09 * sign;
			if (matchPoints < 0500) return 10 * sign;
			if (matchPoints < 0600) return 11 * sign;
			if (matchPoints < 0750) return 12 * sign;
			if (matchPoints < 0900) return 13 * sign;
			if (matchPoints < 1100) return 14 * sign;
			if (matchPoints < 1300) return 15 * sign;
			if (matchPoints < 1500) return 16 * sign;
			if (matchPoints < 1750) return 17 * sign;
			if (matchPoints < 2000) return 18 * sign;
			if (matchPoints < 2250) return 19 * sign;
			if (matchPoints < 2500) return 20 * sign;
			if (matchPoints < 3000) return 21 * sign;
			if (matchPoints < 3500) return 22 * sign;
			if (matchPoints < 4000) return 23 * sign;
			return 24 * sign;
		}

		public static Scorings FromXml(string scoring)
		{
            return scoring switch
            {
                "Pairs" => Scorings.scPairs,
                _ => Scorings.scIMP,
            };
        }
	}
}
