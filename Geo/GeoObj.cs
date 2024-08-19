using OsmSharp.Tags;

namespace OSMRender.Geo;

public abstract class GeoObj {
    public long Id { get; set; }

    public TagsCollectionBase Tags { get; set; }

    protected GeoObj(long id, TagsCollectionBase tags) {
        Id = id;
        Tags = tags;
    }

    public abstract Bounds Bounds { get; }
}