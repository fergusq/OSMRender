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