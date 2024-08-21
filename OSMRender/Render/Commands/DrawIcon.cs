using OSMRender.Geo;
using VectSharp;
using BigGustave;

namespace OSMRender.Render.Commands;

public class DrawIcon : DrawCommand {

    private static Dictionary<string, RasterImage> ImageCache = new();
    public static string SearchPath { get; set; } = "";
    
    public DrawIcon(IDictionary<string, string> properties, int importance, string feature, GeoObj obj) : base(properties, importance, feature, obj) {
    }

    public override void Draw(PageRenderer renderer, int layer) {
        if (layer != Layer) return;
        if (!TryGetCoordinates(out var lat, out var lon)) return;

        var x = renderer.LongitudeToX(lon);
        var y = renderer.LatitudeToY(lat);
        var size = Properties.ContainsKey("icon-width") ? GetNum("icon-width", renderer.Renderer.ZoomLevel, 1) : 10;

        var image = GetImage(GetString("icon-image"));
        if (image is null) {
            renderer.Logger.Error($"Icon `{Properties["icon-image"]}' does not exist");
            return;
        }
        renderer.Graphics.DrawRasterImage(x - size/2, y - size/2, size, size, image);
    }

    private static RasterImage? GetImage(string path) {
        if (ImageCache.TryGetValue(path, out var image)) {
            return image;
        }

        if (!File.Exists(path)) {
            var path2 = Path.Combine(SearchPath, path);
            if (File.Exists(path2)) {
                path = path2;
            } else {
                return null;
            }
        }

        using var stream = File.OpenRead(path);
        Png png = Png.Open(stream);
        
        byte[] data = new byte[png.Width * png.Height * 4];
        for (int x = 0; x < png.Width; x++) {
            for (int y = 0; y < png.Height; y++) {
                var pixel = png.GetPixel(x, y);
                data[4*y*png.Width + 4*x] = pixel.R;
                data[4*y*png.Width + 4*x + 1] = pixel.G;
                data[4*y*png.Width + 4*x + 2] = pixel.B;
                data[4*y*png.Width + 4*x + 3] = pixel.A;
            }
        }

        var newImage = new RasterImage(data, png.Width, png.Height, PixelFormats.RGBA, true);
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