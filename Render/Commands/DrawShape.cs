using OSMRender.Geo;

namespace OSMRender.Render.Commands;

public class DrawShape : DrawCommand {
    public DrawShape(IDictionary<string, string> properties, GeoObj obj) : base(properties, obj) {
    }

    public override void Draw(PageRenderer renderer, int layer) {
        //throw new NotImplementedException();
    }

    public override IEnumerable<int> GetLayers() {
        return Array.Empty<int>();
    }
}