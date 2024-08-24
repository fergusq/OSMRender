// This file is part of OSMRender.
// Copyright (c) 2024 Iikka Hauhio
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
using VectSharp.Filters;

namespace OSMRender.Render.Commands;

public class DrawFill(IDictionary<string, string> properties, int importance, string feature, Area obj) : DrawCommand(properties, importance, feature, obj) {

    private readonly Area Area = obj;

    public override void Draw(PageRenderer renderer, int layer) {
        // Areas are all drawn in layer 0
        if (layer != Layer) return;

        List<GraphicsPath> paths = [];
        foreach (var outer in Area.OuterEdges) {
            paths.Add(NodesToPath(renderer, outer));
        }

        // Construct a mask where the background is white (visible) and inner polygons are black (invisible)
        Graphics mask = new();
        var (point, size) = GetPageBounds(renderer);
        mask.FillRectangle(point, size, Colours.White);
        foreach (var inner in Area.InnerEdges) {
            mask.FillPath(NodesToPath(renderer, inner), Colours.Black);
        }

        Graphics output = new();
        foreach (var path in paths) {
            output.FillPath(path, GetColour("fill-color", "fill-opacity"));
        }

        renderer.Graphics.DrawGraphics(0, 0, output, new MaskFilter(mask));

        foreach (var path in paths) {
            if (GetString("border-style") != "") {
                StrokePath(path, renderer, "border");
            }
        }
    }

    public override IEnumerable<int> GetLayers() {
        return [Layer];
    }

    private int Layer => GetLayerCode(
        0,
        LayerProperty
    );
}