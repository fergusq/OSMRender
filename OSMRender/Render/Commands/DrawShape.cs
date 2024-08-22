// This file is part of OSMRender.
// Copyright (c) 2024 Iikka Hauhio
//
// OSMRender is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OSMRender is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OSMRender. If not, see <https://www.gnu.org/licenses/>.

using System.Globalization;
using OSMRender.Geo;
using VectSharp;

namespace OSMRender.Render.Commands;

public class DrawShape : LineDrawCommand {

    private readonly static double LINE_SHAPE_INTERVAL = 100;
    private readonly static double MIN_LINE_SHAPE_LEN = LINE_SHAPE_INTERVAL / 2;

    public DrawShape(IDictionary<string, string> properties, int importance, string feature, GeoObj obj) : base(properties, importance, feature, obj) {
    }

    public override void Draw(PageRenderer renderer, int layer) {
        if (layer != Layer) return;

        List<GraphicsPath> paths = new();
        Colour fillColour = GetColour("fill-color", "fill-opacity");
        Colour pathColour = GetColour("border-color", "border-opacity", defaultColour: GetColour("line-color", "line-opacity", defaultColour: fillColour));
        double pathWidth = 1.0;
        GraphicsPath path = new();
        var shape = GetString("shape");
        if (shape == "custom") {
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
                    paths.Add(path);
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
                path.Close();
                paths.Add(path);
            }
        } else if (shape == "square") {
            path.MoveTo(-1, -1);
            path.LineTo(1, -1);
            path.LineTo(1, 1);
            path.LineTo(-1, 1);
            path.Close();
            paths.Add(path);
            renderer.Graphics.FillPath(path, fillColour);
            renderer.Graphics.StrokePath(path, pathColour, pathWidth);
        } else if (shape == "circle") {
            path.Arc(0, 0, 1, 0, 2*Math.PI);
            paths.Add(path);
            renderer.Graphics.FillPath(path, fillColour);
            renderer.Graphics.StrokePath(path, pathColour, pathWidth);
        } else {
            renderer.Logger.Error($"Unknown shape `{shape}'");
            return;
        }

        var size = Properties.ContainsKey("shape-size") ? GetNum("shape-size", renderer.Renderer.ZoomLevel) : 1;
        double angleOffset = 0;
        if (Properties.ContainsKey("angle")) {
            angleOffset = GetNum("angle", renderer.Renderer.ZoomLevel) / 180 * Math.PI;
        }

        if (Obj is Line) {
            var linePath = new GraphicsPath();
            foreach (var node in Nodes) {
                linePath.LineTo(renderer.LongitudeToX(node.Longitude), renderer.LatitudeToY(node.Latitude));
            }

            // Draw shape along the line at intervals of LINE_SHAPE_INTERVAL units
            var len = linePath.MeasureLength();
            if (len >= MIN_LINE_SHAPE_LEN) {
                for (double i = MIN_LINE_SHAPE_LEN; i < len; i += LINE_SHAPE_INTERVAL) {
                    var position = linePath.GetPointAtAbsolute(i);
                    var tangent = linePath.GetTangentAtAbsolute(i);
                    var angle = Math.Atan2(tangent.Y, tangent.X) + angleOffset;

                    foreach (var path1 in paths) {
                        var max = Math.Max(path1.GetBounds().Size.Height, path1.GetBounds().Size.Width);
                        var transformed = path1.Transform(p => Rotate(p, angle)).Transform(p => new VectSharp.Point(position.X + p.X/max*size, position.Y + p.Y/max*size));
                        renderer.Graphics.FillPath(transformed, fillColour);
                        renderer.Graphics.StrokePath(transformed, pathColour, pathWidth);
                    }
                }
            }
        } else {
            TryGetCoordinatesAndAngle(out var lat, out var lon, out var angle);
            
            angle = -angle - angleOffset;
            var shapeX = renderer.LongitudeToX(lon);
            var shapeY = renderer.LatitudeToY(lat);

            foreach (var path1 in paths) {
                var max = Math.Max(path1.GetBounds().Size.Height, path1.GetBounds().Size.Width);
                var transformed = path1.Transform(p => Rotate(p, angle)).Transform(p => new VectSharp.Point(shapeX + p.X/max*size, shapeY + p.Y/max*size));
                renderer.Graphics.FillPath(transformed, fillColour);
                renderer.Graphics.StrokePath(transformed, pathColour, pathWidth);
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
        Obj.Tags is not null && Obj.Tags.ContainsKey("layer") ? int.Parse(Obj.Tags["layer"]) : 0
    );
}