using System.Globalization;

namespace OSMRender.Utils;

internal static class Extensions {
    internal static long ParseInvariantLong(this string s) {
        return long.Parse(s, CultureInfo.InvariantCulture);
    }

    internal static int ParseInvariantInt(this string s) {
        return int.Parse(s, CultureInfo.InvariantCulture);
    }

    internal static double ParseInvariantDouble(this string s) {
        return double.Parse(s, CultureInfo.InvariantCulture);
    }
}