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

using OsmSharp.Tags;

namespace OSMRender.Geo;

public class Area : GeoObj {

    public List<List<Point>> OuterEdges { get; set; }

    public List<List<Point>> InnerEdges { get; set; }

    public override Bounds Bounds => OuterEdges.SelectMany(e => e).Select(n => n.Bounds).Aggregate((a, b) => a.MergeWith(b));

    public double MeanLatitude => OuterEdges.SelectMany(e => e).Select(p => p.Latitude).Sum() / OuterEdges.SelectMany(e => e).Count();
    public double MeanLongitude => OuterEdges.SelectMany(e => e).Select(p => p.Longitude).Sum() / OuterEdges.SelectMany(e => e).Count();

    public Area(long id, TagsCollectionBase tags) : base(id, tags) {
        OuterEdges = new();
        InnerEdges = new();
    }

    public IEnumerable<Point> Edge {
        get {
            var edge = new List<Point>();
            foreach (var outer in OuterEdges) {
                edge.AddRange(outer);
            }
            foreach (var inner in InnerEdges) {
                edge.AddRange(inner);
            }
            return edge;
        }
    }
}