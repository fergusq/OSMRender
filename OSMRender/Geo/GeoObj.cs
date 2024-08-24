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

using System.Collections.Immutable;

namespace OSMRender.Geo;

/// <summary>
/// Represents a point (node), line (way), area (way/relation), or the background of the map.
/// </summary>
public abstract class GeoObj(long id, IDictionary<string, string> tags) {
    public long Id { get; set; } = id;

    public ImmutableDictionary<string, string> Tags { get; set; } = tags.ToImmutableDictionary();

    public abstract Bounds Bounds { get; }
}