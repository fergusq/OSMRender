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
        Dictionary<long, HashSet<long>> nodeToLine = new();
        Dictionary<long, HashSet<long>> endNodeToLine = new();
        foreach(var line in lines.Values) {
            if (line.Nodes.Count >= 2) {
                foreach (var node in line.Nodes) {
                    if (!nodeToLine.ContainsKey(node.Id)) {
                        nodeToLine[node.Id] = new();
                    }
                    nodeToLine[node.Id].Add(line.Id);
                }

                var startId = line.Nodes[0].Id;
                var endId = line.Nodes[^1].Id;
                foreach (var node in new long[] { startId, endId }) {
                    if (!endNodeToLine.ContainsKey(node)) {
                        endNodeToLine[node] = new();
                    }
                    endNodeToLine[node].Add(line.Id);
                }
            }
        }

        var lineToRef = lines.Values.Select(l => new LineRef() { LineId = l.Id }).ToDictionary(l => l.LineId);

        var nodeToRef = nodeToLine
            .Select(p => (p.Key, Ref: p.Value.Select(l => lineToRef[l])))
            .ToDictionary(p => p.Key, p => p.Ref);

        var endNodeToRef = endNodeToLine
            .Select(p => (p.Key, Ref: p.Value.Select(l => lineToRef[l])))
            .ToDictionary(p => p.Key, p => p.Ref);

        foreach(var node in points.Keys) {
            if (!endNodeToRef.ContainsKey(node)) {
                continue;
            }

            foreach (var endNode in endNodeToRef[node]) {
                var tags = lines[endNode.LineId].Tags;
                List<long> matchingLines = new();
                foreach (var endNode2 in endNodeToRef[node]) {
                    if (endNode2.LineId == endNode.LineId) continue;

                    if (lines[endNode2.LineId].Tags?.Equals(tags) ?? false) {
                        matchingLines.Add(endNode2.LineId);
                    }
                }

                if (matchingLines.Count == 1) {
                    var line1 = lines[endNode.LineId];
                    var line2 = lines[matchingLines[0]];

                    //Console.WriteLine($"Merging {line2.Id} to {line1.Id}");

                    // Change refs
                    lineToRef.Values.ToList().ForEach(r => {
                        if (r.LineId == line2.Id) {
                            r.LineId = line1.Id;
                        }
                    });

                    // Remove fromlines
                    lines.Remove(line2.Id);

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
                        throw new Exception($"cannot merge lines {line1.Id}, {line2.Id}");
                    }
                }
            }
        }
    }

    internal void CombineAdjacentLineDraws() {
        foreach (var feature in DrawCommands.Select(c => c.Feature).ToHashSet()) {
            CombineAdjacentLineDrawsFor(feature);
        }
    }

    // TODO: could these two share code? Ugly repeated code...
    internal void CombineAdjacentLineDrawsFor(string feature) {
        Dictionary<long, LineDrawCommand> lineToDraw = new();
        DrawCommands
            .Where(c => c.Feature == feature && c is LineDrawCommand d && d.Points.Count > 0)
            .Select(c => (LineDrawCommand) c)
            .ToList()
            .ForEach(p => lineToDraw[p.Obj.Id] = p);
        Dictionary<long, HashSet<long>> nodeToLine = new();
        Dictionary<long, HashSet<long>> endNodeToLine = new();
        foreach(var drawCmd in lineToDraw.Values) {
            if (drawCmd.Points.Count >= 2) {
                foreach (var node in drawCmd.Points) {
                    if (!nodeToLine.ContainsKey(node.Id)) {
                        nodeToLine[node.Id] = new();
                    }
                    nodeToLine[node.Id].Add(drawCmd.Obj.Id);
                }

                var startId = drawCmd.Points[0].Id;
                var endId = drawCmd.Points[^1].Id;
                foreach (var node in new long[] { startId, endId }) {
                    if (!endNodeToLine.ContainsKey(node)) {
                        endNodeToLine[node] = new();
                    }
                    endNodeToLine[node].Add(drawCmd.Obj.Id);
                }
            }
        }

        var lineToRef = lineToDraw.Values.Select(l => new LineRef() { LineId = l.Obj.Id }).ToDictionary(l => l.LineId);

        var nodeToRef = nodeToLine
            .Select(p => (p.Key, Ref: p.Value.Select(l => lineToRef[l])))
            .ToDictionary(p => p.Key, p => p.Ref);

        var endNodeToRef = endNodeToLine
            .Select(p => (p.Key, Ref: p.Value.Select(l => lineToRef[l])))
            .ToDictionary(p => p.Key, p => p.Ref);

        foreach(var node in Points.Keys) {
            if (!endNodeToRef.ContainsKey(node)) {
                continue;
            }

            foreach (var endNode in endNodeToRef[node]) {
                var tags = lineToDraw[endNode.LineId].Properties;
                List<long> matchingLines = new();
                foreach (var endNode2 in endNodeToRef[node]) {
                    if (endNode2.LineId == endNode.LineId) continue;

                    if (lineToDraw[endNode2.LineId].Properties.All(p => tags.ContainsKey(p.Key) && tags[p.Key].Equals(p.Value))) {
                        matchingLines.Add(endNode2.LineId);
                    }
                }

                if (matchingLines.Count == 1) {
                    var line1 = lineToDraw[endNode.LineId];
                    var line2 = lineToDraw[matchingLines[0]];

                    //Console.WriteLine($"Merging {line2.Obj.Id} {line2.Feature} to {line1.Obj.Id} {line1.Feature}");

                    // Change refs
                    lineToRef.Values.ToList().ForEach(r => {
                        if (r.LineId == line2.Obj.Id) {
                            r.LineId = line1.Obj.Id;
                        }
                    });

                    // Remove fromlines
                    lineToDraw.Remove(line2.Obj.Id);
                    DrawCommands.Remove(line2);

                    // Merge
                    if (line1.Points[0].Id == line2.Points[0].Id) {
                        line2.Points.Reverse();
                        line1.Points.InsertRange(0, line2.Points);
                    } else if (line1.Points[^1].Id == line2.Points[0].Id) {
                        line1.Points.AddRange(line2.Points);
                    } else if (line1.Points[0].Id == line2.Points[^1].Id) {
                        line1.Points.InsertRange(0, line2.Points);
                    } else if (line1.Points[^1].Id == line2.Points[^1].Id) {
                        line2.Points.Reverse();
                        line1.Points.AddRange(line2.Points);
                    } else {
                        throw new Exception($"cannot merge lines {line1.Obj.Id}, {line2.Obj.Id}");
                    }
                }
            }
        }
    }
}