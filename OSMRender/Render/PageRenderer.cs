// This file is part of OSMRender.
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
public class PageRenderer {

    public Renderer Renderer { get; set; }

    public Page Page { get; set; }

    public Graphics Graphics => Page.Graphics;

    public int TileX { get; set; }
    public int TileY { get; set; }

    public int TileWidth { get; set; }
    public int TileHeight { get; set; }

    internal ILogger Logger;

    public Geo.Bounds TileBounds => Geo.Bounds.From(
        Renderer.TileNumberToLatitude(TileY + TileHeight),
        Renderer.TileNumberToLatitude(TileY),
        Renderer.TileNumberToLongitude(TileX),
        Renderer.TileNumberToLongitude(TileX + TileWidth)
    );

    public PageRenderer(Renderer renderer, Page page, ILogger logger, int tileX, int tileY, int tileWidth = 1, int tileHeight = 1) {
        Renderer = renderer;
        Page = page;
        TileX = tileX;
        TileY = tileY;
        TileWidth = tileWidth;
        TileHeight = tileHeight;
        Logger = logger;
    }

    public double LongitudeToX(double longitude) {
        var x = Renderer.LongitudeToTileNumber(longitude);
        return 256 * (x - TileX);
    }

    public double LatitudeToY(double latitude) {
        var y = Renderer.LatitudeToTileNumber(latitude);
        return 256 * (y - TileY);
    }

}