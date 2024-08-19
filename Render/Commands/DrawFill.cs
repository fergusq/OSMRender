using OSMRender.Geo;
using VectSharp;

namespace OSMRender.Render.Commands;

public class DrawFill : DrawCommand {
    private readonly Area Area;

    public DrawFill(IDictionary<string, string> properties, Area obj) : base(properties, obj) {
        Area = obj;
    }

    public override void Draw(PageRenderer renderer, int layer) {
        // Areas are all drawn in layer 0
        if (layer == Layer) {
            var first = Area.Edge.First();
            GraphicsPath path = new();
            path.MoveTo(renderer.LongitudeToX(first.Longitude), renderer.LatitudeToY(first.Latitude));
            foreach (var node in Area.Edge.Skip(1)) {
                path.LineTo(renderer.LongitudeToX(node.Longitude), renderer.LatitudeToY(node.Latitude));
            }
            path.LineTo(renderer.LongitudeToX(first.Longitude), renderer.LatitudeToY(first.Latitude));

            renderer.Graphics.FillPath(path, GetColour("fill-color"));

            if (GetString("border-style") != "") {
                StrokePath(path, renderer, "border");
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