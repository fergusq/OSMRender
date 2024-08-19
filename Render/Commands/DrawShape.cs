using System.Globalization;
using OSMRender.Geo;
using VectSharp;

namespace OSMRender.Render.Commands;

public class DrawShape : DrawCommand {
    public DrawShape(IDictionary<string, string> properties, GeoObj obj) : base(properties, obj) {
    }

    public override void Draw(PageRenderer renderer, int layer) {
        if (layer != Layer) return;
        foreach (var (lat, lon, angle1) in GetCoordinatesAndAngle(Obj is Line line ? Math.Max(line.Nodes.Count / 5, 1) : 1)) {
            var angle = angle1;
            if (Properties.ContainsKey("angle")) {
                angle += GetNum("angle", renderer.Renderer.ZoomLevel, 0f) / 180 * Math.PI;
            }

            var shapeX = renderer.LongitudeToX(lon);
            var shapeY = renderer.LatitudeToY(lat);
            var size = Properties.ContainsKey("shape-size") ? GetNum("shape-size", renderer.Renderer.ZoomLevel, 1) : 1;

            Colour pathColour = Colour.FromRgb(0, 0, 0);
            Colour fillColour = Colour.FromRgb(0, 0, 0);
            double pathWidth = 1.0;
            GraphicsPath path = new();
            if (Properties["shape"] == "custom") {
                var def = Properties["shape-def"].Replace(" ", "").Split(";");
                foreach (var command in def) {
                    if (command.StartsWith("p:")) {
                        pathColour = Colour.FromCSSString(command[2..]) ?? pathColour;
                    } else if (command.StartsWith("f:")) {
                        fillColour = Colour.FromCSSString(command[2..]) ?? fillColour;
                    } else if (command.StartsWith("pw:")) {
                        pathWidth = double.Parse(command[3..], CultureInfo.InvariantCulture);
                    } else if (command.StartsWith("a:")) {
                        var points = command[2..].Split(",");
                        var x = double.Parse(points[0], CultureInfo.InvariantCulture);
                        var y = double.Parse(points[1], CultureInfo.InvariantCulture);
                        var dx = double.Parse(points[2], CultureInfo.InvariantCulture);
                        var dy = double.Parse(points[3], CultureInfo.InvariantCulture);
                        path.QuadraticBezierTo(new VectSharp.Point(x, y), new VectSharp.Point(dx, dy));
                    } else if (command.StartsWith("z") || command.StartsWith("Z")) {
                        path.Close();
                        var max = Math.Max(path.GetBounds().Size.Height, path.GetBounds().Size.Width);
                        path = path.Transform(p => Rotate(p, -angle)).Transform(p => new VectSharp.Point(shapeX + p.X/max*size, shapeY + p.Y/max*size));
                        renderer.Graphics.FillPath(path, fillColour);
                        renderer.Graphics.StrokePath(path, pathColour, pathWidth);
                        path = new();
                    } else if (command.StartsWith("m:")) {
                        var points = command[2..].Split(",");
                        if (points.Length%2 != 0) {
                            throw new Exception("odd number of coordinates");
                        }
                        for (int i = 0; i < points.Length; i += 2) {
                            var x = double.Parse(points[i], CultureInfo.InvariantCulture);
                            var y = double.Parse(points[i+1], CultureInfo.InvariantCulture);
                            path.MoveTo(x, y);
                        }
                    } else {
                        var points = command.StartsWith("l:") ? command[2..].Split(",") : command.Split(",");
                        if (points.Length%2 != 0) {
                            throw new Exception("odd number of coordinates");
                        }
                        for (int i = 0; i < points.Length; i += 2) {
                            var x = double.Parse(points[i], CultureInfo.InvariantCulture);
                            var y = double.Parse(points[i+1], CultureInfo.InvariantCulture);
                            path.LineTo(x, y);
                        }
                    }
                }
                if (path.Segments.Count > 0) {
                    var max = Math.Max(path.GetBounds().Size.Height, path.GetBounds().Size.Width);
                    path = path.Transform(p => Rotate(p, -angle)).Transform(p => new VectSharp.Point(shapeX + p.X/max*size, shapeY + p.Y/max*size));
                    renderer.Graphics.StrokePath(path, pathColour, pathWidth);
                }
            } else if (Properties["shape"] == "square") {
                path.MoveTo(shapeX + size * (-1), shapeY + size * (-1));
                path.LineTo(shapeX + size * (1), shapeY + size * (-1));
                path.LineTo(shapeX + size * (1), shapeY + size * (1));
                path.LineTo(shapeX + size * (-1), shapeY + size * (1));
                path.Close();
                renderer.Graphics.FillPath(path, fillColour);
                renderer.Graphics.StrokePath(path, pathColour, pathWidth);
            }
        }
    }

    private static VectSharp.Point Rotate(VectSharp.Point point, double angle) {
        return new VectSharp.Point(point.X * Math.Cos(angle) - point.Y * Math.Sin(angle), point.X * Math.Sin(angle) + point.Y * Math.Cos(angle));
    }

    public override IEnumerable<int> GetLayers() {
        return new int[] { Layer };
    }

    private int Layer => GetLayerCode(
        2,
        0,
        Obj.Tags is not null && Obj.Tags.ContainsKey("layer") ? int.Parse(Obj.Tags.GetValue("layer")) : 0
    );
}