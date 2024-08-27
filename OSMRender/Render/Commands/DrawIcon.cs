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
using VectSharp;
using BigGustave;

namespace OSMRender.Render.Commands;

public class DrawIcon(IDictionary<string, string> properties, int importance, string feature, GeoObj obj) : DrawCommand(properties, importance, feature, obj) {

    private static readonly Dictionary<string, RasterImage> ImageCache = [];
    public static string SearchPath { get; set; } = "";

    public override void Draw(PageRenderer renderer, int layer) {
        if (layer != Layer) return;
        if (!TryGetCoordinates(out var lat, out var lon)) return;

        var x = renderer.LongitudeToX(lon);
        var y = renderer.LatitudeToY(lat);
        var size = Properties.ContainsKey("icon-width") ? RenderingProperties.IconWidth.GetFor(renderer.Renderer.ZoomLevel) : 10;

        var image = GetImage(RenderingProperties.IconImage);
        if (image is null) {
            renderer.Logger.Error($"{Feature}: Icon `{Properties["icon-image"]}' does not exist");
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
        return [Layer];
    }

    private int Layer => GetLayerCode(
        2,
        LayerProperty
    );
}