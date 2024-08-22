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

using OSMRender.Render.Commands;
using OsmSharp.API;
using OsmSharp.Tags;

namespace OSMRender.Geo;

/// <summary>
/// Represents an OSM document. Contains all points, lines, areas, relations, etc. contained within the document.
/// It also contains DrawCommands associated with the map features.
/// Initially, there are no draw commands; they must be added by applying a Ruleset to the GeoDocument.
/// </summary>
public class GeoDocument {
    public IDictionary<long, Point> Points { get; set; }
    public IDictionary<long, Area> Areas { get; set; }
    public IDictionary<long, Line> Lines { get; set; }
    public IDictionary<long, Relation> Relations { get; set; }
    public IList<DrawCommand> DrawCommands { get; set; }

    public Bounds Bounds => Points.Select(p => p.Value.Bounds).Aggregate((a, b) => a.MergeWith(b));

    private GeoDocument() {
        Points = new Dictionary<long, Point>();
        Areas = new Dictionary<long, Area>();
        Lines = new Dictionary<long, Line>();
        Relations = new Dictionary<long, Relation>();
        DrawCommands = new List<DrawCommand>();
    }

    /// <summary>
    /// Creates a new GeoDocument from OSM data. Adds nodes as Points, ways as Lines and Areas, and relations as Areas or Relations.
    /// If a way has the same start and end point, it is added both as a Line and an Area. Similarly, multipolygon relations are added both as Relations and Areas.
    /// </summary>
    /// <param name="source">the OSM data</param>
    /// <returns>the GeoDocument</returns>
    public static GeoDocument FromOSM(Osm source) {

        Dictionary<long, Point> points = new();
        Dictionary<long, Area> areas = new();
        Dictionary<long, Line> lines = new();
        Dictionary<long, Relation> relations = new();

        long maxPointId = 0;
        foreach (var node in source.Nodes) {
            if (node is null) continue;
            points[node.Id ?? 0] = new Point(node.Id ?? 0, node.Tags, node.Latitude ?? 0, node.Longitude ?? 0);
            if ((node.Id ?? 0) > maxPointId) {
                maxPointId = node.Id ?? 0;
            }
        }

        var bounds = points.Select(p => p.Value.Bounds).Aggregate((a, b) => a.MergeWith(b));

        long maxWayId = 0;
        foreach (var way in source.Ways) {
            if (way is null) continue;
            lines[way.Id ?? 0] = new Line(way.Id ?? 0, way.Tags);
            foreach (var nodeRef in way.Nodes) {
                lines[way.Id ?? 0].Nodes.Add(points[nodeRef]);
            }

            if (way.Nodes[0] == way.Nodes[^1]) {
                // Add also as an area
                var area = new Area(way.Id ?? 0, way.Tags);
                area.OuterEdges.Add(new());
                foreach (var nodeRef in way.Nodes) {
                    area.OuterEdges[0].Add(points[nodeRef]);
                }
                areas[area.Id] = area;
            }

            if ((way.Id ?? 0) > maxWayId) {
                maxWayId = way.Id ?? 0;
            }
        }

        foreach (var relation in source.Relations) {
            if (relation is null) continue;
            relations[relation.Id ?? 0] = new Relation(relation.Id ?? 0, relation.Tags);
            /*foreach (var member in relation.Members) {
                relations[relation.Id ?? 0].Members.Add(new Relation.Member {
                    Role = member.Role,
                    Value = member.Type == OsmSharp.OsmGeoType.Node ? points[member.Id] :
                        member.Type == OsmSharp.OsmGeoType.Way ? lines[member.Id] :
                        relations[member.Id],
                });
            }*/

            // Add also as an area
            if (relation.Tags.Contains(new Tag("type", "multipolygon"))) {
                var area = new Area(relation.Id ?? 0, relation.Tags);
                foreach (var member in relation.Members) {
                    if (!lines.ContainsKey(member.Id)) {
                        continue;
                    }
                    if (member.Role == "outer") {
                        area.OuterEdges.Add(lines[member.Id].Nodes);
                    } else if (member.Role == "inner") {
                        area.InnerEdges.Add(lines[member.Id].Nodes);
                    }
                }
                if (area.OuterEdges.Count > 0) {
                    areas[area.Id] = area;
                }
            }
        }

        CombineAdjacentLines(points, lines);

        var doc = new GeoDocument
        {
            Points = points,
            Lines = lines,
            Areas = areas,
            Relations = relations
        };

        return doc;
    }

    private class LineRef {
        public long LineId;
    }

    private static void CombineAdjacentLines(Dictionary<long, Point> points, Dictionary<long, Line> lines) {
        CombineAdjacentFor(points.Keys, lines, line => {});
    }

    internal void CombineAdjacentLineDraws() {
        foreach (var feature in DrawCommands.Select(c => c.Feature).ToHashSet()) {
            Dictionary<long, LineDrawCommand> lineToDraw = new();
            DrawCommands
                .Where(c => c.Feature == feature && c is LineDrawCommand d && d.Nodes.Count > 0)
                .Select(c => (LineDrawCommand) c)
                .ToList()
                .ForEach(p => lineToDraw[p.Obj.Id] = p);
            CombineAdjacentFor(Points.Keys, lineToDraw, l => DrawCommands.Remove(l));
        }
    }

    /// <summary>
    /// Merges lines by joining two adjacents lines together if they share the same properties.
    /// </summary>
    /// <typeparam name="T">the type of line being merged, must implement IMergeableLine</typeparam>
    /// <param name="points">points which the lines are made of</param>
    /// <param name="lineToDraw">a mapping from line id to line</param>
    /// <param name="remove">an action used to remove lines that were merged</param>
    /// <exception cref="Exception">only in case of bugs</exception>
    private static void CombineAdjacentFor<T>(IEnumerable<long> points, Dictionary<long, T> lineToDraw, Action<T> remove) where T : IMergeableLine {
        Dictionary<long, HashSet<long>> nodeToLine = new();
        Dictionary<long, HashSet<long>> endNodeToLine = new();
        foreach(var drawCmd in lineToDraw.Values) {
            if (drawCmd.Nodes.Count >= 2) {
                foreach (var node in drawCmd.Nodes) {
                    if (!nodeToLine.ContainsKey(node.Id)) {
                        nodeToLine[node.Id] = new();
                    }
                    nodeToLine[node.Id].Add(drawCmd.MergeableLineId);
                }

                var startId = drawCmd.Nodes[0].Id;
                var endId = drawCmd.Nodes[^1].Id;
                foreach (var node in new long[] { startId, endId }) {
                    if (!endNodeToLine.ContainsKey(node)) {
                        endNodeToLine[node] = new();
                    }
                    endNodeToLine[node].Add(drawCmd.MergeableLineId);
                }
            }
        }

        var lineToRef = lineToDraw.Values.Select(l => new LineRef() { LineId = l.MergeableLineId }).ToDictionary(l => l.LineId);

        var nodeToRef = nodeToLine
            .Select(p => (p.Key, Ref: p.Value.Select(l => lineToRef[l])))
            .ToDictionary(p => p.Key, p => p.Ref);

        var endNodeToRef = endNodeToLine
            .Select(p => (p.Key, Ref: p.Value.Select(l => lineToRef[l])))
            .ToDictionary(p => p.Key, p => p.Ref);

        foreach(var node in points) {
            if (!endNodeToRef.ContainsKey(node)) {
                continue;
            }

            foreach (var endNode in endNodeToRef[node]) {
                var tags = lineToDraw[endNode.LineId].MergeableLineProperties;
                List<long> matchingLines = new();
                foreach (var endNode2 in endNodeToRef[node]) {
                    if (endNode2.LineId == endNode.LineId) continue;

                    if (lineToDraw[endNode2.LineId].MergeableLineProperties.All(p => tags.ContainsKey(p.Key) && tags[p.Key].Equals(p.Value))) {
                        matchingLines.Add(endNode2.LineId);
                    }
                }

                if (matchingLines.Count == 1) {
                    var line1 = lineToDraw[endNode.LineId];
                    var line2 = lineToDraw[matchingLines[0]];

                    //Console.WriteLine($"Merging {line2.MergeableLineId} {line2.Feature} to {line1.MergeableLineId} {line1.Feature}");

                    // Change refs
                    lineToRef.Values.ToList().ForEach(r => {
                        if (r.LineId == line2.MergeableLineId) {
                            r.LineId = line1.MergeableLineId;
                        }
                    });

                    // Remove fromlines
                    lineToDraw.Remove(line2.MergeableLineId);
                    remove.Invoke(line2);

                    // Merge
                    if (line1.Nodes[0].Id == line2.Nodes[0].Id) {
                        line2.Nodes.Reverse();
                        line1.Nodes.InsertRange(0, line2.Nodes);
                    } else if (line1.Nodes[^1].Id == line2.Nodes[0].Id) {
                        line1.Nodes.AddRange(line2.Nodes);
                    } else if (line1.Nodes[0].Id == line2.Nodes[^1].Id) {
                        line1.Nodes.InsertRange(0, line2.Nodes);
                    } else if (line1.Nodes[^1].Id == line2.Nodes[^1].Id) {
                        line2.Nodes.Reverse();
                        line1.Nodes.AddRange(line2.Nodes);
                    } else {
                        throw new Exception($"cannot merge lines {line1.MergeableLineId}, {line2.MergeableLineId}");
                    }
                }
            }
        }
    }
}