namespace OSMRender.Geo;

internal interface IMergeableLine {
    public List<Point> Nodes { get; }

    public long MergeableLineId { get; }

    public IDictionary<string, string> MergeableLineProperties { get; }
}