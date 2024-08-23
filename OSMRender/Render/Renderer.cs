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
using OSMRender.Logging;
using OSMRender.Render.Commands;
using VectSharp;

namespace OSMRender.Render;

/// <summary>
/// Renderer is used for rendering tiles on a specified zoom level.
/// </summary>
public class Renderer {

    public Bounds Bounds { get; set; }

    public int MinTileX { get; set; }
    public int MinTileY { get; set; }
    public int MaxTileX { get; set; }
    public int MaxTileY { get; set; }

    public int ZoomLevel { get; set; }

    private readonly ILogger Logger;

    public Renderer(Bounds bounds, int zoomLevel, ILogger logger) {
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

    /// <summary>
    /// Renders a tileset or a single image containing the specified tile range.
    /// </summary>
    /// <param name="doc">the GeoDocument to be renderer that contains the draw commands</param>
    /// <param name="tiled">true if multiple tiles are being renderer, false if only a single image is rendered containing all tiles</param>
    /// <returns>a dictionary from (tileX, tileY) to rendered tiles; if tiled is false, contains only a single rendered image</returns>
    public Dictionary<(int, int), Page> Render(GeoDocument doc, bool tiled = true)
    {
        Logger.Debug($"Drawing tiles {MinTileX}..{MaxTileX} / {MinTileY}..{MaxTileY}");
        Dictionary<int, List<DrawCommand>> layers = GetLayers(doc);
        Dictionary<(int, int), Page> pages = [];
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
            var pageRenderer = new PageRenderer(this, page, Logger, MinTileX, MinTileY);
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

    /// <summary>
    /// Renders a single tile at the specified coordinates.
    /// </summary>
    /// <param name="doc">the GeoDocument to be renderer that contains the draw commands</param>
    /// <param name="x">the tile x coordinate</param>
    /// <param name="y">the tile y coordinate</param>
    /// <returns>the rendered tile</returns>
    public Page RenderTile(GeoDocument doc, int x, int y) {
        Dictionary<int, List<DrawCommand>> layers = GetLayers(doc);
        RenderPage(layers, x, y, out Page page, out bool empty);
        return page;
    }

    private Dictionary<int, List<DrawCommand>> GetLayers(GeoDocument doc)
    {
        Dictionary<int, List<DrawCommand>> layers = [];
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
                    layers[layer] = [];
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
        var pageRenderer = new PageRenderer(this, page, Logger, x, y);
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