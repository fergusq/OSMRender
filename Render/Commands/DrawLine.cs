using OSMRender.Geo;
using VectSharp;

namespace OSMRender.Render.Commands;

public class DrawLine : DrawCommand {
    public DrawLine(IDictionary<string, string> properties, GeoObj obj) : base(properties, obj) {
    }

    public override void Draw(PageRenderer renderer, int layer) {
        if (layer != FillLayer && layer != StrokeLayer) {
            return;
        }

        IList<Geo.Point> nodes;
        if (Obj is Line line) {
            nodes = line.Nodes;
            if (nodes.Count == 0) {
                return;
            }
        } else if (Obj is Area) {
            return;
        } else {
            throw new NotImplementedException();
        }
        var first = nodes[0];
        GraphicsPath path = new();
        path.MoveTo(renderer.LongitudeToX(first.Longitude), renderer.LatitudeToY(first.Latitude));
        foreach (var node in nodes) {
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