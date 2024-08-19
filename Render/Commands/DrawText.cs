using OSMRender.Geo;
using VectSharp;

namespace OSMRender.Render.Commands;

public class DrawText : DrawCommand {
    public DrawText(IDictionary<string, string> properties, GeoObj obj) : base(properties, obj) {
    }

    public override void Draw(PageRenderer renderer, int layer) {
        if (layer != Layer) {
            return;
        }

        var text = "";
        if (Properties.TryGetValue("text", out var key) && key is not null) {
            if (Obj.Tags.ContainsKey(key)) {
                text = Obj.Tags[key];
            }
        } else if (Obj.Tags.TryGetValue("name", out var name) && name is not null) {
            text = name;
        } else {
            return;
        }
        

        FontFamily fontFamily = FontFamily.ResolveFontFamily(FontFamily.StandardFontFamilies.Helvetica);
        //if (Properties.TryGetValue("font-family", out var family)) {
        //    fontFamily = FontFamily.ResolveFontFamily(family) ?? fontFamily;
        //}

        var font = new Font(fontFamily, GetNum("font-size", renderer.Renderer.ZoomLevel, 0));

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

        double lat = 0, lon = 0;
        if (Obj is Line line) {
            if (line.Nodes.Count < 2) return;

            var nodes = line.Nodes[0].Longitude < line.Nodes[^1].Longitude ? line.Nodes : line.Nodes.AsEnumerable().Reverse();
            GraphicsPath path = new();
            foreach (var node in nodes) {
                path.LineTo(renderer.LongitudeToX(node.Longitude), renderer.LatitudeToY(node.Latitude));
            }

            renderer.Graphics.FillTextOnPath(path, text, font, GetColour("text-color"), reference: 0.5, anchor: anchor,
                        textBaseline: TextBaselines.Middle);
            return;
        } else TryGetCoordinates(out lat, out lon);

        var origin = new VectSharp.Point(renderer.LongitudeToX(lon), renderer.LatitudeToY(lat));
        renderer.Graphics.FillText(origin, text, font, GetColour("text-color"), TextBaselines.Baseline);
    }

    public override IEnumerable<int> GetLayers() {
        return new int[] { Layer };
    }

    private int Layer => GetLayerCode(
        3,
        0,
        Obj.Tags is not null && Obj.Tags.ContainsKey("layer") ? int.Parse(Obj.Tags.GetValue("layer")) : 0
    );
}