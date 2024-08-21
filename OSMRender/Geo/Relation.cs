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