using System.Collections.Generic;

namespace SubnauticaObjectives.Graph;

// Evaluates boolean fact expressions parsed from the campaign graph rule strings.
//
// Grammar (precedence, lowest to highest):
//   expr   := term   ( 'OR'  term   )*
//   term   := factor ( 'AND' factor )*
//   factor := 'NOT' factor | '(' expr ')' | FACT
//   FACT   := [a-zA-Z_][a-zA-Z0-9_]*
public static class RuleParser
{
    // Returns true if every string in the rules list evaluates to true (implicit AND between list items).
    public static bool EvaluateAll(IReadOnlyList<string> rules, ISet<string> facts)
    {
        foreach (var rule in rules)
        {
            if (!Evaluate(rule, facts))
                return false;
        }
        return true;
    }

    // Evaluates a single boolean rule expression against the given fact set.
    public static bool Evaluate(string rule, ISet<string> facts)
    {
        if (string.IsNullOrWhiteSpace(rule))
            return true;

        var tokens = Tokenize(rule);
        int pos = 0;
        return ParseExpr(tokens, ref pos, facts);
    }

    private static bool ParseExpr(List<string> tokens, ref int pos, ISet<string> facts)
    {
        bool result = ParseTerm(tokens, ref pos, facts);
        while (pos < tokens.Count && tokens[pos] == "OR")
        {
            pos++;
            result |= ParseTerm(tokens, ref pos, facts);
        }
        return result;
    }

    private static bool ParseTerm(List<string> tokens, ref int pos, ISet<string> facts)
    {
        bool result = ParseFactor(tokens, ref pos, facts);
        while (pos < tokens.Count && tokens[pos] == "AND")
        {
            pos++;
            result &= ParseFactor(tokens, ref pos, facts);
        }
        return result;
    }

    private static bool ParseFactor(List<string> tokens, ref int pos, ISet<string> facts)
    {
        if (pos >= tokens.Count)
            return false;

        if (tokens[pos] == "NOT")
        {
            pos++;
            return !ParseFactor(tokens, ref pos, facts);
        }

        if (tokens[pos] == "(")
        {
            pos++; // consume '('
            bool result = ParseExpr(tokens, ref pos, facts);
            if (pos < tokens.Count && tokens[pos] == ")")
                pos++; // consume ')'
            return result;
        }

        // Plain fact name.
        return facts.Contains(tokens[pos++]);
    }

    private static List<string> Tokenize(string expr)
    {
        var tokens = new List<string>();
        int i = 0;
        while (i < expr.Length)
        {
            if (char.IsWhiteSpace(expr[i])) { i++; continue; }
            if (expr[i] == '(') { tokens.Add("("); i++; continue; }
            if (expr[i] == ')') { tokens.Add(")"); i++; continue; }

            int start = i;
            while (i < expr.Length && expr[i] != '(' && expr[i] != ')' && !char.IsWhiteSpace(expr[i]))
                i++;

            tokens.Add(expr.Substring(start, i - start));
        }
        return tokens;
    }
}
