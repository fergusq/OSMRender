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