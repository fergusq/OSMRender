using OsmSharp.Tags;

namespace OSMRender.Geo;

public class Point : GeoObj {

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public override Bounds Bounds => Bounds.FromPoint(Latitude, Longitude);

    public Point(long id, TagsCollectionBase tags, double lat, double lon) : base(id, tags) {
        Latitude = lat;
        Longitude = lon;
    }
}