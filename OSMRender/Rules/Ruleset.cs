// This file is part of OSMRender.
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

using OSMRender.Geo;
using OSMRender.Logging;
using OSMRender.Render.Commands;
using OsmSharp.Tags;

namespace OSMRender.Rules;

/// <summary>
/// A ruleset contains feature declarations and target rules. Its Apply method can be used to add draw commands to a GeoDocument.
/// </summary>
public class Ruleset {
    public interface IQuery {
        public bool Matches(GeoDocument doc, GeoObj obj);
    }

    public interface IRule {
        public bool Matches(GeoDocument doc, Feature feature);
        public void Apply(GeoDocument doc, Feature feature, State state);
    }

    public readonly struct Feature {
        public readonly string Name;
        public readonly GeoObj Obj;

        public Feature(string name, GeoObj obj) {
            Name = name;
            Obj = obj;
        }
    }

    public IDictionary<string, IQuery> PointFeatures { get; set; }
    public IDictionary<string, IQuery> LineFeatures { get; set; }
    public IDictionary<string, IQuery> AreaFeatures { get; set; }
    public IDictionary<string, string> Properties { get; set; }
    public IList<IRule> Rules { get; set; }

    private readonly ILogger Logger;

    public Ruleset(ILogger logger) {
        PointFeatures = new Dictionary<string, IQuery>();
        LineFeatures = new Dictionary<string, IQuery>();
        AreaFeatures = new Dictionary<string, IQuery>();
        Properties = new Dictionary<string, string>();
        Rules = new List<IRule>();
        Logger = logger;
    }

    /// <summary>
    /// Constructs the feature database and evaluates all rules for all features, adding draw commands for each evaluated draw statement to the GeoDocument object.
    /// </summary>
    /// <param name="doc">the GeoDocument for which the rules are evaluated and DrawCommands of which are modified</param>
    public void Apply(GeoDocument doc) {
        var features = new List<Feature>();
        foreach (var point in doc.Points) {
            foreach (var query in PointFeatures) {
                if (query.Value.Matches(doc, point.Value)) {
                    Logger.Debug($"Added {point.Value.Id} as feature `{query.Key}'");
                    features.Add(new Feature(query.Key, point.Value));
                }
            }
        }
        foreach (var line in doc.Lines) {
            foreach (var query in LineFeatures) {
                if (query.Value.Matches(doc, line.Value)) {
                    Logger.Debug($"Added {line.Value.Id} as feature `{query.Key}'");
                    features.Add(new Feature(query.Key, line.Value));
                }
            }
        }
        foreach (var area in doc.Areas) {
            foreach (var query in AreaFeatures) {
                if (query.Value.Matches(doc, area.Value)) {
                    Logger.Debug($"Added {area.Value.Id} as feature `{query.Key}'");
                    features.Add(new Feature(query.Key, area.Value));
                }
            }
        }
        foreach (var rule in Rules) {
            foreach (var feature in features) {
                if (rule.Matches(doc, feature)) {
                    rule.Apply(doc, feature, new State(this));
                }
            }
        }
        doc.CombineAdjacentLineDraws();
        doc.DrawCommands.Add(new DrawBackground(new State(this).Properties, 0, "background", new Background(-1, new TagsCollection(), doc.Bounds)));
    }
}