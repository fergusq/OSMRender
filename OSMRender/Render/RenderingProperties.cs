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

using VectSharp;
using OSMRender.Utils;

namespace OSMRender.Render;

public class RenderingProperties {

    [AttributeUsage(AttributeTargets.Property)]
    private abstract class PropertyAttribute : Attribute {

        public abstract string Key { get; }

        public abstract object Default { get; }

        public abstract object ParseValue(string value, RenderingProperties properties);
    }

    private class StringPropertyAttribute(string key, string defaultValue, params string[] alternatives) : PropertyAttribute {
        public override string Key { get; } = key;

        public override object Default { get; } = defaultValue;

        public string[] Alternatives { get; } = alternatives;

        public override object ParseValue(string value, RenderingProperties properties) {
            return value;
        }
    }

    private class DoublePropertyAttribute(string key, double defaultValue, string? baseVal = null) : PropertyAttribute {
        public override string Key { get; } = key;

        public override object Default { get; } = defaultValue;

        private readonly string? BaseValProperty = baseVal;

        public override object ParseValue(string val, RenderingProperties properties) {
            double baseVal = BaseValProperty is null || BaseValProperty == "" ? 1.0 : (double) typeof(RenderingProperties).GetProperty(BaseValProperty).GetValue(properties);
            
            return ParseDouble(val).GetValue(baseVal);
        }
    }

    private class ZoomableValuePropertyAttribute(string key, double defaultValue, string? baseVal = null) : PropertyAttribute {
        public override string Key { get; } = key;

        public override object Default { get; } = ZoomableValue.Of(defaultValue);

        private readonly string? BaseValProperty = baseVal;

        public override object ParseValue(string val, RenderingProperties properties) {
            ZoomableValue baseVal = BaseValProperty is null || BaseValProperty == "" ? ZoomableValue.Of(1.0) : (ZoomableValue) typeof(RenderingProperties).GetProperty(BaseValProperty).GetValue(properties);
            
            if (val.Contains(':')) {
                List<(double, RelativeDouble)> alts = [];
                foreach (var pair in val.Split(';')) {
                    var level = pair.Substring(0, pair.IndexOf(':')).ParseInvariantDouble();
                    var value = ParseDouble(pair.Substring(pair.IndexOf(':')+1));
                    alts.Add((level, value));
                }
                return new ZoomableValue(alts, baseVal);
            } else {
                return new ZoomableValue([(0, ParseDouble(val))], baseVal);
            }
        }
    }

    private class ColourPropertyAttribute(string key, string defaultColour) : PropertyAttribute {
        public override string Key { get; } = key;

        public override object Default { get; } = Colour.FromCSSString(defaultColour) ?? Colours.Black;

        private Colour DefaultColour = Colour.FromCSSString(defaultColour) ?? Colours.Black;

        public override object ParseValue(string val, RenderingProperties properties) {
            var parts = val.Split(' ');
            if (parts.Length == 3) {
                var colour1 = Colour.FromCSSString(parts[0]) ?? DefaultColour;
                var colour2 = Colour.FromCSSString(parts[1]) ?? DefaultColour;
                var ratio = ParseDouble(parts[2]).GetValue(1.0);
                return Colour.FromRgba(
                    colour1.R * (1-ratio) + colour2.R * ratio,
                    colour1.G * (1-ratio) + colour2.G * ratio,
                    colour1.B * (1-ratio) + colour2.B * ratio,
                    colour1.A * (1-ratio) + colour2.A * ratio
                );
            } else {
                return Colour.FromCSSString(val) ?? DefaultColour;
            }
        }
    }

    private class EnumPropertyAttribute(string key, object defaultValue, Type enumType) : PropertyAttribute {
        public override string Key { get; } = key;

        public override object Default { get; } = defaultValue;

        public Type EnumType { get; } = enumType;

        public override object ParseValue(string value, RenderingProperties properties) {
            value = value.ToLower();
            foreach (var field in EnumType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)) {
                foreach (var attr in field.GetCustomAttributes(typeof(EnumValueAttribute), false)) {
                    if (((EnumValueAttribute) attr).Key.ToLower() == value) {
                        return field.GetRawConstantValue();
                    }
                }
            }
            throw new ArgumentException();
        }
    }

    private static RelativeDouble ParseDouble(string val) {
        if (val.EndsWith("%")) {
            val = val.Substring(0, val.Length - 1).Trim();
            double value = val.ParseInvariantDouble();
            return new RelativeDouble(value / 100.0, true);
        } else {
            double value = val.ParseInvariantDouble();
            return new RelativeDouble(value, false);
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    private class EnumValueAttribute(string key) : Attribute {
        public string Key { get; } = key;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private RenderingProperties() {}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public static RenderingProperties FromDictionary(IDictionary<string, string> dictionary) {
        var properties = new RenderingProperties();

        foreach (var p in typeof(RenderingProperties).GetProperties()) {
            foreach (var attr in p.GetCustomAttributes(typeof(PropertyAttribute), true)) {
                var propAttr = (PropertyAttribute) attr;
                if (dictionary.ContainsKey(propAttr.Key)) {
                    var val = propAttr.ParseValue(dictionary[propAttr.Key], properties);
                    p.SetValue(properties, val);
                } else {
                    p.SetValue(properties, propAttr.Default);
                }
            }
        }

        return properties;
    }

    public enum Alignments {
        [EnumValue("center")]
        Center,
        [EnumValue("near")]
        Near,
        [EnumValue("far")]
        Far,
    }

    public enum LineStyles {
        [EnumValue("none")]
        None,
        [EnumValue("dash")]
        Dash,
        [EnumValue("dashlong")]
        Dashlong,
        [EnumValue("dashdot")]
        Dashdot,
        [EnumValue("dashdotdot")]
        Dashdotdot,
        [EnumValue("dash8to2")]
        Dash8to2,
        [EnumValue("dash10to2")]
        Dash10to2,
        [EnumValue("dot")]
        Dot,
        [EnumValue("solid")]
        Solid,
    }

    public enum LineJoins {
        [EnumValue("round")]
        Round,
    }

    public enum LineCaps {
        [EnumValue("none")]
        None,
        [EnumValue("round")]
        Round,
        [EnumValue("square")]
        Square,
    }

    public enum Shapes {
        [EnumValue("circle")]
        Circle,
        [EnumValue("square")]
        Square,
        [EnumValue("triangle")]
        Triangle,
        [EnumValue("diamond")]
        Diamond,
        [EnumValue("custom")]
        Custom,
    }

    public class ZoomableValue(IEnumerable<(double zoomLevel, RelativeDouble value)> values, ZoomableValue? baseVal = null) {
        private readonly IEnumerable<(double zoomLevel, RelativeDouble value)> Values = values;
        private readonly ZoomableValue? BaseVal = baseVal;

        public double GetFor(double zoomLevel) {
            double value = 0;
            foreach (var alt in Values) {
                if (alt.zoomLevel <= zoomLevel) {
                    value = alt.value.GetValue(BaseVal?.GetFor(zoomLevel) ?? 1.0);
                }
            }
            return value;
        }

        public static ZoomableValue Of(double value) {
            return new ZoomableValue([(0, new RelativeDouble(value, false))]);
        }
    }

    public readonly struct RelativeDouble(double value, bool isRelative) {
        private readonly double Value = value;
        private readonly bool IsRelative = isRelative;

        public double GetValue(double baseValue) {
            return IsRelative ? Value * baseValue : Value;
        }
    }

    [DoubleProperty("min-zoom", 0.0)]
    public double MinZoom { get; set; }

    [DoubleProperty("max-zoom", 20.0)]
    public double MaxZoom { get; set; }

    [EnumProperty("align-horizontal", Alignments.Center, typeof(Alignments))]
    public Alignments AlignHorizontal { get; set; }

    [EnumProperty("align-vertical", Alignments.Center, typeof(Alignments))]
    public Alignments AlignVertical { get; set; }

    [ColourProperty("border-color", "#000")]
    public Colour BorderColor { get; set; }

    [ZoomableValueProperty("border-opacity", 1.0)]
    public ZoomableValue BorderOpacity { get; set; }

    public Colour GetBorderColorFor(double zoomLevel) {
        return BorderColor.WithAlpha(BorderOpacity.GetFor(zoomLevel));
    }

    [EnumProperty("border-start-cap", LineCaps.None, typeof(LineCaps))]
    public LineCaps BorderStartCap { get; set; }

    [EnumProperty("border-end-cap", LineCaps.None, typeof(LineCaps))]
    public LineCaps BorderEndCap { get; set; }

    [EnumProperty("border-style", LineStyles.None, typeof(LineStyles))]
    public LineStyles BorderStyle { get; set; }

    [ZoomableValueProperty("border-width", 1.0, nameof(LineWidth))]
    public ZoomableValue BorderWidth { get; set; }

    [ColourProperty("fill-color", "#000")]
    public Colour FillColor { get; set; }

    [ZoomableValueProperty("fill-opacity", 1.0)]
    public ZoomableValue FillOpacity { get; set; }

    public Colour GetFillColorFor(double zoomLevel) {
        return FillColor.WithAlpha(FillOpacity.GetFor(zoomLevel));
    }

    [ZoomableValueProperty("font-size", 10.0)]
    public ZoomableValue FontSize { get; set; }

    [StringProperty("icon-image", "")]
    public string IconImage { get; set; } = "";

    [ZoomableValueProperty("icon-width", 16.0)]
    public ZoomableValue IconWidth { get; set; }

    [ColourProperty("line-color", "#000")]
    public Colour LineColor { get; set; }

    [ZoomableValueProperty("line-opacity", 1.0)]
    public ZoomableValue LineOpacity { get; set; }

    public Colour GetLineColorFor(double zoomLevel) {
        return LineColor.WithAlpha(LineOpacity.GetFor(zoomLevel));
    }

    [EnumProperty("line-start-cap", LineCaps.None, typeof(LineCaps))]
    public LineCaps LineStartCap { get; set; }

    [EnumProperty("line-end-cap", LineCaps.None, typeof(LineCaps))]
    public LineCaps LineEndCap { get; set; }

    [EnumProperty("line-join", LineJoins.Round, typeof(LineJoins))]
    public LineJoins LineJoin { get; set; }

    [EnumProperty("line-style", LineStyles.Solid, typeof(LineStyles))]
    public LineStyles LineStyle { get; set; }

    [ZoomableValueProperty("line-width", 5.0)]
    public ZoomableValue LineWidth { get; set; }

    [ColourProperty("map-background-color", "#fff")]
    public Colour MapBackgroundColor { get; set; }

    [ZoomableValueProperty("map-background-opacity", 1.0)]
    public ZoomableValue MapBackgroundOpacity { get; set; }

    public Colour GetMapBackgroundColorFor(double zoomLevel) {
        return MapBackgroundColor.WithAlpha(MapBackgroundOpacity.GetFor(zoomLevel));
    }

    [EnumProperty("shape", Shapes.Circle, typeof(Shapes))]
    public Shapes Shape { get; set; }

    [StringProperty("shape-def", "")]
    public string ShapeDef { get; set; } = "";

    [ZoomableValueProperty("shape-size", 16.0)]
    public ZoomableValue ShapeSize { get; set; }

    [ZoomableValueProperty("shape-spacing", 2.0)]
    public ZoomableValue ShapeSpacing { get; set; }

    [ZoomableValueProperty("angle", 2.0)]
    public ZoomableValue Angle { get; set; }

    [StringProperty("text", "")]
    public string Text { get; set; } = "";

    [ColourProperty("text-color", "#000")]
    public Colour TextColor { get; set; }

    [ZoomableValueProperty("text-opacity", 1.0)]
    public ZoomableValue TextOpacity { get; set; }

    public Colour GetTextColorFor(double zoomLevel) {
        return TextColor.WithAlpha(TextOpacity.GetFor(zoomLevel));
    }

    [EnumProperty("text-align-horizontal", Alignments.Center, typeof(Alignments))]
    public Alignments TextAlignHorizontal { get; set; }

    [EnumProperty("text-align-vertical", Alignments.Center, typeof(Alignments))]
    public Alignments TextAlignVertical { get; set; }

    [ZoomableValueProperty("text-halo-width", 0.0)]
    public ZoomableValue TextHaloWidth { get; set; }

    [ColourProperty("text-halo-color", "#fff")]
    public Colour TextHaloColor { get; set; }

    [ZoomableValueProperty("text-halo-opacity", 1.0)]
    public ZoomableValue TextHaloOpacity { get; set; }

    public Colour GetTextHaloColorFor(double zoomLevel) {
        return TextHaloColor.WithAlpha(TextHaloOpacity.GetFor(zoomLevel));
    }

    [ZoomableValueProperty("text-offset-horizontal", 0.0, nameof(FontSize))]
    public ZoomableValue TextOffsetHorizontal { get; set; }

    [ZoomableValueProperty("text-offset-vertical", 0.0, nameof(FontSize))]
    public ZoomableValue TextOffsetVertical { get; set; }
}