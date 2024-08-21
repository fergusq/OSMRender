using OSMRender.Geo;
using OSMRender.Render.Commands;
using OsmSharp.Tags;

namespace OSMRender.Rules;

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

    public Ruleset() {
        PointFeatures = new Dictionary<string, IQuery>();
        LineFeatures = new Dictionary<string, IQuery>();
        AreaFeatures = new Dictionary<string, IQuery>();
        Properties = new Dictionary<string, string>();
        Rules = new List<IRule>();
    }

    public void Apply(GeoDocument doc) {
        var features = new List<Feature>();
        foreach (var point in doc.Points) {
            foreach (var query in PointFeatures) {
                if (query.Value.Matches(doc, point.Value)) {
                    features.Add(new Feature(query.Key, point.Value));
                }
            }
        }
        foreach (var line in doc.Lines) {
            foreach (var query in LineFeatures) {
                if (query.Value.Matches(doc, line.Value)) {
                    features.Add(new Feature(query.Key, line.Value));
                }
            }
        }
        foreach (var area in doc.Areas) {
            foreach (var query in AreaFeatures) {
                if (query.Value.Matches(doc, area.Value)) {
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