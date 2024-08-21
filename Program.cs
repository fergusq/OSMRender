using System.Xml.Serialization;
using OSMRender.Geo;
using OSMRender.Render;
using OsmSharp.API;
using VectSharp;
using VectSharp.PDF;
using VectSharp.SVG;
using VectSharp.Raster;
using OSMRender.Logging;

/*using VectSharp;
using VectSharp.PDF;
// ...*/
/*void Test() {
Document doc = new Document();

doc.Pages.Add(new Page(1000, 1000));

Graphics gpr = doc.Pages.Last().Graphics;
GraphicsPath path = new GraphicsPath();
path.MoveTo(100, 100);
path.LineTo(200, 300);
path.LineTo(1200, 1200);
gpr.StrokePath(path, Colour.FromRgb(1, 1, 1), 10);

//...
doc.SaveAsPDF(@"Sample.pdf");
}
Test();*/
Osm osm;
GeoDocument doc;
using (var stream = new FileStream("export.osm", FileMode.Open)) {
    XmlSerializer serializer = new(typeof(Osm));
    osm = (Osm?)serializer.Deserialize(stream);
    if (osm is null) {
        return;
    }
    doc = GeoDocument.FromOSM(osm);
}

using var logger = new FileLogger("OSMRender.log");
logger.Level = FileLogger.LoggingLevel.Debug;

logger.Debug($"Found {doc.Points.Count} points");
logger.Debug($"Found {doc.Lines.Count} lines");
logger.Debug($"Found {doc.Areas.Count} areas");
logger.Debug($"Found {doc.Relations.Count} relations");


var ruleCode = File.ReadAllText("OSMExport.mrules");
var rules = OSMRender.Rules.Parser.ParseRules(ruleCode, logger);

rules.Apply(doc);

void GenerateTiles() {
    for (int zoomLevel = 11; zoomLevel <= 17; zoomLevel++) {
        Console.WriteLine($"Rendering zoom level {zoomLevel}");
        var renderer = new Renderer(
            osm.Bounds is not null && osm.Bounds.MinLatitude is not null ? OSMRender.Geo.Bounds.FromOsmBounds(osm.Bounds) : doc.Bounds,
            zoomLevel
        );
        var tiles = renderer.Render(doc);
        var total = tiles.Count;
        int i = 0;
        foreach (var pair in tiles) {
            if (i++%100 == 0) {
                Console.Write($"\rSaving {100*i/total}%");
            }
            var (x, y) = pair.Key;
            var tile = pair.Value;
            var document = new Document();
            document.Pages.Add(tile);
            var path = Path.Combine("tiles", $"{zoomLevel}", $"{x}");
            Directory.CreateDirectory(path);
            //var fileName = Path.Combine(path, $"{y}.png");
            //using var stream = new FileStream(fileName, FileMode.Create);
            //Raster.SaveAsPNG(tile, stream);
            SVGContextInterpreter.SaveAsSVG(tile, Path.Combine(path, $"{y}.svg"), SVGContextInterpreter.TextOptions.DoNotEmbed, filterOption: new SVGContextInterpreter.FilterOption(SVGContextInterpreter.FilterOption.FilterOperations.NeverRasteriseAndIgnore, 0, false));
        }
        Console.WriteLine($"\rSaving 100%");
    }
}

void GeneratePDF() {
    var document = new Document();
    for (int zoomLevel = 11; zoomLevel <= 18; zoomLevel++) {
        Console.WriteLine($"Rendering zoom level {zoomLevel}");
        var renderer = new Renderer(
            osm.Bounds is not null && osm.Bounds.MinLatitude is not null ? OSMRender.Geo.Bounds.FromOsmBounds(osm.Bounds) : doc.Bounds,
            zoomLevel
        );
        var tiles = renderer.Render(doc, tiled: false);
        foreach (var pair in tiles) {
            document.Pages.Add(pair.Value);
        }
        //SVGContextInterpreter.SaveAsSVG(tiles.Values.First(), $"export{zoomLevel}.svg");
    }
    document.SaveAsPDF($"export.pdf");
}

GeneratePDF();