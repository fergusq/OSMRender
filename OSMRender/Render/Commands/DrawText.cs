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

public class DrawText : LineDrawCommand {

    private readonly static double LINE_TEXT_INTERVAL = 400;

    internal bool Disabled = false;

    internal readonly string Text = "";

    public DrawText(IDictionary<string, string> properties, int importance, string feature, GeoObj obj) : base(properties, importance, feature, obj) {
        if (Properties.TryGetValue("text", out var key) && key is not null) {
            if (Obj.Tags.ContainsKey(key)) {
                Text = Obj.Tags[key];
            } else {
                Disabled = true; // TODO we should parse the expression with [[addr:housenum]] and @if() etc.
            }
        } else if (Obj.Tags.TryGetValue("name", out var name) && name is not null) {
            Text = name;
        }
    }

    public override void Draw(PageRenderer renderer, int layer) {
        if (layer != Layer || Disabled) {
            return;
        }

        FontFamily fontFamily = FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica);
        //if (Properties.TryGetValue("font-family", out var family)) {
        //    fontFamily = FontFamily.ResolveFontFamily(family) ?? fontFamily;
        //}

        var fontSize = GetNum("font-size", renderer.Renderer.ZoomLevel, defaultValue: 10);
        var font = new Font(fontFamily, fontSize);

        TextAnchors anchor = TextAnchors.Center;
        if (Properties.TryGetValue("text-align-horizontal", out var align)) {
            switch (align) {
            case "center":
                anchor = TextAnchors.Center;
                break;
            case "left":
                anchor = TextAnchors.Left;
                break;
            case "right":
                anchor = TextAnchors.Right;
                break;
            }
        }

        var halo = GetNum("text-halo-width", renderer.Renderer.ZoomLevel, 1f);
        var haloColour = GetColour("text-halo-color", "text-halo-opacity", defaultColour: Colours.White);
        if (Obj is Line && Nodes.Count >= 2) {
            var nodes = Nodes[0].Longitude < Nodes.Last().Longitude ? Nodes : Nodes.AsEnumerable().Reverse();
            GraphicsPath path = new();
            foreach (var node in nodes) {
                path.LineTo(renderer.LongitudeToX(node.Longitude), renderer.LatitudeToY(node.Latitude));
            }

            int n = (int) Math.Ceiling(path.MeasureLength() / LINE_TEXT_INTERVAL);

            for (int i = 0; i < n; i++) {
                if (halo > 0) { // TODO halo widths
                    renderer.Graphics.StrokeTextOnPath(path, Text, font, haloColour, reference: (i+1f) / (n+1f), anchor: anchor, textBaseline: TextBaselines.Middle, lineWidth: 1);
                }
                renderer.Graphics.FillTextOnPath(path, Text, font, GetColour("text-color", "text-opacity"), reference: (i+1f) / (n+1f), anchor: anchor, textBaseline: TextBaselines.Middle);
            }
            return;
        } else if (TryGetCoordinates(out double lat, out double lon)) {
            double x = renderer.LongitudeToX(lon), y = renderer.LatitudeToY(lat);

            y += GetNum("text-offset-vertical", renderer.Renderer.ZoomLevel, fontSize);

            GraphicsPath path = new();
            path.LineTo(x-1, y);
            path.LineTo(x+1, y);
            if (halo > 0) { // TODO halo widths
                renderer.Graphics.StrokeTextOnPath(path, Text, font, haloColour, reference: 0.5, anchor: TextAnchors.Center, textBaseline: TextBaselines.Middle, lineWidth: 1);
            }
            renderer.Graphics.FillTextOnPath(path, Text, font, GetColour("text-color", "text-opacity"), reference: 0.5, anchor: TextAnchors.Center, textBaseline: TextBaselines.Middle);
        }
    }

    public override IEnumerable<int> GetLayers() {
        return [Layer];
    }

    private int Layer => GetLayerCode(
        3,
        LayerProperty
    );
}