using OSMRender.Geo;
using VectSharp;

namespace OSMRender.Render.Commands;

public class DrawText : LineDrawCommand {

    private readonly static double LINE_TEXT_INTERVAL = 400;

    private readonly bool Disabled = false;

    public DrawText(IDictionary<string, string> properties, int importance, string feature, GeoObj obj) : base(properties, importance, feature, obj) {
        if (Properties.TryGetValue("text", out var key) && key is not null) {
            if (Obj.Tags.ContainsKey(key)) {
                Properties["text"] = Obj.Tags[key];
            } else {
                Disabled = true; // TODO we should parse the expression with [[addr:housenum]] and @if() etc.
            }
        } else if (Obj.Tags.TryGetValue("name", out var name) && name is not null) {
            Properties["text"] = name;
        }
    }

    public override void Draw(PageRenderer renderer, int layer) {
        if (layer != Layer || Disabled) {
            return;
        }

        string text;
        if (Properties.TryGetValue("text", out var key) && key is not null) {
            text = key;
        } else {
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

        if (Obj is Line && Points.Count >= 2) {
            var nodes = Points[0].Longitude < Points[^1].Longitude ? Points : Points.AsEnumerable().Reverse();
            GraphicsPath path = new();
            foreach (var node in nodes) {
                path.LineTo(renderer.LongitudeToX(node.Longitude), renderer.LatitudeToY(node.Latitude));
            }

            int n = (int) Math.Ceiling(path.MeasureLength() / LINE_TEXT_INTERVAL);

            var halo = GetNum("text-halo-width", renderer.Renderer.ZoomLevel, 1f);
            for (int i = 0; i < n; i++) {
                if (halo > 0) { // TODO halo widths
                    renderer.Graphics.StrokeTextOnPath(path, text, font, GetColour("text-halo-color", Colours.White), reference: (i+1f) / (n+1f), anchor: anchor, textBaseline: TextBaselines.Middle, lineWidth: 1);
                }
                renderer.Graphics.FillTextOnPath(path, text, font, GetColour("text-color"), reference: (i+1f) / (n+1f), anchor: anchor, textBaseline: TextBaselines.Middle);
            }
            return;
        } else if (TryGetCoordinates(out double lat, out double lon)) {
            double x = renderer.LongitudeToX(lon), y = renderer.LatitudeToY(lat);

            y += GetNum("text-offset-vertical", renderer.Renderer.ZoomLevel, fontSize);

            GraphicsPath path = new();
            path.LineTo(x-1, y);
            path.LineTo(x+1, y);
            if (GetNum("text-halo-width", renderer.Renderer.ZoomLevel, 1f) > 0) { // TODO halo widths
                renderer.Graphics.StrokeTextOnPath(path, text, font, GetColour("text-halo-color", Colours.White), reference: 0.5, anchor: TextAnchors.Center, textBaseline: TextBaselines.Middle, lineWidth: 1);
            }
            renderer.Graphics.FillTextOnPath(path, text, font, GetColour("text-color"), reference: 0.5, anchor: TextAnchors.Center, textBaseline: TextBaselines.Middle);
        }
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