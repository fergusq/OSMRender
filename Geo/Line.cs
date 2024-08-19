using OsmSharp.Tags;

namespace OSMRender.Geo;

public class Line : GeoObj {

    public List<Point> Nodes { get; set; }

    public override Bounds Bounds => Nodes.Select(n => n.Bounds).Aggregate((a, b) => a.MergeWith(b));

    public Line(long id, TagsCollectionBase tags) : base(id, tags) {
        Nodes = new List<Point>();
    }
}