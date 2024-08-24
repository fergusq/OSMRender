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

namespace OSMRender.Geo;

/// <summary>
/// An area is an OSM way or multipolygon relation. If it's a way, its first and last nodes must be the same.
/// </summary>
public class Area : GeoObj {

    public List<List<Point>> OuterEdges { get; set; }

    public List<List<Point>> InnerEdges { get; set; }

    public override Bounds Bounds => OuterEdges.SelectMany(e => e).Select(n => n.Bounds).Aggregate((a, b) => a.MergeWith(b));

    public double MeanLatitude => OuterEdges.SelectMany(e => e).Select(p => p.Latitude).Sum() / OuterEdges.SelectMany(e => e).Count();
    public double MeanLongitude => OuterEdges.SelectMany(e => e).Select(p => p.Longitude).Sum() / OuterEdges.SelectMany(e => e).Count();

    public Area(long id, IDictionary<string, string> tags) : base(id, tags) {
        OuterEdges = [];
        InnerEdges = [];
    }

    private static bool IsClockwise(IList<Point> points) {
        var n = points.Count;
        if (points.First().Id == points.Last().Id) {
            n--;
        }
        var sum = 0.0;
        for (int i = 0; i < n-1; i++) {
            sum += (points[i+1].Longitude - points[i].Longitude) * (points[i+1].Latitude + points[i].Latitude);
        }
        return sum > 0;
    }
}