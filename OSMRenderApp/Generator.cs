using OSMRender.Geo;
using OSMRender.Logging;
using OSMRender.Render;
using VectSharp;
using VectSharp.PDF;
using VectSharp.Raster;
using VectSharp.SVG;

namespace OSMRenderApp;

public class Generator {
    public enum OutputType {
        Svg,
        Png,
        Pdf,
    }

    private readonly Logger Logger;

    public Generator(Logger logger) {
        Logger = logger;
    }

    public void GenerateTiles(GeoDocument doc, Bounds bounds, OutputType outputType, string path) {
        for (int zoomLevel = 11; zoomLevel <= 17; zoomLevel++) {
            Logger.Info($"Rendering zoom level {zoomLevel}");
            var renderer = new Renderer(bounds, zoomLevel, Logger);
            var tiles = renderer.Render(doc);
            var total = tiles.Count;
            int i = 0;
            foreach (var pair in tiles) {
                if (i++%100 == 0) {
                    Console.Write($"\rSaving {100*i/total}%");
                }
                var (x, y) = pair.Key;
                var tile = pair.Value;
                var dir = Path.Combine(path, $"{zoomLevel}", $"{x}");
                Directory.CreateDirectory(dir);
                //
                if (outputType == OutputType.Svg) {
                    SVGContextInterpreter.SaveAsSVG(
                        tile,
                        Path.Combine(dir, $"{y}.svg"),
                        SVGContextInterpreter.TextOptions.DoNotEmbed,
                        filterOption: new SVGContextInterpreter.FilterOption(SVGContextInterpreter.FilterOption.FilterOperations.NeverRasteriseAndIgnore, 0, false)
                    );
                } else if (outputType == OutputType.Png) {
                    var fileName = Path.Combine(dir, $"{y}.png");
                    using var stream = new FileStream(fileName, FileMode.Create);
                    Raster.SaveAsPNG(tile, stream);
                } else {
                    throw new NotImplementedException($"output type {outputType} is not supported for tile generation");
                }
            }
            Console.WriteLine($"\rSaving 100%");
        }
    }

    public void GeneratePDF(GeoDocument doc, Bounds bounds, OutputType outputType, string path) {
        Action<Document, string> save = outputType switch
        {
            OutputType.Pdf => (d, f) => d.SaveAsPDF(f),
            OutputType.Png => (d, f) => Raster.SaveAsPNG(d.Pages.Last(), f),
            OutputType.Svg => (d, f) => SVGContextInterpreter.SaveAsSVG(
                            d.Pages.Last(),
                            f,
                            SVGContextInterpreter.TextOptions.DoNotEmbed,
                            filterOption: new SVGContextInterpreter.FilterOption(SVGContextInterpreter.FilterOption.FilterOperations.NeverRasteriseAndIgnore, 0, false)
                        ),
            _ => throw new NotImplementedException(),
        };
        var document = new Document();
        for (int zoomLevel = 11; zoomLevel <= 18; zoomLevel++) {
            Logger.Info($"Rendering zoom level {zoomLevel}");
            var renderer = new Renderer(bounds, zoomLevel, Logger);
            var tiles = renderer.Render(doc, tiled: false);
            foreach (var pair in tiles) {
                document.Pages.Add(pair.Value);
            }
            if (path.Contains('%')) {
                save.Invoke(document, path.Replace("%", zoomLevel.ToString()));
                document = new();
            }
        }
        if (!path.Contains('%')) {
            save.Invoke(document, path);
        }
    }
}