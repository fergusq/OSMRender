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

namespace OSMRender.Geo;

public readonly struct Bounds {
    public readonly double MinLatitude;
    public readonly double MaxLatitude;
    public readonly double MinLongitude;
    public readonly double MaxLongitude;

    private Bounds(double minLat, double maxLat, double minLon, double maxLon) {
        MinLatitude = minLat;
        MaxLatitude = maxLat;
        MinLongitude = minLon;
        MaxLongitude = maxLon;
    }

    public static Bounds From(double minLat, double maxLat, double minLon, double maxLon) {
        return new Bounds(minLat, maxLat, minLon, maxLon);
    }

    public static Bounds FromPoint(double latitude, double longitude) {
        return new Bounds(latitude, latitude, longitude, longitude);
    }

    public static Bounds FromOsmBounds(OsmSharp.API.Bounds bounds) {
        return new Bounds(bounds.MinLatitude ?? 0f, bounds.MaxLatitude ?? 0f, bounds.MinLongitude ?? 0f, bounds.MaxLongitude ?? 0f);
    }

    public Bounds MergeWith(Bounds other) {
        return new Bounds(
            Math.Min(MinLatitude, other.MinLatitude),
            Math.Max(MaxLatitude, other.MaxLatitude),
            Math.Min(MinLongitude, other.MinLongitude),
            Math.Max(MaxLongitude, other.MaxLongitude)
        );
    }

    public bool Overlaps(Bounds other) {
        return MinLongitude < other.MaxLongitude && MaxLongitude > other.MinLongitude && MinLatitude < other.MaxLatitude && MaxLatitude > other.MinLatitude;
    }

    public Bounds Extend(double amount) {
        double latAmount = (MaxLatitude - MinLatitude) * (amount - 1);
        double lonAmount = (MaxLongitude - MinLongitude) * (amount - 1);
        return new Bounds(
            MinLatitude - latAmount,
            MaxLatitude + latAmount,
            MinLongitude - lonAmount,
            MaxLongitude + lonAmount
        );
    }

    public override string ToString()
    {
        return $"Bounds({MinLatitude}, {MaxLatitude}, {MinLongitude}, {MaxLongitude})";
    }
}