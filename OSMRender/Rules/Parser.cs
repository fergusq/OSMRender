using System.Text.RegularExpressions;
using OSMRender.Geo;
using OSMRender.Logging;
using OSMRender.Render.Commands;

namespace OSMRender.Rules;

public class Parser {

    public class SyntaxErrorException : Exception {
        internal SyntaxErrorException(string message) : base(message) {}
    }

    private static bool TryEat<T>(IList<T> tokens, params T[] token) where T : IEquatable<T> {
        if (tokens.Count > 0 && (token.Length == 0 || token.Any(t => tokens[0].Equals(t)))) {
            //Console.WriteLine($"Eating {tokens[0]}");
            tokens.RemoveAt(0);
            return true;
        }
        return false;
    }

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

    private static List<string> ParseLines(string rules) {
        var lines = rules.Split("\n");
        var outLines = new List<string>();
        var prevIndent = 0;
        var indentLevel = new List<int>() { 0 };
        foreach (var line in lines) {
            var lineTrimmed = line.TrimEnd();
            var indent = 0;
            while (lineTrimmed.StartsWith("\t")) {
                indent++;
                lineTrimmed = lineTrimmed[1..];
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
                while (indentLevel[^1] > indent) {
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

    public static Ruleset ParseRules(string ruleString, Logger? logger = null) {
        var lines = ParseLines(ruleString);
        var rules = new Ruleset();
        var parser = new Parser(lines, rules, logger ?? new DummyLogger());
        parser.Parse();
        return rules;
    }

    private readonly List<string> Lines;
    private readonly Ruleset Rules;
    private readonly Logger Logger;

    private Parser(List<string> lines, Ruleset rules, Logger logger) {
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
            line = null;
            return false;
        } else if (Lines[0] == "<indent>" || Lines[0] == "<deindent>") {
            line = null;
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
        while (regex.IsMatch(code[pos..])) {
            var match = regex.Match(code[pos..]);
            ans.Add(match.Value);
            //Console.WriteLine($"Tokenized `{pattern}' `{match.Value}'");
            pos += match.Length;
        }
        if (pos != code.Length) {
            throw new SyntaxErrorException("cannot tokenize `" + code[pos..] + "'");
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
        var featureTypes = NextLine().Split(",").Select(f => f.Trim());
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
        var name = line[..colon].Trim();
        var queryCode = line[(colon + 1)..].Trim();
        var query = ParseQueryCode(queryCode);

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

    private Ruleset.IQuery ParseQueryCode(string queryCode) {
        var tokens = TokenizeWithRegex(@"^(""[^""]*""|[\w@:\-]+|[=.,()[\] ])", queryCode).Where(s => s.Trim().Length > 0).ToList();
        var query = ParseQuery(tokens);
        return query;
    }

    private Ruleset.IQuery ParseQuery(List<string> tokens) {
        var left = ParseAndQuery(tokens);
        while (TryEat(tokens, "OR", "or")) {
            var right = ParseAndQuery(tokens);
            left = new BoolOpQuery(BoolOpQuery.Op.Or, left, right);
        }
        return left;
    }

    private Ruleset.IQuery ParseAndQuery(List<string> tokens) {
        var left = ParseNestedQuery(tokens);
        while (TryEat(tokens, "AND", "and")) {
            var right = ParseNestedQuery(tokens);
            left = new BoolOpQuery(BoolOpQuery.Op.And, left, right);
        }
        return left;
    }

    private Ruleset.IQuery ParseNestedQuery(List<string> tokens) {
        var left = ParsePrimitiveQuery(tokens);
        while (TryEat(tokens, ".")) {
            var right = ParsePrimitiveQuery(tokens);
            left = right; // TODO: not implemented
        }
        return left;
    }

    private Ruleset.IQuery ParsePrimitiveQuery(List<string> tokens) {
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
        } else if (TryEat(tokens, "node")) {
            Ruleset.IQuery? subquery = null;
            if (TryEat(tokens, "[")) {
                if (tokens.Count > 0 && tokens[0] != "]") {
                    subquery = ParseQuery(tokens);
                }
                Eat(tokens, "]");
            }
            return new IsNodeQuery(subquery);
        } else if (TryEat(tokens, "area")) {
            Ruleset.IQuery? subquery = null;
            if (TryEat(tokens, "[")) {
                if (tokens.Count > 0 && tokens[0] != "]") {
                    subquery = ParseQuery(tokens);
                }
                Eat(tokens, "]");
            }
            return new IsAreaQuery(subquery);
        } else if (TryEat(tokens, "relation")) {
            Ruleset.IQuery? subquery = null;
            if (TryEat(tokens, "[")) {
                if (tokens.Count > 0 && tokens[0] != "]") {
                    subquery = ParseQuery(tokens);
                }
                Eat(tokens, "]");
            }
            return new IsRelationQuery(subquery);
        } else if (TryEat(tokens, "gpstrack")) {
            Ruleset.IQuery? subquery = null;
            if (TryEat(tokens, "[")) {
                if (tokens.Count > 0 && tokens[0] != "]") {
                    subquery = ParseQuery(tokens);
                }
                Eat(tokens, "]");
            }
            return new NullQuery();
        } else if (TryEat(tokens, "gpsroute")) {
            Ruleset.IQuery? subquery = null;
            if (TryEat(tokens, "[")) {
                if (tokens.Count > 0 && tokens[0] != "]") {
                    subquery = ParseQuery(tokens);
                }
                Eat(tokens, "]");
            }
            return new NullQuery();
        } else if (TryEat(tokens, "gpswaypoint")) {
            Ruleset.IQuery? subquery = null;
            if (TryEat(tokens, "[")) {
                if (tokens.Count > 0 && tokens[0] != "]") {
                    subquery = ParseQuery(tokens);
                }
                Eat(tokens, "]");
            }
            return new NullQuery();
        } else if (TryEat(tokens, "contour")) {
            Ruleset.IQuery? subquery = null;
            if (TryEat(tokens, "[")) {
                if (tokens.Count > 0 && tokens[0] != "]") {
                    subquery = ParseQuery(tokens);
                }
                Eat(tokens, "]");
            }
            return new NullQuery();
        } else if (TryEat(tokens, "(")) {
            var subquery = ParseQuery(tokens);
            Eat(tokens, ")");
            return subquery;
        } else if (TryEat(tokens, "[")) {
            var subquery = ParseQuery(tokens);
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
                    val = val[1..^1];
                }
                return new TagQuery(tag, new string[] { val });
            } else {
                return new TagExistsQuery(tag);
            }
        }
    }

    private readonly struct TagQuery : Ruleset.IQuery {
        private readonly string Key;
        private readonly IEnumerable<string> Val;

        public TagQuery(string key, IEnumerable<string> val) {
            Key = key;
            Val = val;
        }

        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            return obj.Tags is not null && obj.Tags.ContainsKey(Key) && Val.Contains(obj.Tags.GetValue(Key));
        }
    }

    private readonly struct TagExistsQuery : Ruleset.IQuery {
        private readonly string Key;

        public TagExistsQuery(string key) {
            Key = key;
        }

        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            return obj.Tags is not null && obj.Tags.ContainsKey(Key);
        }
    }

    private readonly struct IsMultiQuery : Ruleset.IQuery {
        private readonly string Key;
        private readonly int Num;

        public IsMultiQuery(string key, int num) {
            Key = key;
            Num = num;
        }

        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            return obj.Tags is not null && obj.Tags.ContainsKey(Key) && int.Parse(obj.Tags.GetValue(Key)) % Num == 0;
        }
    }

    private readonly struct BoolOpQuery : Ruleset.IQuery {
        public enum Op {
            Or,
            And,
        }
        private readonly Op Operation;
        private readonly Ruleset.IQuery[] Queries;

        public BoolOpQuery(Op operation, params Ruleset.IQuery[] queries) {
            Operation = operation;
            Queries = queries;
        }

        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            return Operation == Op.Or ? Queries.Any(q => q.Matches(doc, obj)) : Queries.All(q => q.Matches(doc, obj));
        }
    }

    private readonly struct IsNodeQuery : Ruleset.IQuery {
        private readonly Ruleset.IQuery? Subquery;

        public IsNodeQuery(Ruleset.IQuery? subquery) {
            Subquery = subquery;
        }

        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            return doc.Points.ContainsKey(obj.Id) && (Subquery is null || Subquery.Matches(doc, obj));
        }
    }

    private readonly struct IsLineQuery : Ruleset.IQuery {
        private readonly Ruleset.IQuery? Subquery;

        public IsLineQuery(Ruleset.IQuery? subquery) {
            Subquery = subquery;
        }

        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            return doc.Lines.ContainsKey(obj.Id) && (Subquery is null || Subquery.Matches(doc, obj));
        }
    }

    private readonly struct IsAreaQuery : Ruleset.IQuery {
        private readonly Ruleset.IQuery? Subquery;

        public IsAreaQuery(Ruleset.IQuery? subquery) {
            Subquery = subquery;
        }

        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            return doc.Areas.ContainsKey(obj.Id) && (Subquery is null || Subquery.Matches(doc, obj));
        }
    }

    private readonly struct IsRelationQuery : Ruleset.IQuery {
        private readonly Ruleset.IQuery? Subquery;

        public IsRelationQuery(Ruleset.IQuery? subquery) {
            Subquery = subquery;
        }

        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            return doc.Relations.ContainsKey(obj.Id) && (Subquery is null || Subquery.Matches(doc, obj));
        }
    }

    private readonly struct NotQuery : Ruleset.IQuery {
        private readonly Ruleset.IQuery Subquery;

        public NotQuery(Ruleset.IQuery subquery) {
            Subquery = subquery;
        }

        public readonly bool Matches(GeoDocument doc, GeoObj obj)
        {
            return !Subquery.Matches(doc, obj);
        }
    }

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

            var key = line[..line.IndexOf(':')].Trim();
            var value = line[(line.IndexOf(':')+1)..].Trim();
            ans[key] = value;
        }
        return ans;
    }

    private (string, string) ParseStatementWithArg() {
        var line = NextLine();
        if (!line.Contains(':')) {
            throw new SyntaxErrorException($"Invalid statement `{line}', missing `:'");
        }
        var key = line[..line.IndexOf(':')].Trim();
        var val = line[(line.IndexOf(':')+1)..].Trim();
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
            value = null;
            return false;
        }
        var key = Lines[0][..Lines[0].IndexOf(':')].Trim();
        var val = Lines[0][(Lines[0].IndexOf(':')+1)..].Trim();
        if (key != expectedKey) {
            value = null;
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
            var pattern = cond["$regex(\"".Length..^2];
            return new RegexCondition(new Regex(pattern));
        } else if (cond.StartsWith("$")) {
            throw new SyntaxErrorException($"Invalid target: `{cond}'");
        } else {
            cond = cond.Replace("*", ".*"); // TODO purkkaa!
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
                var query = ParseQueryCode(val);
                var stmts = ParseStatements();
                return new IfStatement(
                    new (ICondition, IEnumerable<IStatement>)[] { (new QueryCondition(query), stmts) },
                    Array.Empty<IStatement>()
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
                    elseStmts = new List<IStatement>();
                }
                return new IfStatement(branches, elseStmts);
            } else {
                throw new SyntaxErrorException($"Invalid command `{key}' : `{val}'");
            }
        }
    }

    private interface ICondition {
        public bool Matches(GeoDocument doc, Ruleset.Feature feature);
    }

    private readonly struct TrueCondition : ICondition {
        public bool Matches(GeoDocument doc, Ruleset.Feature feature) {
            return true;
        }
    }

    private readonly struct RegexCondition : ICondition {
        private readonly Regex Regex;

        public RegexCondition(Regex regex) {
            Regex = regex;
        }

        public bool Matches(GeoDocument doc, Ruleset.Feature feature) {
            return Regex.IsMatch(feature.Name);
        }
    }

    private readonly struct QueryCondition : ICondition {
        private readonly Ruleset.IQuery Query;

        public QueryCondition(Ruleset.IQuery query) {
            Query = query;
        }

        public bool Matches(GeoDocument doc, Ruleset.Feature feature) {
            return Query.Matches(doc, feature.Obj);
        }
    }

    private interface IStatement {
        public void Apply(GeoDocument doc, Ruleset.Feature feature, State state);
    }

    private readonly struct DefineStatement : IStatement {
        private readonly IDictionary<string, string> Properties;

        public DefineStatement(IDictionary<string, string> properties) {
            Properties = properties;
        }

        public void Apply(GeoDocument doc, Ruleset.Feature feature, State state) {
            Properties.ToList().ForEach(p => state.Properties[p.Key] = p.Value);
        }
    }

    private readonly struct IfStatement : IStatement {
        private readonly IEnumerable<(ICondition, IEnumerable<IStatement>)> Branches;
        private readonly IEnumerable<IStatement> ElseStatements;

        public IfStatement(IEnumerable<(ICondition, IEnumerable<IStatement>)> branches, IEnumerable<IStatement> elseStatements) {
            Branches = branches;
            ElseStatements = elseStatements;
        }

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

    private class StopException : Exception {}

    private readonly struct StopStatement : IStatement {

        public StopStatement() {}

        public void Apply(GeoDocument doc, Ruleset.Feature feature, State state) {
            throw new StopException();
        }
    }

    private readonly struct DrawStatement : IStatement {
        private readonly string Type;
        private readonly int Importance;
        private readonly Logger Logger;

        public DrawStatement(string type, int importance, Logger logger) {
            Type = type;
            Importance = importance;
            Logger = logger;
        }

        public void Apply(GeoDocument doc, Ruleset.Feature feature, State state) {
            Logger.Debug($"Drawing {feature.Name} {feature.Obj.Id} as {Type} with {string.Join(", ", state.Properties.Select(p => p.Key + "=" + p.Value))}");
            switch (Type) {
            case "fill":
                if (feature.Obj is not Area) {
                    Logger.Warning($"draw:fill is only supported for areas, not for {feature.Name}");
                    throw new StopException();
                }
                doc.DrawCommands.Add(new DrawFill(state.Properties, Importance, feature.Name, (Area) feature.Obj));
                break;
            case "line":
                if (feature.Obj is not Line) {
                    Logger.Warning($"draw:line is only supported for lines, not for {feature.Name}");
                    throw new StopException();
                }
                doc.DrawCommands.Add(new DrawLine(state.Properties, Importance, feature.Name, (Line) feature.Obj));
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
                doc.DrawCommands.Add(new DrawText(state.Properties, Importance, feature.Name, feature.Obj));
                doc.DrawCommands.Add(new DrawShape(state.Properties, Importance, feature.Name, feature.Obj));
                break;
            }
        }
    }

    private readonly struct Rule : Ruleset.IRule
    {
        private readonly ICondition Condition;
        private readonly IEnumerable<IStatement> Statements;
        private readonly Logger Logger;

        public Rule(ICondition cond, IEnumerable<IStatement> statements, Logger logger) {
            Condition = cond;
            Statements = statements;
            Logger = logger;
        }

        public void Apply(GeoDocument doc, Ruleset.Feature feature, State state) {
            Logger.Debug($"Found target for {feature.Name} {feature.Obj.Id} {feature.Obj.Tags.GetValue("name")}");
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