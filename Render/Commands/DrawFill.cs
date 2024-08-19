using OSMRender.Geo;
using VectSharp;

namespace OSMRender.Render.Commands;

public class DrawFill : DrawCommand {
    public DrawFill(IDictionary<string, string> properties, GeoObj obj) : base(properties, obj) {
    }

    public override void Draw(PageRenderer renderer, int layer) {
        // Areas are all drawn in layer 0
        if (layer == Layer) {
            IList<Geo.Point> edge;
            if (Obj is Area area) {
                edge = area.Edge.ToList();
            } else if (Obj is Line) {
                return;
            } else {
                throw new NotImplementedException();
            }
            var first = edge[0];
            edge.RemoveAt(0);
            GraphicsPath path = new();
            path.MoveTo(renderer.LongitudeToX(first.Longitude), renderer.LatitudeToY(first.Latitude));
            foreach (var node in edge) {
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