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

    public List<Point> OuterEdge { get; set; }

    public List<List<Point>> InnerEdges { get; set; }

    public override Bounds Bounds => OuterEdge.Select(n => n.Bounds).Aggregate((a, b) => a.MergeWith(b));

    public double MeanLatitude => OuterEdge.Select(p => p.Latitude).Sum() / OuterEdge.Count;
    public double MeanLongitude => OuterEdge.Select(p => p.Longitude).Sum() / OuterEdge.Count;

    public Area(long id, TagsCollectionBase tags) : base(id, tags) {
        OuterEdge = new List<Point>();
        InnerEdges = new List<List<Point>>();
    }

    public IEnumerable<Point> Edge {
        get {
            var edge = new List<Point>();
            edge.AddRange(OuterEdge);
            foreach (var inner in InnerEdges) {
                edge.AddRange(inner);
            }
            return edge;
        }
    }
}