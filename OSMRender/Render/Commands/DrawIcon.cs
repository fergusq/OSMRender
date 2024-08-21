using OSMRender.Geo;
using VectSharp;
using VectSharp.MuPDFUtils;

namespace OSMRender.Render.Commands;

public class DrawIcon : DrawCommand {

    private static Dictionary<string, RasterImage> ImageCache = new();
    public static string SearchPath { get; set; } = "";
    
    public DrawIcon(IDictionary<string, string> properties, int importance, GeoObj obj) : base(properties, importance, obj) {
    }

    public override void Draw(PageRenderer renderer, int layer) {
        if (layer != Layer) return;
        if (!TryGetCoordinates(out var lat, out var lon)) return;

        var x = renderer.LongitudeToX(lon);
        var y = renderer.LatitudeToY(lat);
        var size = Properties.ContainsKey("icon-width") ? GetNum("icon-width", renderer.Renderer.ZoomLevel, 1) : 10;

        var image = GetImage(Properties["icon-image"]);
        renderer.Graphics.DrawRasterImage(x - size/2, y - size/2, size, size, image);
    }

    private static RasterImage GetImage(string path) {
        if (ImageCache.TryGetValue(path, out var image)) {
            return image;
        }

        if (!File.Exists(path)) {
            var path2 = Path.Combine(SearchPath, path);
            if (File.Exists(path2)) {
                path = path2;
            }
        }

        var newImage = new RasterImageFile(path);
        ImageCache[path] = newImage;
        return newImage;
    }

    public override IEnumerable<int> GetLayers() {
        return new int[] { Layer };
    }

    private int Layer => GetLayerCode(
        2,
        0,
        Obj.Tags is not null && Obj.Tags.ContainsKey("layer") ? int.Parse(Obj.Tags.GetValue("layer")) : 0
    );
}