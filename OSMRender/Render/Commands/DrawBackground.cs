using OSMRender.Geo;
using VectSharp;

namespace OSMRender.Render.Commands;

public class DrawBackground : DrawCommand {
    private readonly Background Background;

    public DrawBackground(IDictionary<string, string> properties, int importance, string feature, Background background) : base(properties, importance, feature, background) {
        Background = background;
    }

    public override void Draw(PageRenderer renderer, int layer) {
        // Background is drawn in layer -1
        if (layer != Layer) return;
        GraphicsPath path = new();
        path.MoveTo(renderer.LongitudeToX(Background.Bounds.MinLongitude), renderer.LatitudeToY(Background.Bounds.MinLatitude));
        path.LineTo(renderer.LongitudeToX(Background.Bounds.MinLongitude), renderer.LatitudeToY(Background.Bounds.MaxLatitude));
        path.LineTo(renderer.LongitudeToX(Background.Bounds.MaxLatitude), renderer.LatitudeToY(Background.Bounds.MaxLatitude));
        path.LineTo(renderer.LongitudeToX(Background.Bounds.MaxLatitude), renderer.LatitudeToY(Background.Bounds.MinLatitude));
        path.Close();

        renderer.Graphics.FillPath(path, GetColour("map-background-color"));
    }

    public override IEnumerable<int> GetLayers() {
        return new int[] { Layer };
    }

    private int Layer => GetLayerCode(
        -1,
        0,
        Obj.Tags is not null && Obj.Tags.ContainsKey("layer") ? int.Parse(Obj.Tags.GetValue("layer")) : 0
    );
}