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

using OSMRender.Render.Commands;

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

    public GeoDocument(Dictionary<long, Point> points, Dictionary<long, Area> areas, Dictionary<long, Line> lines, Dictionary<long, Relation> relations) {
        
        // Combines adjacent lines that have the same properties (e.g. adjacent road segments that have the same name) into a single line.
        // This is an important postprocessing step that must be done for OSM data.
        LineMerger.CombineAdjacentFor(points.Keys, lines, line => {});
        
        Points = points;
        Areas = areas;
        Lines = lines;
        Relations = relations;
        DrawCommands = new List<DrawCommand>();
    }

    /// <summary>
    /// Combines adjacent LineDrawsCommands (i.e. roads, shapes) into single line draws. The ruleset will call this method.
    /// </summary>
    internal void CombineAdjacentLineDraws() {
        foreach (var feature in DrawCommands.Select(c => c.Feature).ToHashSet()) {
            Dictionary<long, LineDrawCommand> lineToDraw = new();
            DrawCommands
                .Where(c => c.Feature == feature && c is LineDrawCommand d && d.Nodes.Count > 0)
                .Select(c => (LineDrawCommand) c)
                .ToList()
                .ForEach(p => lineToDraw[p.Obj.Id] = p);
            LineMerger.CombineAdjacentFor(Points.Keys, lineToDraw, l => DrawCommands.Remove(l));
        }
    }
}