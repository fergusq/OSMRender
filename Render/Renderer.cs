using OSMRender.Geo;
using OsmSharp.API;
using VectSharp;

namespace OSMRender.Render;

public class Renderer {

    public Geo.Bounds Bounds { get; set; }

    public int MinTileX { get; set; }
    public int MinTileY { get; set; }
    public int MaxTileX { get; set; }
    public int MaxTileY { get; set; }

    public int ZoomLevel { get; set; }

    public Renderer(Geo.Bounds bounds, int zoomLevel) {
        Bounds = bounds;
        ZoomLevel = zoomLevel;

        MinTileX = (int) Math.Floor(LongitudeToTileNumber(bounds.MinLongitude));
        MinTileY = (int) Math.Floor(LatitudeToTileNumber(bounds.MaxLatitude));
        MaxTileX = (int) Math.Ceiling(LongitudeToTileNumber(bounds.MaxLongitude));
        MaxTileY = (int) Math.Ceiling(LatitudeToTileNumber(bounds.MinLatitude));
    }

    public double LongitudeToTileNumber(double longitude) {
        var n = Math.Pow(2, ZoomLevel);
        return n * ((longitude + 180.0) / 360.0);
    }

    public double LatitudeToTileNumber(double latitude) {
        var n = Math.Pow(2, ZoomLevel);
        var lat_rad = latitude / 180.0 * Math.PI;
        return n * (1 - (Math.Log(Math.Tan(lat_rad) + 1/Math.Cos(lat_rad)) / Math.PI)) / 2;
    }

    public double TileNumberToLongitude(double tileX) {
        var n = Math.Pow(2, ZoomLevel);
        return tileX / n * 360.0 - 180.0;
    }

    public double TileNumberToLatitude(double tileY) {
        var n = Math.Pow(2, ZoomLevel);
        double lat_rad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * tileY / n)));
        return lat_rad * 180.0 / Math.PI;
    }

    public Dictionary<(int, int), Page> Render(GeoDocument doc) {
        Console.WriteLine($"Drawing tiles {MinTileX}..{MaxTileX} / {MinTileY}..{MaxTileY}");
        HashSet<int> layers = new();
        foreach (var cmd in doc.DrawCommands) {
            cmd.GetLayers().ToList().ForEach(l => layers.Add(l));
        }
        var layersSorted = layers.ToList();
        layersSorted.Sort();
        Dictionary<(int, int), Page> pages = new();
        for (int x = MinTileX; x <= MaxTileX; x++) {
            for (int y = MinTileY; y <= MaxTileY; y++) {
                var page = new Page(256, 256);
                pages[(x, y)] = page;
                var pageRenderer = new PageRenderer(this, page, x, y);
                Console.WriteLine($"Drawing tile {x}/{y} ({pageRenderer.TileBounds})...");
                foreach (var layer in layersSorted) {
                    Console.WriteLine($"Drawing layer {layer}...");
                    foreach (var cmd in doc.DrawCommands.Reverse()) {
                        if (cmd.Bounds.Overlaps(pageRenderer.TileBounds)) {
                            cmd.Draw(pageRenderer, layer);
                        }
                    }
                }
            }
        }

        return pages;
    }
}