using OsmSharp.Tags;

namespace OSMRender.Geo;

public class Relation : GeoObj {

    public struct Member {
        public string Role;
        public GeoObj Value;
    }

    public IList<Member> Members { get; set; }

    public override Bounds Bounds => Members.Select(n => n.Value.Bounds).Aggregate((a, b) => a.MergeWith(b));

    public Relation(long id, TagsCollectionBase tags) : base(id, tags) {
        Members = new List<Member>();
    }
}