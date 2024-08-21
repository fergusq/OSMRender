using OSMRender.Geo;
using OSMRender.Logging;
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

    private readonly Logger Logger;

    public Renderer(Geo.Bounds bounds, int zoomLevel, Logger logger) {
        Bounds = bounds;
        ZoomLevel = zoomLevel;
        Logger = logger;

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

    public Dictionary<(int, int), Page> Render(GeoDocument doc, bool tiled = true)
    {
        Logger.Debug($"Drawing tiles {MinTileX}..{MaxTileX} / {MinTileY}..{MaxTileY}");
        Dictionary<int, List<DrawCommand>> layers = GetLayers(doc);
        Dictionary<(int, int), Page> pages = new();
        if (tiled)
        {
            for (int x = MinTileX; x <= MaxTileX; x++)
            {
                for (int y = MinTileY; y <= MaxTileY; y++)
                {
                    RenderPage(layers, x, y, out Page page, out bool empty);
                    if (!empty)
                    {
                        pages[(x, y)] = page;
                    }
                }
            }
        }
        else
        {
            int tileWidth = MaxTileX - MinTileX + 1;
            int tileHeight = MaxTileY - MinTileY + 1;
            var page = new Page(256 * tileWidth, 256 * tileHeight);
            pages[(MinTileX, MinTileY)] = page;
            Logger.Debug($"Generating {page.Width}x{page.Height} page");
            var pageRenderer = new PageRenderer(this, page, MinTileX, MinTileY);
            var bounds = pageRenderer.TileBounds.Extend(1.1);
            Logger.Debug("Bounds: " + pageRenderer.TileBounds.ToString());
            foreach (var layer in layers.Keys.OrderBy(key => key))
            {
                foreach (var cmd in layers[layer].AsEnumerable().Reverse())
                {
                    //if (cmd.Bounds.Overlaps(bounds)) {
                    cmd.Draw(pageRenderer, layer);
                    //}
                }
            }
        }

        return pages;
    }

    public Page RenderTile(GeoDocument doc, int x, int y) {
        Dictionary<int, List<DrawCommand>> layers = GetLayers(doc);
        RenderPage(layers, x, y, out Page page, out bool empty);
        return page;
    }

    private Dictionary<int, List<DrawCommand>> GetLayers(GeoDocument doc)
    {
        Dictionary<int, List<DrawCommand>> layers = new();
        foreach (var cmd in doc.DrawCommands)
        {
            if (ZoomLevel < cmd.MinZoom || ZoomLevel > cmd.MaxZoom)
            {
                continue;
            }
            foreach (var layer in cmd.GetLayers())
            {
                if (!layers.ContainsKey(layer))
                {
                    layers[layer] = new List<DrawCommand>();
                }

                layers[layer].Add(cmd);
            }
        }
        foreach (var layer in layers)
        {
            layer.Value.Sort((a, b) => b.Importance - a.Importance);
        }

        return layers;
    }

    private void RenderPage(Dictionary<int, List<DrawCommand>> layers, int x, int y, out Page page, out bool empty)
    {
        page = new Page(256, 256);
        var pageRenderer = new PageRenderer(this, page, x, y);
        var bounds = pageRenderer.TileBounds.Extend(1.1);
        Logger.Debug($"Drawing tile {x}/{y} ({pageRenderer.TileBounds})...");
        empty = true;
        foreach (var layer in layers.Keys.OrderBy(key => key))
        {
            //Console.WriteLine($"Drawing layer {layer}...");
            foreach (var cmd in layers[layer].AsEnumerable().Reverse())
            {
                if (cmd.Bounds.Overlaps(bounds))
                {
                    cmd.Draw(pageRenderer, layer);
                    empty = false;
                }
            }
        }
    }
}