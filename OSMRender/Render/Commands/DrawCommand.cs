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

using OSMRender.Geo;
using OSMRender.Utils;
using VectSharp;

namespace OSMRender.Render.Commands;

public abstract class DrawCommand {

    public IDictionary<string, string> Properties { get; }
    public int Importance { get; }
    public string Feature { get; }
    public GeoObj Obj { get; }

    public Bounds Bounds => Obj.Bounds;

    public double MinZoom => Properties.ContainsKey("min-zoom") ? Properties["min-zoom"].ParseInvariantDouble() : 0;
    public double MaxZoom => Properties.ContainsKey("max-zoom") ? Properties["max-zoom"].ParseInvariantDouble() : 100;

    public DrawCommand(IDictionary<string, string> properties, int importance, string feature, GeoObj obj) {
        Properties = new Dictionary<string, string>();
        properties.ToList().ForEach(p => Properties[p.Key] = p.Value);
        Importance = importance;
        Feature = feature;
        Obj = obj;
    }

    protected void StrokePath(GraphicsPath path, PageRenderer renderer, string prefix, double baseWidth=0) {
        var lineStyle = GetString($"{prefix}-style");
        var lineJoinStyle = GetString($"line-join");
        var lineCapStyle = GetString($"{prefix}-end-cap");

        LineDash? dash = null;
        if (lineStyle == "none") {
            return;
        } else if (lineStyle == "dash") {
            dash = new(2, 2, 0);
        } else if (lineStyle == "dashlong") {
            dash = new(4, 4, 0);
        } else if (lineStyle == "dot") {
            dash = new(2, 5, 0);
        } else if (lineStyle == "solid") {
            dash = null;
        }

        LineCaps caps = LineCaps.Butt;
        if (lineCapStyle == "round") {
            caps = LineCaps.Round;
        }

        LineJoins joins = LineJoins.Miter;
        if (lineJoinStyle == "round") {
            joins = LineJoins.Round;
        }

        renderer.Graphics.StrokePath(
            path,
            GetColour($"{prefix}-color", opacityProperty: $"{prefix}-opacity"),
            prefix == "border" ? baseWidth + 2*GetNum($"border-width", renderer.Renderer.ZoomLevel, baseWidth) : GetNum($"{prefix}-width", renderer.Renderer.ZoomLevel, baseWidth),
            lineDash: dash,
            lineCap: caps,
            lineJoin: joins
        );
    }

    protected string GetString(string property) {
        if (Properties.ContainsKey(property)) {
            return Properties[property];
        } else {
            return "";
        }
    }

    protected Colour GetColour(string property, string opacityProperty, Colour? defaultColour = null) {
        Colour colour;
        if (Properties.ContainsKey(property)) {
            var val = Properties[property];
            var parts = val.Split(' ');
            if (parts.Length == 3) {
                var colour1 = Colour.FromCSSString(parts[0]) ?? defaultColour ?? Colour.FromRgb(0, 0, 0);
                var colour2 = Colour.FromCSSString(parts[1]) ?? defaultColour ?? Colour.FromRgb(0, 0, 0);
                var ratio = ParseDouble(parts[2], 1f);
                colour = Colour.FromRgba(
                    colour1.R * (1-ratio) + colour2.R * ratio,
                    colour1.G * (1-ratio) + colour2.G * ratio,
                    colour1.B * (1-ratio) + colour2.B * ratio,
                    colour1.A * (1-ratio) + colour2.A * ratio
                );
            } else {
                colour = Colour.FromCSSString(val) ?? defaultColour ?? Colour.FromRgb(0, 0, 0);
            }
        } else {
            colour =  defaultColour ?? Colour.FromRgb(0, 0, 0);
        }
        return opacityProperty is null ? colour : colour.WithAlpha(GetNum(opacityProperty, 0, 1, 1));
    }

    protected bool TryGetCoordinates(out double lat, out double lon) {
        if (Obj is Geo.Point point) {
            lat = point.Latitude;
            lon = point.Longitude;
            return true;
        } else if (Obj is Area area) {
            lat = area.OuterEdges.SelectMany(e => e).Select(p => p.Latitude).Sum() / area.OuterEdges.SelectMany(e => e).Count();
            lon = area.OuterEdges.SelectMany(e => e).Select(p => p.Longitude).Sum() / area.OuterEdges.SelectMany(e => e).Count();
            return true;
        } else if (Obj is Line line && line.Nodes.Count > 0) {
            lat = line.Nodes[line.Nodes.Count / 2].Latitude;
            lon = line.Nodes[line.Nodes.Count / 2].Longitude;
            return true;
        } else {
            lat = 0;
            lon = 0;
            return false;
        }
    }

    protected bool TryGetCoordinatesAndAngle(out double lat, out double lon, out double angle) {
        if (Obj is Geo.Point point) {
            lat = point.Latitude;
            lon = point.Longitude;
            angle = 0;
            return true;
        } else if (Obj is Area area) {
            lat = area.MeanLatitude;
            lon = area.MeanLongitude;
            angle = 0;
            return true;
        } else if (Obj is Line line && line.Nodes.Count >= 2) {
            lat = line.Nodes[line.Nodes.Count / 2].Latitude;
            lon = line.Nodes[line.Nodes.Count / 2].Longitude;
            var a = line.Nodes[line.Nodes.Count / 2 + 1].Latitude - line.Nodes[line.Nodes.Count / 2].Latitude;
            var b = line.Nodes[line.Nodes.Count / 2 + 1].Longitude - line.Nodes[line.Nodes.Count / 2].Longitude;
            angle = Math.Atan2(a, b);
            return true;
        } else if (Obj is Line line2 && line2.Nodes.Count > 0) {
            lat = line2.Nodes[line2.Nodes.Count / 2].Latitude;
            lon = line2.Nodes[line2.Nodes.Count / 2].Longitude;
            angle = 0;
            return true;
        } else {
            lat = 0;
            lon = 0;
            angle = 0;
            return false;
        }
    }

    protected double GetNum(string property, int zoomLevel, double baseVal = 0, double defaultValue = 0) {
        if (Properties.ContainsKey(property)) {
            var val = Properties[property];
            if (val.Contains(':')) {
                double ans = 0;
                foreach (var pair in val.Split(';')) {
                    double level = pair.Substring(0, pair.IndexOf(':')).ParseInvariantDouble();
                    double value = ParseDouble(pair.Substring(pair.IndexOf(':')+1), baseVal);
                    if (zoomLevel >= level) {
                        ans = value;
                    }
                }
                return ans;
            } else {
                return ParseDouble(val, baseVal);
            }
        } else {
            return defaultValue;
        }
    }

    protected static double ParseDouble(string val, double baseVal) {
        if (val.EndsWith("%")) {
            val = val.Substring(0, val.Length - 1).Trim();
            double value = val.ParseInvariantDouble();
            return value / 100 * baseVal;
        } else {
            double value = val.ParseInvariantDouble();
            return value;
        }
    }

    protected static GraphicsPath NodesToPath(PageRenderer renderer, IEnumerable<Geo.Point> nodes) {
        var path = new GraphicsPath();
        var first = nodes.First();
        path.MoveTo(renderer.LongitudeToX(first.Longitude), renderer.LatitudeToY(first.Latitude));
        foreach (var node in nodes.Skip(1)) {
            path.LineTo(renderer.LongitudeToX(node.Longitude), renderer.LatitudeToY(node.Latitude));
        }
        path.LineTo(renderer.LongitudeToX(first.Longitude), renderer.LatitudeToY(first.Latitude));
        path.Close();
        return path;
    }

    protected (VectSharp.Point point, Size size) GetPageBounds(PageRenderer renderer) {
        var bounds = Obj.Bounds;
        var minX = renderer.LongitudeToX(bounds.MinLongitude);
        var maxX = renderer.LongitudeToX(bounds.MaxLongitude);
        var minY = renderer.LatitudeToY(bounds.MaxLatitude); // Y axis is flipped
        var maxY = renderer.LatitudeToY(bounds.MinLatitude);
        var buffer = 20; // add a bit extra space for borders etc.
        return (
            new VectSharp.Point(minX - buffer, minY - buffer),
            new Size(maxX - minX + 2 * buffer, maxY - minY + 2 * buffer)
        );
    }

    protected static int GetLayerCode(int layer) {
        return layer * 10000;
    }

    protected static int GetLayerCode(int layer, int sublayer) {
        return layer * 10000 + 100 * sublayer;
    }

    protected static int GetLayerCode(int layer, int sublayer, int subsublayer) {
        return layer * 10000 + 100 * sublayer + subsublayer;
    }

    protected int LayerProperty {
        get {
            try {
                if (Obj.Tags is not null && Obj.Tags.ContainsKey("layer")) {
                    var layerString = Obj.Tags["layer"];
                    layerString = layerString.Contains(';') ? layerString.Substring(0, layerString.IndexOf(';')) : layerString;
                    return layerString.ParseInvariantInt();
                }
                return 0;
            } catch (FormatException) {
                return 0;
            }
        }
    }

    public abstract void Draw(PageRenderer renderer, int layer);

    public abstract IEnumerable<int> GetLayers();
}