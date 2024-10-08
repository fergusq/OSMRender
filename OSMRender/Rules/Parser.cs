// This file is part of OSMRender.
// Copyright (c) 2024 Iikka Hauhio
//
// OSMRender is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OSMRender is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OSMRender. If not, see <https://www.gnu.org/licenses/>.

using System.Globalization;
using System.Text.RegularExpressions;
using OSMRender.Geo;
using OSMRender.Logging;
using OSMRender.Render.Commands;
using OSMRender.Utils;

namespace OSMRender.Rules;

/// <summary>
/// Parser for Maperitive rulesets
/// </summary>
public class Parser {

    /// <summary>
    /// Represents syntax errors present in Maperitive rulesets
    /// </summary>
    public class SyntaxErrorException : Exception {
        internal SyntaxErrorException(string message) : base(message) {}
    }

    /// <summary>
    /// If the first item in the list is equal to any of the given alternatives, remove it from the list and return true, otherwise return false;
    /// </summary>
    /// <typeparam name="T">type of the tokens</typeparam>
    /// <param name="tokens">list of tokens</param>
    /// <param name="token">list of alternatives</param>
    /// <returns>true if item was removed, false otherwise</returns>
    private static bool TryEat<T>(IList<T> tokens, params T[] token) where T : IEquatable<T> {
        if (tokens.Count > 0 && (token.Length == 0 || token.Any(t => tokens[0].Equals(t)))) {
            //Console.WriteLine($"Eating {tokens[0]}");
            tokens.RemoveAt(0);
            return true;
        }
        return false;
    }

    /// <summary>
    /// If the first item in the list is equal to any of the given alternatives, remove it from the list. Otherwise, throw a syntax error.
    /// </summary>
    /// <typeparam name="T">type of the tokens</typeparam>
    /// <param name="tokens">list of tokens</param>
    /// <param name="token">list of alternatives</param>
    /// <returns>the item removed from the list</returns>
    /// <exception cref="SyntaxErrorException">if the first item in the list is not any of the alternatives</exception>
    private static T Eat<T>(IList<T> tokens, params T[] token) where T : IEquatable<T> {
        if (tokens.Count > 0 && (token.Length == 0 || token.Any(t => tokens[0].Equals(t)))) {
            //Console.WriteLine($"Eating {tokens[0]}");
            var ans = tokens[0];
            tokens.RemoveAt(0);
            return ans;
        } else if (tokens.Count > 0) {
            throw new SyntaxErrorException($"expected `{string.Join("' or `", token)}', got `{tokens[0]}'");
        } else {
            throw new SyntaxErrorException($"expected `{string.Join("' or `", token)}', got eof");
        }
    }

    /// <summary>
    /// Returns a list of lines, removing comments and empty lines
    /// </summary>
    /// <param name="rules">the content of the rule file as a string</param>
    /// <returns>list of lines</returns>
    private static List<string> ParseLines(string rules) {
        var lines = rules.Split('\n');
        var outLines = new List<string>();
        var prevIndent = 0;
        var indentLevel = new List<int>() { 0 };
        foreach (var line in lines) {
            var lineTrimmed = line.TrimEnd();
            var indent = 0;
            while (lineTrimmed.StartsWith("\t")) {
                indent++;
                lineTrimmed = lineTrimmed.Substring(1);
            }
            lineTrimmed = lineTrimmed.TrimStart();

            if (lineTrimmed.StartsWith("//")) {
                continue;
            }

            if (lineTrimmed == "") {
                continue;
            }

            if (indent > prevIndent) {
                outLines.Add("<indent>");
                indentLevel.Add(indent);
            } else if (indent < prevIndent) {
                while (indentLevel.Last() > indent) {
                    outLines.Add("<deindent>");
                    indentLevel.RemoveAt(indentLevel.Count-1);
                }
            }

            outLines.Add(lineTrimmed);
            prevIndent = indent;
        }
        while (indentLevel.Count > 1) {
            outLines.Add("<deindent>");
            indentLevel.RemoveAt(indentLevel.Count-1);
        }
        return outLines;
    }

    /// <summary>
    /// Parses the given rules into a Ruleset object.
    /// </summary>
    /// <param name="ruleString">the content of the ruleset file as a string</param>
    /// <param name="logger">logger instance (if null, a DummyLogger is created)</param>
    /// <returns>the parsed Ruleset</returns>
    public static Ruleset ParseRules(string ruleString, ILogger? logger = null) {
        logger ??= new DummyLogger();
        var lines = ParseLines(ruleString);
        var rules = new Ruleset(logger);
        var parser = new Parser(lines, rules, logger);
        parser.Parse();
        return rules;
    }

    private readonly List<string> Lines;
    private readonly Ruleset Rules;
    private readonly ILogger Logger;

    private Parser(List<string> lines, Ruleset rules, ILogger logger) {
        Lines = lines;
        Rules = rules;
        Logger = logger;
    }

    private bool TryEatLine(params string[] token) {
        return TryEat(Lines, token);
    }

    private string EatLine(params string[] token) {
        return Eat(Lines, token);
    }

    private string NextLine() {
        if (Lines.Count == 0) {
            throw new SyntaxErrorException("expected line, got eof");
        } else if (Lines[0] == "<indent>" || Lines[0] == "<deindent>") {
            throw new SyntaxErrorException($"expected line, got {Lines[0]}");
        }
        var ans = Lines[0];
        //Console.WriteLine($"NextLineing {ans}");
        Lines.RemoveAt(0);
        return ans;
    }

    private bool TryNextLine(out string line) {
        if (Lines.Count == 0) {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            line = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            return false;
        } else if (Lines[0] == "<indent>" || Lines[0] == "<deindent>") {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            line = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            return false;
        }
        line = Lines[0];
        //Console.WriteLine($"NextLineing {line}");
        Lines.RemoveAt(0);
        return true;
    }

    private static IEnumerable<string> TokenizeWithRegex(string pattern, string code) {
        var ans = new List<string>();
        var regex = new Regex(pattern);
        var pos = 0;
        while (regex.IsMatch(code.Substring(pos))) {
            var match = regex.Match(code.Substring(pos));
            ans.Add(match.Value);
            //Console.WriteLine($"Tokenized `{pattern}' `{match.Value}'");
            pos += match.Length;
        }
        if (pos != code.Length) {
            throw new SyntaxErrorException("cannot tokenize `" + code.Substring(pos) + "'");
        }
        return ans;
    }

    private void Parse() {
        while (Lines.Count > 0) {
            if (TryEatLine("features")) {
                if (TryEatLine("<indent>")) {
                    ParseFeatureSet();
                    while (!TryEatLine("<deindent>")) {
                        ParseFeatureSet();
                    }
                }
            } else if (TryEatLine("properties")) {
                if (TryEatLine("<indent>")) {
                    ParseProperties().ToList().ForEach(pair => Rules.Properties[pair.Key] = pair.Value);
                    while (!TryEatLine("<deindent>")) {
                        ParseProperties().ToList().ForEach(pair => Rules.Properties[pair.Key] = pair.Value);
                    }
                }
            } else if (TryEatLine("rules")) {
                if (TryEatLine("<indent>")) {
                    Rules.Rules.Add(ParseRule());
                    while (!TryEatLine("<deindent>")) {
                        Rules.Rules.Add(ParseRule());
                    }
                }
            } else {
                // This will give an error
                EatLine("features", "properties", "rules");
            }
        }
    }

    private void ParseFeatureSet() {
        var featureTypes = NextLine().Split(',').Select(f => f.Trim());
        if (!featureTypes.All(f => f == "points" || f == "areas" || f == "lines")) {
            throw new SyntaxErrorException("Unknown feature type: " + string.Join(", ", featureTypes));
        }
        if (TryEatLine("<indent>")) {
            while (TryNextLine(out var line)) {
                ParseFeature(featureTypes, line);
            }
            EatLine("<deindent>");
        }
    }

    private void ParseFeature(IEnumerable<string> featureTypes, string line) {
        if (!line.Contains(':')) {
            throw new SyntaxErrorException($"Invalid feature query `{line}', missing `:'");
        }

        var colon = line.IndexOf(':');
        var name = line.Substring(0, colon).Trim();
        var queryCode = line.Substring(colon+1).Trim();
        var query = ParseQuery(queryCode);

        foreach (var type in featureTypes) {
            if (type == "points") {
                Rules.PointFeatures.Add(name, query);
            } else if (type == "areas") {
                Rules.AreaFeatures.Add(name, query);
            } else if (type == "lines") {
                Rules.LineFeatures.Add(name, query);
            }
        }
    }

    /// <summary>
    /// Parses a feature query such as "area[natural=water]".
    /// </summary>
    /// <param name="queryCode">the query as a string</param>
    /// <returns>the IQuery object that can be used to test the query on map objects</returns>
    public static Ruleset.IQuery ParseQuery(string queryCode) {
        var tokens = TokenizeWithRegex(@"^(""[^""]*""|[\w@:\-]+|[=.,()[\] ])", queryCode).Where(s => s.Trim().Length > 0).ToList();
        var query = ParseOrQuery(tokens);
        while (tokens.Count > 0) {
            var right = ParsePrimitiveQuery(tokens);
            query = new BoolOpQuery(BoolOpQuery.Op.Or, query, right);
        }
        return query;
    }

    private static Ruleset.IQuery ParseOrQuery(List<string> tokens) {
        var left = ParseAndQuery(tokens);
        while (TryEat(tokens, "OR", "or")) {
            var right = ParseAndQuery(tokens);
            left = new BoolOpQuery(BoolOpQuery.Op.Or, left, right);
        }
        return left;
    }

    private static Ruleset.IQuery ParseAndQuery(List<string> tokens) {
        var left = ParseNestedQuery(tokens);
        while (TryEat(tokens, "AND", "and")) {
            var right = ParseNestedQuery(tokens);
            left = new BoolOpQuery(BoolOpQuery.Op.And, left, right);
        }
        return left;
    }

    private static Ruleset.IQuery ParseNestedQuery(List<string> tokens) {
        var left = ParsePrimitiveQuery(tokens);
        while (TryEat(tokens, ".")) {
            var right = ParsePrimitiveQuery(tokens);
            left = right; // TODO: not implemented
        }
        return left;
    }

    private static Ruleset.IQuery ParsePrimitiveQuery(List<string> tokens) {
        if (TryEat(tokens, "@isOneOf")) {
            Eat(tokens, "(");
            var key = Eat(tokens);
            var vals = new List<string>();
            while (TryEat(tokens, ",")) {
                vals.Add(Eat(tokens));
            }
            Eat(tokens, ")");
            return new TagQuery(key, vals);
        } else if (TryEat(tokens, "@isMulti")) {
            Eat(tokens, "(");
            var key = Eat(tokens);
            Eat(tokens, ",");
            var num = int.Parse(Eat(tokens));
            Eat(tokens, ")");
            return new IsMultiQuery(key, num);
        } else if (TryEat(tokens, "@isTrue")) {
            Eat(tokens, "(");
            var key = Eat(tokens);
            Eat(tokens, ")");
            return new IsBoolQuery(key, true);
        } else if (TryEat(tokens, "@isFalse")) {
            Eat(tokens, "(");
            var key = Eat(tokens);
            Eat(tokens, ")");
            return new IsBoolQuery(key, false);
        } else if (TryEat(tokens, "node")) {
            Ruleset.IQuery? subquery = null;
            if (TryEat(tokens, "[")) {
                if (tokens.Count > 0 && tokens[0] != "]") {
                    subquery = ParseOrQuery(tokens);
                }
                Eat(tokens, "]");
            }
            return new IsNodeQuery(subquery);
        } else if (TryEat(tokens, "area")) {
            Ruleset.IQuery? subquery = null;
            if (TryEat(tokens, "[")) {
                if (tokens.Count > 0 && tokens[0] != "]") {
                    subquery = ParseOrQuery(tokens);
                }
                Eat(tokens, "]");
            }
            return new IsAreaQuery(subquery);
        } else if (TryEat(tokens, "relation")) {
            Ruleset.IQuery? subquery = null;
            if (TryEat(tokens, "[")) {
                if (tokens.Count > 0 && tokens[0] != "]") {
                    subquery = ParseOrQuery(tokens);
                }
                Eat(tokens, "]");
            }
            return new IsRelationQuery(subquery);
        } else if (TryEat(tokens, "gpstrack")) {
            Ruleset.IQuery? subquery = null;
            if (TryEat(tokens, "[")) {
                if (tokens.Count > 0 && tokens[0] != "]") {
                    subquery = ParseOrQuery(tokens);
                }
                Eat(tokens, "]");
            }
            return new NullQuery();
        } else if (TryEat(tokens, "gpsroute")) {
            Ruleset.IQuery? subquery = null;
            if (TryEat(tokens, "[")) {
                if (tokens.Count > 0 && tokens[0] != "]") {
                    subquery = ParseOrQuery(tokens);
                }
                Eat(tokens, "]");
            }
            return new NullQuery();
        } else if (TryEat(tokens, "gpswaypoint")) {
            Ruleset.IQuery? subquery = null;
            if (TryEat(tokens, "[")) {
                if (tokens.Count > 0 && tokens[0] != "]") {
                    subquery = ParseOrQuery(tokens);
                }
                Eat(tokens, "]");
            }
            return new NullQuery();
        } else if (TryEat(tokens, "gpspoint")) {
            Ruleset.IQuery? subquery = null;
            if (TryEat(tokens, "[")) {
                if (tokens.Count > 0 && tokens[0] != "]") {
                    subquery = ParseOrQuery(tokens);
                }
                Eat(tokens, "]");
            }
            return new NullQuery();
        } else if (TryEat(tokens, "contour")) {
            Ruleset.IQuery? subquery = null;
            if (TryEat(tokens, "[")) {
                if (tokens.Count > 0 && tokens[0] != "]") {
                    subquery = ParseOrQuery(tokens);
                }
                Eat(tokens, "]");
            }
            return new NullQuery();
        } else if (TryEat(tokens, "(")) {
            var subquery = ParseOrQuery(tokens);
            Eat(tokens, ")");
            return subquery;
        } else if (TryEat(tokens, "[")) {
            var subquery = ParseOrQuery(tokens);
            Eat(tokens, "]");
            return subquery;
        } else if (TryEat(tokens, "NOT", "not")) {
            var subquery = ParsePrimitiveQuery(tokens);
            return new NotQuery(subquery);
        } else {
            var tag = Eat(tokens);
            if (TryEat(tokens, "=")) {
                var val = Eat(tokens);
                if (val.StartsWith("\"") && val.EndsWith("\"")) {
                    // TODO purkkaa
                    val = val.Substring(1, val.Length-2);
                }
                return new TagQuery(tag, [val]);
            } else {
                return new TagExistsQuery(tag);
            }
        }
    }

    /// <summary>
    /// Represents a tag query, i.e. either tag=value or @isAnyOf(tag, value1, value2).
    /// If there are no values, TagExistQuery should be used instead.
    /// </summary>
    private readonly struct TagQuery(string key, IEnumerable<string> val) : Ruleset.IQuery {

        private readonly string Key = key;
        private readonly IEnumerable<string> Val = val;

        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            return obj.Tags is not null && obj.Tags.ContainsKey(Key) && Val.Contains(obj.Tags[Key]);
        }
    }

    /// <summary>
    /// Represents an existence query (a tag query without any values).
    /// </summary>
    private readonly struct TagExistsQuery(string key) : Ruleset.IQuery {

        private readonly string Key = key;

        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            return obj.Tags is not null && obj.Tags.ContainsKey(Key);
        }
    }

    /// <summary>
    /// Represents a @isMulti query.
    /// </summary>
    private readonly struct IsMultiQuery(string key, int num) : Ruleset.IQuery {

        private readonly string Key = key;
        private readonly int Num = num;

        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            return obj.Tags is not null && obj.Tags.ContainsKey(Key) && obj.Tags[Key].ParseInvariantDouble() % Num == 0;
        }
    }

    /// <summary>
    /// Represents an @isTrue or @isFalse query.
    /// </summary>
    private readonly struct IsBoolQuery(string key, bool val) : Ruleset.IQuery {

        private readonly string Key = key;
        private readonly bool Val = val;

        private static readonly HashSet<string> TruthyValues = ["yes", "1", "true"];
        private static readonly HashSet<string> FalsyValues = ["no", "0", "false"];

        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            if (obj.Tags is not null && obj.Tags.ContainsKey(Key)) {
                return (Val ? TruthyValues : FalsyValues).Contains(obj.Tags[Key]);
            } else {
                return !Val;
            }
        }
    }

    /// <summary>
    /// Represents either an "A OR B" or "A AND B" query.
    /// </summary>
    private readonly struct BoolOpQuery(BoolOpQuery.Op operation, params Ruleset.IQuery[] queries) : Ruleset.IQuery {

        public enum Op {
            Or,
            And,
        }

        private readonly Op Operation = operation;
        private readonly Ruleset.IQuery[] Queries = queries;

        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            return Operation == Op.Or ? Queries.Any(q => q.Matches(doc, obj)) : Queries.All(q => q.Matches(doc, obj));
        }
    }

    /// <summary>
    /// Represents a node[] query.
    /// </summary>
    private readonly struct IsNodeQuery(Ruleset.IQuery? subquery) : Ruleset.IQuery {

        private readonly Ruleset.IQuery? Subquery = subquery;

        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            return doc.Points.ContainsKey(obj.Id) && (Subquery is null || Subquery.Matches(doc, obj));
        }
    }

    /// <summary>
    /// Represents a line[] query.
    /// </summary>
    private readonly struct IsLineQuery(Ruleset.IQuery? subquery) : Ruleset.IQuery {

        private readonly Ruleset.IQuery? Subquery = subquery;

        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            return doc.Lines.ContainsKey(obj.Id) && (Subquery is null || Subquery.Matches(doc, obj));
        }
    }

    /// <summary>
    /// Represents a area[] query.
    /// </summary>
    private readonly struct IsAreaQuery(Ruleset.IQuery? subquery) : Ruleset.IQuery {

        private readonly Ruleset.IQuery? Subquery = subquery;

        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            return doc.Areas.ContainsKey(obj.Id) && (Subquery is null || Subquery.Matches(doc, obj));
        }
    }

    /// <summary>
    /// Represents a relation[] query.
    /// </summary>
    private readonly struct IsRelationQuery(Ruleset.IQuery? subquery) : Ruleset.IQuery {

        private readonly Ruleset.IQuery? Subquery = subquery;

        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            return doc.Relations.ContainsKey(obj.Id) && (Subquery is null || Subquery.Matches(doc, obj));
        }
    }

    /// <summary>
    /// Represents a "NOT A" query.
    /// </summary>
    private readonly struct NotQuery(Ruleset.IQuery subquery) : Ruleset.IQuery {

        private readonly Ruleset.IQuery Subquery = subquery;

        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            return !Subquery.Matches(doc, obj);
        }
    }

    /// <summary>
    /// Represents a query that always fails. Used for unimplemented query types.
    /// </summary>
    private readonly struct NullQuery : Ruleset.IQuery {
        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            return false;
        }
    }

    private IDictionary<string, string> ParseProperties() {
        var ans = new Dictionary<string, string>();
        while (TryNextLine(out var line)) {
            if (!line.Contains(':')) {
                throw new SyntaxErrorException($"Invalid property `{line}', missing `:'");
            }

            var key = line.Substring(0, line.IndexOf(':')).Trim();
            var value = line.Substring(line.IndexOf(':')+1).Trim();
            ans[key] = value;
        }
        return ans;
    }

    private (string, string) ParseStatementWithArg() {
        var line = NextLine();
        if (!line.Contains(':')) {
            throw new SyntaxErrorException($"Invalid statement `{line}', missing `:'");
        }
        var key = line.Substring(0, line.IndexOf(':')).Trim();
        var val = line.Substring(line.IndexOf(':') + 1).Trim();
        return (key, val);
    }

    private string ParseStatementWithArg(string expectedKey) {
        var (stmt, val) = ParseStatementWithArg();
        if (stmt != expectedKey) {
            throw new SyntaxErrorException($"Invalid command `{stmt}', expected `{expectedKey}'");
        }
        return val;
    }

    private bool TryParseStatementWithArg(string expectedKey, out string value) {
        if (Lines.Count == 0 || !Lines[0].Contains(':')) {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            value = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            return false;
        }
        var key = Lines[0].Substring(0, Lines[0].IndexOf(':')).Trim();
        var val = Lines[0].Substring(Lines[0].IndexOf(':') + 1).Trim();
        if (key != expectedKey) {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            value = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            return false;
        }
        Lines.RemoveAt(0);
        value = val;
        return true;
    }

    private Ruleset.IRule ParseRule() {
        var cond = ParseStatementWithArg("target");
        ICondition condition = ParseCondition(cond);
        var stmts = ParseStatements();
        return new Rule(condition, stmts, Logger);
    }

    private static ICondition ParseCondition(string cond) {
        if (cond == "$featuretype(point)") {
            return new QueryCondition(new IsNodeQuery());
        } else if (cond == "$featuretype(line)") {
            return new QueryCondition(new IsLineQuery());
        } else if (cond == "$featuretype(area)") {
            return new QueryCondition(new IsAreaQuery());
        } else if (cond == "$featuretype(any)") {
            return new TrueCondition();
        } else if (cond.StartsWith("$regex(\"") && cond.EndsWith("\")")) { // TODO purkkaa!
            var pattern = cond.Substring("$regex(\"".Length);
            pattern = pattern.Substring(0, pattern.Length - 2);
            return new RegexCondition(new Regex(pattern));
        } else if (cond.StartsWith("$")) {
            throw new SyntaxErrorException($"Invalid target: `{cond}'");
        } else {
            cond = "^" + cond.Replace("*", ".*") + "$"; // TODO purkkaa!
            return new RegexCondition(new Regex(cond));
        }
    }

    private IEnumerable<IStatement> ParseStatements() {
        EatLine("<indent>");
        var stmts = new List<IStatement>();
        while (!TryEatLine("<deindent>")) {
            stmts.Add(ParseStatement());
        }
        return stmts;
    }

    private IStatement ParseStatement() {
        if (TryEatLine("define")) {
            if (TryEatLine("<indent>")) {
                var properties = ParseProperties();
                EatLine("<deindent>");
                return new DefineStatement(properties);
            } else {
                return new DefineStatement(new Dictionary<string, string>());
            }
        } else if (TryEatLine("stop")) {
            return new StopStatement();
        } else {
            var (key, val) = ParseStatementWithArg();
            if (key == "draw") {
                return new DrawStatement(val, Lines.Count, Logger);
            } else if (key == "for") {
                var query = ParseQuery(val);
                var stmts = ParseStatements();
                return new IfStatement(
                    [(new QueryCondition(query), stmts)],
                    []
                );
            } else if (key == "if") {
                var branches = new List<(ICondition, IEnumerable<IStatement>)>();
                var condition = ParseCondition(val);
                var stmts = ParseStatements();
                branches.Add((condition, stmts));
                while (TryParseStatementWithArg("elseif", out var elseCond)) {
                    var branchCondition = ParseCondition(elseCond);
                    var branchStmts = ParseStatements();
                    branches.Add((branchCondition, branchStmts));
                }
                IEnumerable<IStatement> elseStmts;
                if (TryEatLine("else")) {
                    elseStmts = ParseStatements();
                } else {
                    elseStmts = [];
                }
                return new IfStatement(branches, elseStmts);
            } else {
                throw new SyntaxErrorException($"Invalid command `{key}' : `{val}'");
            }
        }
    }

    /// <summary>
    /// Represents a condition of an if statement (typuically RegexCondition) or a for statement (typically a QueryCondition).
    /// </summary>
    private interface ICondition {
        public bool Matches(GeoDocument doc, Ruleset.Feature feature);
    }

    /// <summary>
    /// A condition that always succeeds (used for $featuretype(any)).
    /// </summary>
    private readonly struct TrueCondition : ICondition {
        public bool Matches(GeoDocument doc, Ruleset.Feature feature) {
            return true;
        }
    }

    /// <summary>
    /// A condition that matches the feature name against a regular expession.
    /// </summary>
    private readonly struct RegexCondition(Regex regex) : ICondition {

        private readonly Regex Regex = regex;

        public bool Matches(GeoDocument doc, Ruleset.Feature feature) {
            return Regex.IsMatch(feature.Name);
        }
    }

    /// <summary>
    /// A condition that tests the feature object against a query.
    /// </summary>
    private readonly struct QueryCondition(Ruleset.IQuery query) : ICondition {

        private readonly Ruleset.IQuery Query = query;

        public bool Matches(GeoDocument doc, Ruleset.Feature feature) {
            return Query.Matches(doc, feature.Obj);
        }
    }

    /// <summary>
    /// Represents a statement (if, for, draw, etc.)
    /// </summary>
    private interface IStatement {
        public void Apply(GeoDocument doc, Ruleset.Feature feature, State state);
    }

    /// <summary>
    /// The define statement alters the state by setting properties.
    /// </summary>
    private readonly struct DefineStatement(IDictionary<string, string> properties) : IStatement {

        private readonly IDictionary<string, string> Properties = properties;

        public void Apply(GeoDocument doc, Ruleset.Feature feature, State state) {
            Properties.ToList().ForEach(p => state.Properties[p.Key] = p.Value);
        }
    }

    /// <summary>
    /// The if statement executes one of its branches based on conditions, or the else statements if no branches were evaluated.
    /// </summary>
    private readonly struct IfStatement(IEnumerable<(ICondition, IEnumerable<IStatement>)> branches, IEnumerable<IStatement> elseStatements) : IStatement {

        private readonly IEnumerable<(ICondition, IEnumerable<IStatement>)> Branches = branches;
        private readonly IEnumerable<IStatement> ElseStatements = elseStatements;

        public void Apply(GeoDocument doc, Ruleset.Feature feature, State state) {
            foreach (var (condition, stmts) in Branches) {
                if (condition.Matches(doc, feature)) {
                    foreach (var stmt in stmts) {
                        stmt.Apply(doc, feature, state);
                    }
                    return;
                }
            }
            // No branch matched, do else statements
            foreach (var stmt in ElseStatements) {
                stmt.Apply(doc, feature, state);
            }
        }
    }

    /// <summary>
    /// Used to exit a target. The target rule will catch this exception.
    /// </summary>
    private class StopException : Exception {}

    /// <summary>
    /// A statement that exists the target by throwing a StopException.
    /// </summary>
    private readonly struct StopStatement : IStatement {

        public StopStatement() {}

        public void Apply(GeoDocument doc, Ruleset.Feature feature, State state) {
            throw new StopException();
        }
    }

    /// <summary>
    /// The draw statement adds a new draw command to the GeoDocument with the currently defined properties.
    /// Draw commands are evaluated based on their importance, which is based on the location of the draw command in the file.
    /// Commands higher in the file are evaluated after commands lowed in the file (i.e. commands higher will be drawn on top of commands lower).
    /// </summary>
    private readonly struct DrawStatement(string type, int importance, ILogger logger) : IStatement {

        private readonly string Type = type;
        private readonly int Importance = importance;
        private readonly ILogger Logger = logger;

        public void Apply(GeoDocument doc, Ruleset.Feature feature, State state) {
            Logger.Debug($"Drawing {feature.Name} {feature.Obj.Id} as {Type} with Importance={Importance}, {string.Join(", ", state.Properties.Select(p => p.Key + "=" + p.Value))}");
            switch (Type) {
            case "fill":
                if (feature.Obj is Area area) {
                    doc.DrawCommands.Add(new DrawFill(state.Properties, Importance, feature.Name, area, isLine: false));
                } else {
                    Logger.Debug($"{feature.Name}: draw:fill is only supported for areas");
                    throw new StopException();
                }
                break;
            case "line":
                if (feature.Obj is Line line) {
                    doc.DrawCommands.Add(new DrawLine(state.Properties, Importance, feature.Name, line));
                } else if (feature.Obj is Area area2) {
                    doc.DrawCommands.Add(new DrawFill(state.Properties, Importance, feature.Name, area2, isLine: true));
                } else {
                    Logger.Debug($"{feature.Name}: draw:line is only supported for lines");
                    throw new StopException();
                }
                break;
            case "shape":
                doc.DrawCommands.Add(new DrawShape(state.Properties, Importance, feature.Name, feature.Obj));
                break;
            case "text":
                doc.DrawCommands.Add(new DrawText(state.Properties, Importance, feature.Name, feature.Obj));
                break;
            case "icon":
                doc.DrawCommands.Add(new DrawIcon(state.Properties, Importance, feature.Name, feature.Obj));
                break;
            case "shield":
                // TODO: implement
                //doc.DrawCommands.Add(new DrawText(state.Properties, Importance, feature.Name, feature.Obj));
                //doc.DrawCommands.Add(new DrawShape(state.Properties, Importance, feature.Name, feature.Obj));
                break;
            }
        }
    }

    /// <summary>
    /// Represents a target declaration. A rule has a condition specified after the "target:" keyword and a list of statements.
    /// </summary>
    private readonly struct Rule : Ruleset.IRule {

        private readonly ICondition Condition;
        private readonly IEnumerable<IStatement> Statements;
        private readonly ILogger Logger;

        public Rule(ICondition cond, IEnumerable<IStatement> statements, ILogger logger) {
            Condition = cond;
            Statements = statements;
            Logger = logger;
        }

        public void Apply(GeoDocument doc, Ruleset.Feature feature, State state) {
            Logger.Debug($"Found target for {feature.Name} {feature.Obj.Id} {(feature.Obj.Tags.TryGetValue("name", out var name) ? name : "")}");
            foreach (var stmt in Statements) {
                try {
                    stmt.Apply(doc, feature, state);
                } catch (StopException) {
                    break;
                }
            }
        }

        public bool Matches(GeoDocument doc, Ruleset.Feature feature) {
            return Condition.Matches(doc, feature);
        }
    }
}