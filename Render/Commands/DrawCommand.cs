using System.Globalization;
using OSMRender.Geo;
using VectSharp;

namespace OSMRender.Render.Commands;

public abstract class DrawCommand {

    protected IDictionary<string, string> Properties;
    protected GeoObj Obj;

    public Bounds Bounds => Obj.Bounds;

    public DrawCommand(IDictionary<string, string> properties, GeoObj obj) {
        Properties = new Dictionary<string, string>();
        properties.ToList().ForEach(p => Properties[p.Key] = p.Value);
        Obj = obj;
    }

    protected void StrokePath(GraphicsPath path, PageRenderer renderer, string prefix, float baseWidth=0f) {
        var lineStyle = GetString($"{prefix}-style");
        var lineJoinStyle = GetString($"line-join");
        var lineCapStyle = GetString($"line-end-cap");

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
            return (1 + value / 100f) * baseVal * 2;
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