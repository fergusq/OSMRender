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

public class DrawFill(IDictionary<string, string> properties, int importance, string feature, Area obj, bool isLine = false) : DrawCommand(properties, importance, feature, obj) {

    public static bool AllowMask { get; set; } = true;

    private readonly Area Area = obj;

    private readonly bool IsLine = isLine;

    public override void Draw(PageRenderer renderer, int layer) {
        // Areas are all drawn in layer 0
        if (layer != Layer) return;

        if (IsLine) {
            List<GraphicsPath> paths = [];
            Area.OuterEdges.ForEach(edge => paths.Add(NodesToPath(renderer, edge)));
            Area.InnerEdges.ForEach(edge => paths.Add(NodesToPath(renderer, edge)));

            foreach (var path in paths) {
                StrokePath(path, renderer, border: false);
            }
        } else if (Area.InnerEdges.Count == 0) {
            foreach (var edge in Area.OuterEdges) {
                renderer.Graphics.FillPath(NodesToPath(renderer, edge), RenderingProperties.GetFillColorFor(renderer.Renderer.ZoomLevel));
            }
        } else if (AllowMask) {
            List<GraphicsPath> paths = [];
            Area.OuterEdges.ForEach(edge => paths.Add(NodesToPath(renderer, edge)));

            // Construct a mask where the background is white (visible) and inner polygons are black (invisible)
            Graphics mask = new();
            var (point, size) = GetPageBounds(renderer);
            mask.FillRectangle(point, size, Colours.White);
            foreach (var inner in Area.InnerEdges) {
                mask.FillPath(NodesToPath(renderer, inner), Colours.Black);
            }

            Graphics output = new();
            foreach (var path in paths) {
                output.FillPath(path, RenderingProperties.GetFillColorFor(renderer.Renderer.ZoomLevel));
            }

            renderer.Graphics.DrawGraphics(0, 0, output, new MaskFilter(mask));
        } else {
            // If masks are disallowed, draw the edge by calculating a new edge that contains the inner edges
            // This requires no rasterization to export PDF files, but might cause graphical issues
            renderer.Graphics.FillPath(NodesToPath(renderer, Area.CalculateEdge()), RenderingProperties.GetFillColorFor(renderer.Renderer.ZoomLevel));
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