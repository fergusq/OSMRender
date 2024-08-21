using OSMRender.Geo;
using VectSharp;

namespace OSMRender.Render.Commands;

public class DrawLine : DrawCommand {
    private readonly Line Line;
    public DrawLine(IDictionary<string, string> properties, int importance, string feature, Line obj) : base(properties, importance, feature, obj) {
        Line = obj;
    }

    public override void Draw(PageRenderer renderer, int layer) {
        if (layer != FillLayer && layer != StrokeLayer) {
            return;
        }
        GraphicsPath path = new();
        foreach (var node in Line.Nodes) {
            path.LineTo(renderer.LongitudeToX(node.Longitude), renderer.LatitudeToY(node.Latitude));
        }

        if (layer == FillLayer) {
            StrokePath(path, renderer, "line");
        } else if (layer == StrokeLayer) {
            StrokePath(path, renderer, "border", GetNum("line-width", renderer.Renderer.ZoomLevel, 0f));
        }
    }

    public override IEnumerable<int> GetLayers() {
        return new int[] { StrokeLayer, FillLayer };
    }

    private int StrokeLayer => GetLayerCode(
        1,
        Obj.Tags is not null && Obj.Tags.ContainsKey("layer") ? int.Parse(Obj.Tags.GetValue("layer")) : 0,
        0
    );

    private int FillLayer => GetLayerCode(
        1,
        Obj.Tags is not null && Obj.Tags.ContainsKey("layer") ? int.Parse(Obj.Tags.GetValue("layer")) : 0,
        1
    );
}