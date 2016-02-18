using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Sodes.Bridge.Base
{
    public delegate bool FactorEvaluator(string factor);
    public enum FactorInterpretation
    {
        False,
        Maybe,
        True
    }
    public delegate FactorInterpretation FactorInterpreter(string factor, Seats whoseRule);
    public delegate SpelerBeeld FactorConcluder(string factor, FactorInterpretation expectedOutcome, Seats whoseRule, bool afterwards);

    public static class Rule
    {
        [DebuggerStepThrough()]
        public static RuleExpression Parse(string rule)
        {
            //rule = rule.Replace(" ", "");
            StringBuilder theRule = new StringBuilder(rule);
            int p = 0;
            bool inComment = false;
            while (p < theRule.Length)
            {
                if (theRule[p] == ' ' & !inComment)
                {
                    theRule.Remove(p, 1);
                }
                else
                {
                    if (theRule[p] == '[') inComment = true;
                    else if (theRule[p] == ']') inComment = false;
                    p++;
                }
            }

            if (theRule.Length == 0) theRule.Append("true");		// empty rule can be used for 'always true'

            RuleExpression e = RuleExpression.Parse(theRule);
            if (theRule.Length > 0) throw new ArgumentException(string.Format("Parsed: {0} remainder: {1}", e, theRule));
            return e;
        }

        public static bool Evaluate(string rule, FactorEvaluator evaluator)
        {
            return Rule.Parse(rule).Evaluate(evaluator);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static bool IsValid(string rule, ref string error)
        {
            try
            {
                Rule.Parse(rule);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static SpelerBeeld Conclude(string rule, FactorInterpreter interpreter, FactorConcluder concluder, Seats whoseRule, bool afterwards)
        {
            try
            {
                var parsed = Rule.Parse(rule);
                return parsed.Conclude(interpreter, FactorInterpretation.True, concluder, whoseRule, afterwards);
            }
            catch (Exception x)
            {
                throw new ArgumentException(rule, x);
            }
        }
    }

    public class RuleExpression
    {
        internal List<RuleOrPart> orParts;

        public static RuleExpression Parse(StringBuilder theRule)
        {
            RuleExpression result = new RuleExpression();
            result.orParts = new List<RuleOrPart>();
            bool moreOrParts = false;
            do
            {
                result.orParts.Add(RuleOrPart.Parse(theRule));
                if (theRule.Length > 0 && theRule[0] == '+')
                {
                    moreOrParts = true;
                    theRule.Remove(0, 1);
                }
                else
                {
                    moreOrParts = false;
                }
            } while (moreOrParts);

            return result;
        }

        public bool Evaluate(FactorEvaluator evaluator)
        {
            bool result = false;
            foreach (var orPart in this.orParts)
            {
                if (orPart.Evaluate(evaluator))
                {
                    result = true;
                }
            }

            return result;
        }

        public override string ToString()
        {
            bool first = true;
            string result = string.Empty;
            foreach (var orPart in this.orParts)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    result += "+";
                }

                result += orPart.ToString();
            }

            return result;
        }

        public FactorInterpretation Interpret(FactorInterpreter interpreter, Seats whoseRule)
        {
            FactorInterpretation result = FactorInterpretation.False;
            foreach (var orPart in this.orParts)
            {
                switch (orPart.Interpret(interpreter, whoseRule))
                {
                    case FactorInterpretation.False:
                        break;
                    case FactorInterpretation.Maybe:
                        result = FactorInterpretation.Maybe;
                        break;
                    case FactorInterpretation.True:
                        return FactorInterpretation.True;			// if one element is true, the entire or is true
                }
            }

            return result;
        }

        public SpelerBeeld Conclude(FactorInterpreter interpreter, FactorInterpretation expectedOutcome, FactorConcluder concluder, Seats whoseRule, bool afterwards)
        {
            SpelerBeeld result = new SpelerBeeld();
            switch (expectedOutcome)
            {
                case FactorInterpretation.False:
                    // all parts have to be false
                    foreach (var orPart in this.orParts)
                    {
                        result.KGV(orPart.Conclude(interpreter, FactorInterpretation.False, concluder, whoseRule, afterwards));
                    }

                    return result;

                case FactorInterpretation.Maybe:
                    // nothing to conclude
                    return result;

                case FactorInterpretation.True:
                    if (this.orParts.Count == 1)
                    {
                        return this.orParts[0].Conclude(interpreter, FactorInterpretation.True, concluder, whoseRule, afterwards);
                    }
                    else
                    {
                        // if all other parts are false, the one remaining must be true
                        RuleOrPart maybePart = null;
                        int maybeCount = 0;
                        foreach (var orPart in this.orParts)
                        {
                            switch (orPart.Interpret(interpreter, whoseRule))
                            {
                                case FactorInterpretation.False:
                                    break;
                                case FactorInterpretation.Maybe:
                                    maybeCount++;
                                    maybePart = orPart;
                                    break;
                                case FactorInterpretation.True:
                                    return result;		// empty result, we already knew this
                            }
                        }

                        if (maybeCount == 1)
                        {
                            return maybePart.Conclude(interpreter, FactorInterpretation.True, concluder, whoseRule, afterwards);
                        }
                        else
                        {
                            return result;
                        }
                    }
            }

            return result;
        }

        public string BasicExplanation
        {
            get
            {
                StringBuilder result = new StringBuilder(100);
                if (this.orParts.Count == 1 && this.orParts[0].andParts.Count >= 1)
                {
                    int andPart = 0;
                    bool firstPartAdded = false;
                    while (andPart < this.orParts[0].andParts.Count)
                    {
                        var p = this.orParts[0].andParts[andPart];
                        if (!p.notApplied
                                && p.theFactor.nestedExpression != null
                                && p.theFactor.nestedExpression.orParts.Count == 1
                                && p.theFactor.nestedExpression.orParts[0].andParts.Count == 1
                                && p.theFactor.nestedExpression.orParts[0].andParts[0].notApplied
                                && p.theFactor.nestedExpression.orParts[0].andParts[0].theFactor.nestedExpression != null
                             )
                        {
                            var nested = p.theFactor.nestedExpression.orParts[0].andParts[0].theFactor.nestedExpression;
                            if (nested.orParts.Count == 1 && nested.orParts[0].andParts.Count == 1)
                            {
                                string e = this.orParts[0].andParts[andPart].ToString();
                                while (e.Contains("["))
                                {
                                    var p1 = e.IndexOf('[');
                                    e = e.Remove(p1, e.IndexOf(']') - p1 + 1);
                                }

                                if (e != "(!())"
                                    //&& e != "(!(human))"
                                     )
                                {
                                    result.Append((firstPartAdded ? "*" : "") + e);
                                    firstPartAdded = true;
                                }
                            }
                        }
                        else
                        {
                            result.Append((firstPartAdded ? "*" : "") + this.orParts[0].andParts[andPart].ToString());
                            firstPartAdded = true;
                        }

                        andPart++;
                    }
                }
                else
                {
                    result.Append(this.ToString());
                }

                return result.ToString();
            }
        }
    }

    public class RuleOrPart
    {
        internal List<RuleAndPart> andParts;

        public static RuleOrPart Parse(StringBuilder theRule)
        {
            RuleOrPart result = new RuleOrPart();
            result.andParts = new List<RuleAndPart>();
            bool moreAndParts = false;
            do
            {
                result.andParts.Add(RuleAndPart.Parse(theRule));
                if (theRule.Length > 0 && theRule[0] == '*')
                {
                    moreAndParts = true;
                    theRule.Remove(0, 1);
                }
                else
                {
                    moreAndParts = false;
                }
            } while (moreAndParts);
            return result;
        }

        public bool Evaluate(FactorEvaluator evaluator)
        {
            bool result = true;
            foreach (var andPart in this.andParts)
            {
                if (!andPart.Evaluate(evaluator))
                {
                    result = false;
                }
            }

            return result;
        }

        public override string ToString()
        {
            bool first = true;
            string result = string.Empty;
            foreach (var andPart in this.andParts)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    result += "*";
                }

                result += andPart.ToString();
            }

            return result;
        }

        public FactorInterpretation Interpret(FactorInterpreter interpreter, Seats whoseRule)
        {
            FactorInterpretation result = FactorInterpretation.True;
            foreach (var andPart in this.andParts)
            {
                switch (andPart.Interpret(interpreter, whoseRule))
                {
                    case FactorInterpretation.False:
                        return FactorInterpretation.False;			// if one element is false, the entire and is false
                    case FactorInterpretation.Maybe:
                        result = FactorInterpretation.Maybe;
                        break;
                    case FactorInterpretation.True:
                        break;
                }
            }

            return result;
        }

        public SpelerBeeld Conclude(FactorInterpreter interpreter, FactorInterpretation expectedOutcome, FactorConcluder concluder, Seats whoseRule, bool afterwards)
        {
            SpelerBeeld result = new SpelerBeeld();
            switch (expectedOutcome)
            {
                case FactorInterpretation.True:
                    // all parts have to be true
                    foreach (var andPart in this.andParts)
                    {
                        result.KGV(andPart.Conclude(interpreter, FactorInterpretation.True, concluder, whoseRule, afterwards));
                    }

                    return result;

                case FactorInterpretation.Maybe:
                    // nothing to conclude
                    return result;

                case FactorInterpretation.False:
                    if (this.andParts.Count == 1)
                    {
                        return this.andParts[0].Conclude(interpreter, FactorInterpretation.False, concluder, whoseRule, afterwards);
                    }
                    else
                    {
                        // if all other parts are true, the one remaining must be false
                        RuleAndPart maybePart = null;
                        int maybeCount = 0;
                        foreach (var andPart in this.andParts)
                        {
                            switch (andPart.Interpret(interpreter, whoseRule))
                            {
                                case FactorInterpretation.True:
                                    break;
                                case FactorInterpretation.Maybe:
                                    maybeCount++;
                                    maybePart = andPart;
                                    break;
                                case FactorInterpretation.False:
                                    return result;		// empty result, we already knew this
                            }
                        }

                        if (maybeCount == 1)
                        {
                            return maybePart.Conclude(interpreter, FactorInterpretation.False, concluder, whoseRule, afterwards);
                        }
                        else
                        {
                            return result;
                        }
                    }
            }

            return result;
        }
    }

    public class RuleAndPart
    {
        internal bool notApplied;
        internal RuleFactor theFactor;

        public static RuleAndPart Parse(StringBuilder theRule)
        {
            RuleAndPart result = new RuleAndPart();
            if (theRule[0] == '!')
            {
                result.notApplied = true;
                theRule.Remove(0, 1);
            }
            else
            {
                result.notApplied = false;
            }

            result.theFactor = RuleFactor.Parse(theRule);
            return result;
        }

        public bool Evaluate(FactorEvaluator evaluator)
        {
            bool result = this.theFactor.Evaluate(evaluator);
            if (this.notApplied)
                result = !result;

            return result;
        }

        public override string ToString()
        {
            return (this.notApplied ? "!" : "") + this.theFactor.ToString();
        }

        public FactorInterpretation Interpret(FactorInterpreter interpreter, Seats whoseRule)
        {
            FactorInterpretation interpretation = theFactor.Interpret(interpreter, whoseRule);
            if (this.notApplied)
            {
                switch (interpretation)
                {
                    case FactorInterpretation.False:
                        return FactorInterpretation.True;
                    case FactorInterpretation.True:
                        return FactorInterpretation.False;
                    default:
                        return FactorInterpretation.Maybe;
                }
            }

            return interpretation;
        }

        public SpelerBeeld Conclude(FactorInterpreter interpreter, FactorInterpretation expectedOutcome, FactorConcluder concluder, Seats whoseRule, bool afterwards)
        {
            if (this.notApplied)
            {
                switch (expectedOutcome)
                {
                    case FactorInterpretation.False:
                        expectedOutcome = FactorInterpretation.True;
                        break;
                    case FactorInterpretation.True:
                        expectedOutcome = FactorInterpretation.False;
                        break;
                }
            }

            return this.theFactor.Conclude(interpreter, expectedOutcome, concluder, whoseRule, afterwards);
        }
    }

    public class RuleFactor
    {
        internal RuleExpression nestedExpression;
        private string reconstruct;

        public static RuleFactor Parse(StringBuilder theRule)
        {
            RuleFactor result = new RuleFactor();
            result.reconstruct = string.Empty;

            if (theRule.Length > 0 && theRule[0] == '(')
            {
                theRule.Remove(0, 1);
                //if (theRule.ToString().Contains("[Stayman]")) System.Diagnostics.Debugger.Break();
                result.nestedExpression = RuleExpression.Parse(theRule);

                if (theRule.Length > 0 && theRule[0] == ')')
                {
                    theRule.Remove(0, 1);
                    if (theRule.Length > 0 && theRule[0] == '[')
                    {
                        result.reconstruct = string.Empty;
                        while (theRule[0] != ']')
                        {
                            result.reconstruct += theRule[0];
                            theRule.Remove(0, 1);
                        }
                        result.reconstruct += theRule[0];
                        theRule.Remove(0, 1);
                    }
                }
                else
                {
                    throw new ArgumentOutOfRangeException("Missing ) : " + theRule);
                }
            }
            else
            {
                while (theRule.Length > 0 && theRule[0] != '+' && theRule[0] != '*' && theRule[0] != ')'
                            )
                {
                    result.reconstruct += theRule[0];
                    theRule.Remove(0, 1);
                }

                //if (result.reconstruct.Length > 0 && result.reconstruct[result.reconstruct.Length - 1] == ']')
                //{
                //  result.reconstruct = result.reconstruct.Substring(0, result.reconstruct.IndexOf('['));
                //}

                //if (result.reconstruct.Length == 0)
                //{
                //  result.reconstruct = "true";
                //}

                if (result.reconstruct != "true")
                {
                    switch (result.reconstruct[0])
                    {
                        case 'o':
                            switch (result.reconstruct[1])
                            {
                                case '1':
                                case '2':   // double stopper
                                case '3':   // single (or half) stopper
                                case '4':
                                case '5':
                                case '6':
                                case '8':
                                case '9':   // 'playable' suit
                                    break;
                                case '7':		// only to show a given strength
                                    ValidateNumber(result.reconstruct.Substring(3, 1));
                                    break;
                                default:
                                    throw new InvalidOperationException("Unknown o factor o" + result.reconstruct[1]);
                            }

                            ValidateSuit(result.reconstruct[2]);
                            break;

                        case 'm':
                        case 'p':
                        case 's':
                            switch (result.reconstruct[1])
                            {
                                case 'a':
                                case 'b':
                                case 'c':
                                case 's':
                                case 't':
                                case 'g':
                                case 'S':
                                case 'H':
                                case 'D':
                                case 'C':
                                case 'R':
                                case 'K':
                                case 'N':
                                    break;
                                case 'o':   // po1030H5
                                    ValidateSuit(result.reconstruct[6]);
                                    ValidateNumber(result.reconstruct.Substring(7, 1));
                                    break;
                                default:
                                    throw new InvalidOperationException("Unknown point factor " + result.reconstruct[1]);
                            }

                            ValidateNumber(result.reconstruct.Substring(2, 2));
                            ValidateNumber(result.reconstruct.Substring(4, 2));
                            break;

                        case 't':
                            switch (result.reconstruct.Substring(1, 2))
                            {
                                case "01":
                                case "02":
                                case "03":
                                case "04":
                                case "05":
                                case "06":
                                case "07":
                                case "08":
                                case "09":
                                case "ho":      // Third Hand Opening
                                    break;
                                default:
                                    throw new InvalidOperationException("Unknown t factor " + result.reconstruct.Substring(1, 2));
                            }

                            break;

                        case 'K':
                        case 'R':
                        case 'H':
                        case 'S':
                        case 'D':
                        case 'C':
                            ValidateNumber(result.reconstruct.Substring(1));
                            break;

                        case '>':
                            if (result.reconstruct[1] == '=')
                            {
                                ValidateSuit(result.reconstruct[2]);
                                ValidateSuit(result.reconstruct[3]);
                            }
                            else
                            {
                                ValidateSuit(result.reconstruct[1]);
                                ValidateSuit(result.reconstruct[2]);
                            }

                            break;

                        case '<':
                            if (result.reconstruct[1] == '=')
                            {
                                ValidateSuit(result.reconstruct[2]);
                                ValidateSuit(result.reconstruct[3]);
                            }
                            else
                            {
                                ValidateSuit(result.reconstruct[1]);
                                ValidateSuit(result.reconstruct[2]);
                            }

                            break;

                        case '=':
                            ValidateSuit(result.reconstruct[1]);
                            ValidateNumber(result.reconstruct.Substring(2));
                            break;

                        case 'c':     // controls: 0 = no control, 1 = first round control (A or void), 2 = second round control (K or singleton)
                            switch (result.reconstruct[1])
                            {
                                case '0':
                                case '1':
                                case '2':
                                    break;

                                default:
                                    throw new InvalidOperationException("Unknown control factor " + result.reconstruct[1]);
                            }

                            ValidateSuit(result.reconstruct[2]);
                            break;

                        case 'r':
                            if (result.reconstruct.Substring(1, 2) != "23") throw new InvalidOperationException("Unknown r factor " + result.reconstruct.Substring(1, 2));
                            ValidateSuit(result.reconstruct[3]);
                            ValidateNumber(result.reconstruct.Substring(4));
                            break;

                        case 'v':
                            ValidateSuit(result.reconstruct[1]);
                            ValidateNumber(result.reconstruct.Substring(2));
                            break;

                        case 'a':     // ace-asking
                            switch (result.reconstruct[1])
                            {
                                case 'a':		// indicate number of aces
                                case 'b':		// indicate number of key cards
                                case 'k':		// indicate number of kings
                                case 'q':		// indicate number of trump-queens
                                case '?':		// indicate an asking bid
                                case '0':		// indicate an asking bid
                                case 'l':		// indicate number of losers
                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException("Unknown ace-asking factor " + result.reconstruct[1]);
                            }

                            break;

                        case 'b':     // bid-sequence
                            switch (result.reconstruct[1])
                            {
                                case 'p':
                                    break;

                                default:
                                    throw new InvalidOperationException("Unknown bid-sequence factor " + result.reconstruct[1]);
                            }

                            break;

                        case 'h':     // 'human': needed for bids that only exist to interpret 'weird' human bids
                            break;

                        case '[':     // '[Stayman]': comment for human players
                            break;

                        case 'u':     // v U lnerable
                            break;

                        case 'l':       // combined length
                            ValidateSuit(result.reconstruct[1]);
                            ValidateNumber(result.reconstruct.Substring(2));
                            break;

                        default:
                            throw new InvalidOperationException("Unknown factor " + result.reconstruct[0]);
                    }
                }
            }

            return result;
        }

        private static void ValidateSuit(char suit)
        {
            switch (suit)
            {
                case 'S':
                    break;
                case 'H':
                    break;
                case 'D':
                    break;
                case 'R':
                    break;
                case 'C':
                    break;
                case 'K':
                    break;
                case 'N':
                    break;
                case 'T':
                    break;
                default:
                    throw new InvalidOperationException("Unknown suit " + suit);
            }
        }

        private static void ValidateNumber(string number)
        {
            if (number.EndsWith("]"))
            {
                number = number.Substring(0, number.IndexOf('['));
            }
            foreach (var digit in number)
            {
                if (digit < '0' || digit > '9')
                {
                    throw new InvalidOperationException("Unknown digit " + digit);
                }
            }
        }


        public bool Evaluate(FactorEvaluator evaluator)
        {
            if (this.nestedExpression == null)
            {
                if (this.reconstruct.Length == 0 || this.reconstruct == "true")
                {
                    return true;
                }
                else
                {
                    return evaluator(this.reconstruct);
                }
            }
            else
            {
                return this.nestedExpression.Evaluate(evaluator);
            }
        }

        public override string ToString()
        {
            if (this.nestedExpression == null)
            {
                if (this.reconstruct == "true")
                {
                    return string.Empty;
                }
                else
                {
                    return this.reconstruct;
                }
            }
            else
            {
                return "(" + this.nestedExpression.ToString() + this.reconstruct + ")";
            }
        }

        public FactorInterpretation Interpret(FactorInterpreter interpreter, Seats whoseRule)
        {
            if (this.nestedExpression == null)
            {
                if (this.reconstruct == "true")
                {
                    return FactorInterpretation.True;
                }
                else
                {
                    return interpreter(this.reconstruct, whoseRule);
                }
            }
            else
            {
                return this.nestedExpression.Interpret(interpreter, whoseRule);
            }
        }

        public SpelerBeeld Conclude(FactorInterpreter interpreter, FactorInterpretation expectedOutcome, FactorConcluder concluder, Seats whoseRule, bool afterwards)
        {
            if (this.nestedExpression == null)
            {
                if (this.reconstruct == "true")
                {
                    return new SpelerBeeld();
                }
                else
                {
                    return concluder(this.reconstruct, expectedOutcome, whoseRule, afterwards);
                }
            }
            else
            {
                return this.nestedExpression.Conclude(interpreter, expectedOutcome, concluder, whoseRule, afterwards);
            }
        }

    }
}
