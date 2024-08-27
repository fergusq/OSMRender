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

    public RenderingProperties RenderingProperties { get; }

    public Bounds Bounds => Obj.Bounds;

    public double MinZoom => RenderingProperties.MinZoom;
    public double MaxZoom => RenderingProperties.MaxZoom;

    public DrawCommand(IDictionary<string, string> properties, int importance, string feature, GeoObj obj) {
        Properties = new Dictionary<string, string>();
        properties.ToList().ForEach(p => Properties[p.Key] = p.Value);
        RenderingProperties = RenderingProperties.FromDictionary(properties);
        Importance = importance;
        Feature = feature;
        Obj = obj;
    }

    protected void StrokePath(GraphicsPath path, PageRenderer renderer, bool border) {
        var lineStyle = border ? RenderingProperties.BorderStyle : RenderingProperties.LineStyle;
        var lineJoinStyle = RenderingProperties.LineJoin;
        var lineCapStyle = border ? RenderingProperties.BorderEndCap : RenderingProperties.LineEndCap;

        LineDash? dash = null;
        if (lineStyle == RenderingProperties.LineStyles.None) {
            return;
        } else if (lineStyle == RenderingProperties.LineStyles.Dash) {
            dash = new(6, 2, 0);
        } else if (lineStyle == RenderingProperties.LineStyles.Dashlong) {
            dash = new(12, 12, 0);
        } else if (lineStyle == RenderingProperties.LineStyles.Dot) {
            dash = new(2, 2, 0);
        } else if (lineStyle == RenderingProperties.LineStyles.Solid) {
            dash = null;
        }

        LineCaps caps = LineCaps.Butt;
        if (lineCapStyle == RenderingProperties.LineCaps.None) {
            caps = LineCaps.Butt;
        } else if (lineCapStyle == RenderingProperties.LineCaps.Round) {
            caps = LineCaps.Round;
        } else if (lineCapStyle == RenderingProperties.LineCaps.Square) {
            caps = LineCaps.Square;
        }

        LineJoins joins = LineJoins.Miter;
        if (lineJoinStyle == RenderingProperties.LineJoins.Round) {
            joins = LineJoins.Round;
        }

        var colour = border ? RenderingProperties.GetBorderColorFor(renderer.Renderer.ZoomLevel) : RenderingProperties.GetLineColorFor(renderer.Renderer.ZoomLevel);
        var lineWidth = RenderingProperties.LineWidth.GetFor(renderer.Renderer.ZoomLevel);

        renderer.Graphics.StrokePath(
            path,
            colour,
            border ? lineWidth + 2*RenderingProperties.BorderWidth.GetFor(renderer.Renderer.ZoomLevel) : lineWidth,
            lineDash: dash,
            lineCap: caps,
            lineJoin: joins
        );
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