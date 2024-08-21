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
using VectSharp;

namespace OSMRender.Render.Commands;

public class DrawFill : DrawCommand {
    private readonly Area Area;

    public DrawFill(IDictionary<string, string> properties, int importance, string feature, Area obj) : base(properties, importance, feature, obj) {
        Area = obj;
    }

    public override void Draw(PageRenderer renderer, int layer) {
        // Areas are all drawn in layer 0
        if (layer == Layer) {
            List<GraphicsPath> paths = new();

            // Draw outer edges as separate polygons if there are no inner edges
            if (Area.InnerEdges.Count == 0) {
                foreach (var outer in Area.OuterEdges) {
                    var path = new GraphicsPath();
                    var first = outer.First();
                    path.MoveTo(renderer.LongitudeToX(first.Longitude), renderer.LatitudeToY(first.Latitude));
                    foreach (var node in outer.Skip(1)) {
                        path.LineTo(renderer.LongitudeToX(node.Longitude), renderer.LatitudeToY(node.Latitude));
                    }
                    path.LineTo(renderer.LongitudeToX(first.Longitude), renderer.LatitudeToY(first.Latitude));
                    path.Close();
                    paths.Add(path);
                }
            }

            // Draw everything together (TODO: this is bad, use intersections)
            else {
                var path = new GraphicsPath();
                var first = Area.Edge.First();
                path.MoveTo(renderer.LongitudeToX(first.Longitude), renderer.LatitudeToY(first.Latitude));
                foreach (var node in Area.Edge.Skip(1)) {
                    path.LineTo(renderer.LongitudeToX(node.Longitude), renderer.LatitudeToY(node.Latitude));
                }
                path.LineTo(renderer.LongitudeToX(first.Longitude), renderer.LatitudeToY(first.Latitude));
                paths.Add(path);
            }

            foreach (var path in paths) {
                renderer.Graphics.FillPath(path, GetColour("fill-color"));

                if (GetString("border-style") != "") {
                    StrokePath(path, renderer, "border");
                }
            }
        }
    }

    public override IEnumerable<int> GetLayers() {
        return new int[] { Layer };
    }

    private int Layer => GetLayerCode(
        0,
        0,
        Obj.Tags is not null && Obj.Tags.ContainsKey("layer") ? int.Parse(Obj.Tags.GetValue("layer")) : 0
    );
}