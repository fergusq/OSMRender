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
using OSMRender.Utils;
using VectSharp;

namespace OSMRender.Render.Commands;

public class DrawShape(IDictionary<string, string> properties, int importance, string feature, GeoObj obj) : LineDrawCommand(properties, importance, feature, obj) {

    private readonly static double LINE_SHAPE_INTERVAL = 100;

    public override void Draw(PageRenderer renderer, int layer) {
        if (layer != Layer) return;

        List<GraphicsPath> paths = [];
        Colour fillColour = GetColour("fill-color", "fill-opacity");
        Colour pathColour = GetColour("border-color", "border-opacity", defaultColour: GetColour("line-color", "line-opacity", defaultColour: fillColour));
        double pathWidth = 1.0;
        GraphicsPath path = new();
        var shape = GetString("shape");
        if (shape == "custom") {
            var def = Properties["shape-def"].Replace(" ", "").Split(';');
            foreach (var command in def) {
                if (command.StartsWith("p:")) {
                    pathColour = Colour.FromCSSString(command.Substring(2)) ?? pathColour;
                } else if (command.StartsWith("f:")) {
                    fillColour = Colour.FromCSSString(command.Substring(2)) ?? fillColour;
                } else if (command.StartsWith("pw:")) {
                    pathWidth = command.Substring(3).ParseInvariantDouble();
                } else if (command.StartsWith("a:")) {
                    var points = command.Substring(2).Split(',');
                    var x = points[0].ParseInvariantDouble();
                    var y = points[1].ParseInvariantDouble();
                    var dx = points[2].ParseInvariantDouble();
                    var dy = points[3].ParseInvariantDouble();
                    path.QuadraticBezierTo(new VectSharp.Point(x, y), new VectSharp.Point(dx, dy));
                } else if (command.StartsWith("z") || command.StartsWith("Z")) {
                    path.Close();
                    paths.Add(path);
                    path = new();
                } else if (command.StartsWith("m:")) {
                    var points = command.Substring(2).Split(',');
                    if (points.Length%2 != 0) {
                        throw new Exception("odd number of coordinates");
                    }
                    for (int i = 0; i < points.Length; i += 2) {
                        var x = points[i].ParseInvariantDouble();
                        var y = points[i+1].ParseInvariantDouble();
                        path.MoveTo(x, y);
                    }
                } else {
                    var points = command.StartsWith("l:") ? command.Substring(2).Split(',') : command.Split(',');
                    if (points.Length%2 != 0) {
                        throw new Exception("odd number of coordinates");
                    }
                    for (int i = 0; i < points.Length; i += 2) {
                        var x = points[i].ParseInvariantDouble();
                        var y = points[i+1].ParseInvariantDouble();
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
        } else if (shape == "diamond") {
            path.MoveTo(0, -1);
            path.LineTo(-1, 0);
            path.LineTo(0, 1);
            path.LineTo(1, 0);
            path.Close();
            paths.Add(path);
        } else if (shape == "triangle") {
            path.MoveTo(-0.5, 0);
            path.LineTo(0, -0.75);
            path.LineTo(0.5, 0);
            path.Close();
            paths.Add(path);
        } else if (shape == "circle") {
            path.Arc(0, 0, 1, 0, 2*Math.PI);
            paths.Add(path);
        } else {
            renderer.Logger.Error($"{Feature}: Unknown shape `{shape}'");
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

            var shapeInterval = size * GetNum("shape-spacing", renderer.Renderer.ZoomLevel, defaultValue: LINE_SHAPE_INTERVAL/size);
            var minShapeLen = shapeInterval / 2;

            // Draw shape along the line at intervals of shapeInterval units
            var len = linePath.MeasureLength();
            if (len >= minShapeLen) {
                for (double i = minShapeLen; i < len; i += shapeInterval) {
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
        return [Layer];
    }

    private int Layer => GetLayerCode(
        2,
        LayerProperty
    );
}