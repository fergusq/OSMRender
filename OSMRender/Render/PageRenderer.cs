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

using OSMRender.Logging;
using VectSharp;

namespace OSMRender.Render;

/// <summary>
/// A helper class instantiated by Renderer that represents a single tile or image being generated.
/// </summary>
public class PageRenderer(Renderer renderer, Page page, ILogger logger, int tileX, int tileY, int tileWidth = 1, int tileHeight = 1) {

    public Renderer Renderer { get; set; } = renderer;

    public Page Page { get; set; } = page;

    public Graphics Graphics => Page.Graphics;

    public int TileX { get; set; } = tileX;
    public int TileY { get; set; } = tileY;

    public int TileWidth { get; set; } = tileWidth;
    public int TileHeight { get; set; } = tileHeight;

    internal ILogger Logger = logger;

    public Geo.Bounds TileBounds => Geo.Bounds.From(
        Renderer.TileNumberToLatitude(TileY + TileHeight),
        Renderer.TileNumberToLatitude(TileY),
        Renderer.TileNumberToLongitude(TileX),
        Renderer.TileNumberToLongitude(TileX + TileWidth)
    );

    public double LongitudeToX(double longitude) {
        var x = Renderer.LongitudeToTileNumber(longitude);
        return 256 * (x - TileX);
    }

    public double LatitudeToY(double latitude) {
        var y = Renderer.LatitudeToTileNumber(latitude);
        return 256 * (y - TileY);
    }

}