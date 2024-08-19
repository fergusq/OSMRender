using System.Xml.Serialization;
using OSMRender.Geo;
using OSMRender.Render;
using OSMRender.Rules;
using OsmSharp.API;
using VectSharp;
using VectSharp.Raster;

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

Console.WriteLine($"Found {doc.Points.Count} points");
Console.WriteLine($"Found {doc.Lines.Count} lines");
Console.WriteLine($"Found {doc.Areas.Count} areas");
Console.WriteLine($"Found {doc.Relations.Count} relations");

var ruleCode = File.ReadAllText("OSMExport.mrules");
var rules = Parser.ParseRules(ruleCode);

rules.Apply(doc);

for (int zoomLevel = 11; zoomLevel <= 18; zoomLevel++) {
    var renderer = new Renderer(
        osm.Bounds is not null && osm.Bounds.MinLatitude is not null ? OSMRender.Geo.Bounds.FromOsmBounds(osm.Bounds) : doc.Bounds,
        zoomLevel
    );
    var tiles = renderer.Render(doc);
    foreach (var pair in tiles) {
        var (x, y) = pair.Key;
        var tile = pair.Value;
        var document = new Document();
        document.Pages.Add(tile);
        var path = Path.Combine("tiles", $"{zoomLevel}", $"{x}");
        Directory.CreateDirectory(path);
        var fileName = Path.Combine(path, $"{y}.png");
        using var stream = new FileStream(fileName, FileMode.Create);
        Raster.SaveAsPNG(tile, stream);
    }
}