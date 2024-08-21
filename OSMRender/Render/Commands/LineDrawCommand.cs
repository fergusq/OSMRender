using OSMRender.Geo;

namespace OSMRender.Render.Commands;

public abstract class LineDrawCommand : DrawCommand {

    internal List<Point> Points { get; } = new();

    protected LineDrawCommand(IDictionary<string, string> properties, int importance, string feature, GeoObj obj) : base(properties, importance, feature, obj) {
        if (obj is Line line) {
            Points.AddRange(line.Nodes);
        }
    }
}