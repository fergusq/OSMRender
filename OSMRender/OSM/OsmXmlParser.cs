using System.Xml.Linq;
using OSMRender.Geo;
using OSMRender.Logging;
using OSMRender.Utils;

namespace OSMRender.OSM;

public static class OsmXmlParser {
    public static GeoDocument XmlToGeoDocument(XDocument xml, ILogger? logger = null) {
        Dictionary<long, Point> points = [];
        Dictionary<long, Line> lines = [];
        Dictionary<long, Area> areas = [];
        Dictionary<long, Relation> relations = [];

        foreach (var node in xml.Root.Descendants("node")) {
            var id = node.Attribute("id").Value.ParseInvariantLong();
            var lat = node.Attribute("lat").Value.ParseInvariantDouble();
            var lon = node.Attribute("lon").Value.ParseInvariantDouble();
            var tags = ParseTags(node);
            points[id] = new Point(id, tags, lat, lon);
        }

        foreach (var way in xml.Root.Descendants("way")) {
            var id = way.Attribute("id").Value.ParseInvariantLong();
            var tags = ParseTags(way);
            var nodes = way.Descendants("nd")
                .Select(nd => nd.Attribute("ref").Value.ParseInvariantLong())
                .Where(id => CheckExists("node", points, id, logger))
                .Select(id => points[id]);
            lines[id] = new Line(id, tags) { Nodes = nodes.ToList() };
            if (lines[id].MayBeArea) {
                areas[id] = new Area(id, tags) { OuterEdges = [nodes.ToList()] };
            }
        }

        foreach (var relation in xml.Root.Descendants("relation")) {
            var id = relation.Attribute("id").Value.ParseInvariantLong();
            var tags = ParseTags(relation);
            var members = relation.Descendants("member")
                .Select(member => (
                    type: member.Attribute("type").Value,
                    reference: member.Attribute("ref").Value.ParseInvariantLong(),
                    role: member.Attribute("role")?.Value ?? ""
                ))
                .Where(member => member.type switch {
                    "node" => CheckExists("node", points, member.reference, logger),
                    "way" => CheckExists("way", lines, member.reference, logger),
                    "relation" => CheckExists("relation", relations, member.reference, logger),
                    _ => false
                })
                .Select(member => new Relation.Member() {
                    Role = member.role,
                    Value = member.type switch {
                        "node" => points[member.reference],
                        "way" => lines[member.reference],
                        "relation" => relations[member.reference],
                        _ => throw new Exception("cannot parse member")
                    }
                } );
            relations[id] = new Relation(id, tags) /*{ Members = members.ToList() }*/;
            if (tags.ContainsKey("type") && tags["type"] == "multipolygon") {
                var edges = members.Where(m => m.Value is Line line && line.MayBeArea);
                var outerEdges = edges.Where(m => m.Role == "outer");
                var innerEdges = edges.Where(m => m.Role == "inner");
                if (outerEdges.Count() > 0) {
                    areas[id] = new Area(id, tags) {
                        OuterEdges = outerEdges.Select(m => new List<Point>(((Line) m.Value).Nodes)).ToList(),
                        InnerEdges = innerEdges.Select(m => new List<Point>(((Line) m.Value).Nodes)).ToList()
                    };
                }
            }
        }

        var bounds = xml.Root.Descendants("bounds")
            .Select(b => Bounds.From(
                minLat: b.Attribute("minlat").Value.ParseInvariantDouble(),
                minLon: b.Attribute("minlon").Value.ParseInvariantDouble(),
                maxLat: b.Attribute("maxlat").Value.ParseInvariantDouble(),
                maxLon: b.Attribute("maxlon").Value.ParseInvariantDouble()
            ))
            .First();

        return new GeoDocument(points, areas, lines, relations, bounds);
    }

    private static Dictionary<string, string> ParseTags(XElement element) {
        return element.Descendants("tag").ToDictionary(t => t.Attribute("k").Value, t => t.Attribute("v").Value);
    }

    private static bool CheckExists<T>(string type, Dictionary<long, T> objs, long id, ILogger? logger) {
        bool contains = objs.ContainsKey(id);
        if (!contains) {
            logger?.Warning($"Missing {type} {id}");
        }
        return contains;
    }
}