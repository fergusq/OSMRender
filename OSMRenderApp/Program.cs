using OSMRender.Logging;
using Mono.Options;
using OSMRenderApp;
using OSMRender.Render.Commands;

/** Options **/
var verbosity = 0;
var shouldShowHelp = false;
var rulesetPath = "";
var inputPath = "";
var outputPath = "";
var logPath = "";
var minZoom = 11;
var maxZoom = 18;
var outputType = "pdf";
var server = false;
var port = "8000";

var options = new OptionSet { 
    { "t|type=", "output type (pdf, png, svg, pngtiles, svgtiles) (default: pdf)", t => outputType = t },
    { "r|ruleset=", "ruleset file", r => rulesetPath = r },
    { "i|input=", ".osm input file", i => inputPath = i },
    { "o|output=", "output file", o => outputPath = o },
    { "m|min-zoom=", "min zoom level to create (default: 11)", (int z) => minZoom = z },
    { "M|max-zoom=", "max zoom level to create (default: 18)", (int z) => maxZoom = z },
    { "server", "instead of generating output, start a tile server (-t, -o, -m, -M are ignored)", s => server = s != null },
    { "P|port=", "tile server port (default: 8000)", p => port = p },
    { "L|log=", "log file path", l => logPath = l },
    { "v", "increase debug message verbosity", v => { if (v != null) ++verbosity; } },
    { "h|help", "show this message and exit", h => shouldShowHelp = h != null },
};

var exe = AppDomain.CurrentDomain.FriendlyName;
List<string> extra;
try {
    extra = options.Parse(args);
} catch (OptionException e) {
    Console.Write ($"{exe}: ");
    Console.WriteLine (e.Message);
    Console.WriteLine ($"Try `{exe} --help' for more information.");
    return;
}

if (shouldShowHelp || rulesetPath == "" || inputPath == "" || outputPath == "" && !server) {
    Console.WriteLine ($"Usage: {exe} [OPTIONS] -r rules.mrules -i input.osm -o output.pdf");
    Console.WriteLine ($"       {exe} [OPTIONS] -r rules.mrules -i input.osm --server");
    Console.WriteLine ("Renders a given OSM XML file to an image or tileset.");
    Console.WriteLine ();
    Console.WriteLine ("Options:");
    options.WriteOptionDescriptions (Console.Out);
    return;
}

using Logger logger = (logPath is null || logPath == "") ? new ConsoleLogger() : new FileLogger(logPath);
logger.Level = verbosity == 0 ? Logger.LoggingLevel.Info : Logger.LoggingLevel.Debug;

Reader reader = new(logger);

DrawIcon.SearchPath = Path.GetDirectoryName(rulesetPath) ?? "";
var rules = reader.ReadRules(rulesetPath);
var (doc, bounds) = reader.ReadOSM(inputPath);
rules.Apply(doc);

if (server) {
    var httpServer = new Server(doc, bounds, logger);
    httpServer.StartServer($"http://localhost:{port}/");
} else {
    Generator generator = new(logger);

    Generator.OutputType type = outputType switch {
        "pdf" => Generator.OutputType.Pdf,
        "png" => Generator.OutputType.Png,
        "svg" => Generator.OutputType.Svg,
        "pngtiles" => Generator.OutputType.Png,
        "svgtiles" => Generator.OutputType.Svg,
        _ => throw new NotImplementedException($"unknown output type {outputType}"),
    };

    if (outputType == "pngtiles" || outputType == "svgtiles") {
        generator.GenerateTiles(doc, bounds, type, outputPath);
    } else {
        generator.GeneratePDF(doc, bounds, type, outputPath);
    }
}