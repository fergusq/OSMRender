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
/// A GeoObj corresponding to an OSM way.
/// </summary>
public class Line(long id, IDictionary<string, string> tags) : GeoObj(id, tags), IMergeableLine {

    public List<Point> Nodes { get; set; } = [];

    public long MergeableLineId => Id;

    public IDictionary<string, string> MergeableLineProperties => Tags;

    public override Bounds Bounds => Nodes.Select(n => n.Bounds).Aggregate((a, b) => a.MergeWith(b));
}