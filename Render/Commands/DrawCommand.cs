using System.Globalization;
using OSMRender.Geo;
using VectSharp;

namespace OSMRender.Render.Commands;

public abstract class DrawCommand {

    protected IDictionary<string, string> Properties;
    protected GeoObj Obj;

    public Bounds Bounds => Obj.Bounds;

    public float MinZoom => Properties.ContainsKey("min-zoom") ? float.Parse(Properties["min-zoom"], CultureInfo.InvariantCulture) : 0;
    public float MaxZoom => Properties.ContainsKey("max-zoom") ? float.Parse(Properties["max-zoom"], CultureInfo.InvariantCulture) : 100;

    public DrawCommand(IDictionary<string, string> properties, GeoObj obj) {
        Properties = new Dictionary<string, string>();
        properties.ToList().ForEach(p => Properties[p.Key] = p.Value);
        Obj = obj;
    }

    protected void StrokePath(GraphicsPath path, PageRenderer renderer, string prefix, float baseWidth=0f) {
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
            GetColour($"{prefix}-color"),
            GetNum($"{prefix}-width", renderer.Renderer.ZoomLevel, baseWidth),
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

    protected Colour GetColour(string property) {
        if (Properties.ContainsKey(property)) {
            return Colour.FromCSSString(Properties[property]) ?? Colour.FromRgb(0, 0, 0);
        } else {
            return Colour.FromRgb(0, 0, 0);
        }
    }

    protected bool TryGetCoordinates(out double lat, out double lon) {
        if (Obj is Geo.Point point) {
            lat = point.Latitude;
            lon = point.Longitude;
            return true;
        } else if (Obj is Area area) {
            lat = area.OuterEdge.Select(p => p.Latitude).Sum() / area.OuterEdge.Count;
            lon = area.OuterEdge.Select(p => p.Longitude).Sum() / area.OuterEdge.Count;
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

    protected IEnumerable<(double, double, double)> GetCoordinatesAndAngle(int points) {
        if (Obj is Geo.Point point) {
            return new List<(double, double, double)> { (point.Latitude, point.Longitude, 0) };
        } else if (Obj is Area area) {
            return new List<(double, double, double)> { (area.MeanLatitude, area.MeanLongitude, 0) };
        } else if (Obj is Line line && line.Nodes.Count >= 2) {
            HashSet<(double, double, double)> ans = new();
            for (int i = 1; i <= points; i++) {
                double lat = line.Nodes[line.Nodes.Count * i / (points+1)].Latitude;
                double lon = line.Nodes[line.Nodes.Count * i / (points+1)].Longitude;
                var a = line.Nodes[line.Nodes.Count * i / (points+1) + 1].Latitude - lat;
                var b = line.Nodes[line.Nodes.Count * i / (points+1) + 1].Longitude - lon;
                double angle = Math.Atan2(a, b);
                ans.Add((lat, lon, angle));
            }
            return ans;
        } else {
            return new List<(double, double, double)> { (0, 0, 0) };
        }
    }

    protected float GetNum(string property, int zoomLevel, float baseVal) {
        if (Properties.ContainsKey(property)) {
            var val = Properties[property];
            if (val.Contains(':')) {
                float ans = 0;
                foreach (var pair in val.Split(";")) {
                    int level = int.Parse(pair[..pair.IndexOf(':')]);
                    float value = ParseFloat(pair[(pair.IndexOf(':')+1)..], baseVal);
                    if (zoomLevel >= level) {
                        ans = value;
                    }
                }
                return ans;
            } else {
                return ParseFloat(val, baseVal);
            }
        } else {
            return 0;
        }
    }

    protected static float ParseFloat(string val, float baseVal) {
        if (val.EndsWith("%")) {
            val = val[..^1].Trim();
            float value = float.Parse(val, CultureInfo.InvariantCulture);
            return (1 + value / 100f) * baseVal;
        } else {
            float value = float.Parse(val, CultureInfo.InvariantCulture);
            return value;
        }
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

    public abstract void Draw(PageRenderer renderer, int layer);

    public abstract IEnumerable<int> GetLayers();
}