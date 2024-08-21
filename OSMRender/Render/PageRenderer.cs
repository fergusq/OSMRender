using VectSharp;

namespace OSMRender.Render;

public class PageRenderer {

    public Renderer Renderer { get; set; }

    public Page Page { get; set; }

    public Graphics Graphics => Page.Graphics;

    public int TileX { get; set; }
    public int TileY { get; set; }

    public int TileWidth { get; set; }
    public int TileHeight { get; set; }

    public Geo.Bounds TileBounds => Geo.Bounds.From(
        Renderer.TileNumberToLatitude(TileY + TileHeight),
        Renderer.TileNumberToLatitude(TileY),
        Renderer.TileNumberToLongitude(TileX),
        Renderer.TileNumberToLongitude(TileX + TileWidth)
    );

    public PageRenderer(Renderer renderer, Page page, int tileX, int tileY, int tileWidth = 1, int tileHeight = 1) {
        Renderer = renderer;
        Page = page;
        TileX = tileX;
        TileY = tileY;
        TileWidth = tileWidth;
        TileHeight = tileHeight;
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