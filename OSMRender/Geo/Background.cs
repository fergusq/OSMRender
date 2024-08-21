using OsmSharp.Tags;

namespace OSMRender.Geo;

public class Background : GeoObj {

    public override Bounds Bounds { get; }

    public Background(long id, TagsCollectionBase tags, Bounds bounds) : base(id, tags) {
        Bounds = bounds;
    }
}