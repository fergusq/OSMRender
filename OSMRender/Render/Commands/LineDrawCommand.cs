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

using OSMRender.Geo;

namespace OSMRender.Render.Commands;

public abstract class LineDrawCommand : DrawCommand, IMergeableLine {

    public List<Point> Nodes { get; } = new();

    public long MergeableLineId => Obj.Id;

    public IDictionary<string, string> MergeableLineProperties => Properties;

    protected LineDrawCommand(IDictionary<string, string> properties, int importance, string feature, GeoObj obj) : base(properties, importance, feature, obj) {
        if (obj is Line line) {
            Nodes.AddRange(line.Nodes);
        }
    }
}