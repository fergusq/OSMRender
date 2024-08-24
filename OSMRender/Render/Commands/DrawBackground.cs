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

namespace OSMRender.Render.Commands;

public class DrawBackground(IDictionary<string, string> properties, int importance, string feature, Background background) : DrawCommand(properties, importance, feature, background) {

    private readonly Background Background = background;

    public override void Draw(PageRenderer renderer, int layer) {
        // Background is drawn in layer -1
        if (layer != Layer) return;
        GraphicsPath path = new();
        path.MoveTo(renderer.LongitudeToX(Background.Bounds.MinLongitude), renderer.LatitudeToY(Background.Bounds.MinLatitude));
        path.LineTo(renderer.LongitudeToX(Background.Bounds.MinLongitude), renderer.LatitudeToY(Background.Bounds.MaxLatitude));
        path.LineTo(renderer.LongitudeToX(Background.Bounds.MaxLatitude), renderer.LatitudeToY(Background.Bounds.MaxLatitude));
        path.LineTo(renderer.LongitudeToX(Background.Bounds.MaxLatitude), renderer.LatitudeToY(Background.Bounds.MinLatitude));
        path.Close();

        renderer.Graphics.FillPath(path, GetColour("map-background-color", "map-background-opacity"));
    }

    public override IEnumerable<int> GetLayers() {
        return [Layer];
    }

    private int Layer => GetLayerCode(
        -1,
        LayerProperty
    );
}