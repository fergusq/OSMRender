using OSMRender.Geo;
using OSMRender.Render.Commands;
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
        MaxTileX = (int) Math.Ceiling(LongitudeToTileNumber(bounds.MaxLongitude));

        // Y axis is flipped
        MinTileY = (int) Math.Floor(LatitudeToTileNumber(bounds.MaxLatitude));
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

    public Dictionary<(int, int), Page> Render(GeoDocument doc, bool tiled = true) {
        Console.WriteLine($"Drawing tiles {MinTileX}..{MaxTileX} / {MinTileY}..{MaxTileY}");
        Dictionary<int, List<DrawCommand>> layers = new();
        foreach (var cmd in doc.DrawCommands) {
            if (ZoomLevel < cmd.MinZoom || ZoomLevel > cmd.MaxZoom) {
                continue;
            }
            foreach (var layer in cmd.GetLayers()) {
                if (!layers.ContainsKey(layer)) {
                    layers[layer] = new List<DrawCommand>();
                }

                layers[layer].Add(cmd);
            }
        }
        foreach (var layer in layers) {
            layer.Value.Sort((a, b) => b.Importance - a.Importance);
        }
        var layersSorted = layers.Keys.ToList();
        layersSorted.Sort();
        Dictionary<(int, int), Page> pages = new();
        if (tiled) {
            int total = (MaxTileX - MinTileX + 1) * (MaxTileY - MinTileY + 1);
            int i = 0;
            for (int x = MinTileX; x <= MaxTileX; x++) {
                for (int y = MinTileY; y <= MaxTileY; y++) {
                    i++;
                    if (i % 100 == 0) {
                        Console.Write($"\rRendering {100 * i / total}%");
                    }
                    var page = new Page(256, 256);
                    var pageRenderer = new PageRenderer(this, page, x, y);
                    var bounds = pageRenderer.TileBounds.Extend(1.1);
                    //Console.WriteLine($"Drawing tile {x}/{y} ({pageRenderer.TileBounds})...");
                    bool empty = true;
                    foreach (var layer in layersSorted) {
                        //Console.WriteLine($"Drawing layer {layer}...");
                        foreach (var cmd in layers[layer].AsEnumerable().Reverse()) {
                            if (cmd.Bounds.Overlaps(bounds)) {
                                cmd.Draw(pageRenderer, layer);
                                empty = false;
                            }
                        }
                    }
                    if (!empty) {
                        pages[(x, y)] = page;
                    }
                }
            }
            Console.WriteLine("\rRendering 100%");
        } else {
            int tileWidth = MaxTileX - MinTileX + 1;
            int tileHeight = MaxTileY - MinTileY + 1;
            var page = new Page(256 * tileWidth, 256 * tileHeight);
            pages[(MinTileX, MinTileY)] = page;
            Console.WriteLine($"Generating {page.Width}x{page.Height} page");
            var pageRenderer = new PageRenderer(this, page, MinTileX, MinTileY);
            var bounds = pageRenderer.TileBounds.Extend(1.1);
            Console.WriteLine("Bounds: " + pageRenderer.TileBounds.ToString());
            foreach (var layer in layersSorted) {
                foreach (var cmd in layers[layer].AsEnumerable().Reverse()) {
                    //if (cmd.Bounds.Overlaps(bounds)) {
                        cmd.Draw(pageRenderer, layer);
                    //}
                }
            }
        }

        return pages;
    }
}