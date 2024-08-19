using VectSharp;

namespace OSMRender.Render;

public class PageRenderer {

    public Renderer Renderer { get; set; }

    public Page Page { get; set; }

    public Graphics Graphics => Page.Graphics;

    public int TileX { get; set; }
    public int TileY { get; set; }

    public Geo.Bounds TileBounds => Geo.Bounds.From(
        Renderer.TileNumberToLatitude(TileY + 1),
        Renderer.TileNumberToLatitude(TileY),
        Renderer.TileNumberToLongitude(TileX),
        Renderer.TileNumberToLongitude(TileX + 1)
    );

    public PageRenderer(Renderer renderer, Page page, int tileX, int tileY) {
        Renderer = renderer;
        Page = page;
        TileX = tileX;
        TileY = tileY;
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