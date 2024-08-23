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
    { "t|type=", "output type (pdf, png, svg, pngtiles, svgtiles) (default: pdf); when in server mode: decides the tile image file type (pngtiles, svgtiles) (default: pngtiles)", t => outputType = t },
    { "r|ruleset=", "ruleset file", r => rulesetPath = r },
    { "i|input=", ".osm input file", i => inputPath = i },
    { "o|output=", "output file", o => outputPath = o },
    { "m|min-zoom=", "min zoom level to create (default: 11)", (int z) => minZoom = z },
    { "M|max-zoom=", "max zoom level to create (default: 18)", (int z) => maxZoom = z },
    { "server", "instead of generating output, start a tile server (-o, -m, -M are ignored)", s => server = s != null },
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
var doc = reader.ReadOSM(inputPath);
rules.Apply(doc);

if (server) {
    var httpServer = new Server(doc, doc.Bounds, outputType == "svgtiles" ? Server.TileType.Svg : Server.TileType.Png, logger);
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
        generator.GenerateTiles(doc, doc.Bounds, minZoom, maxZoom, type, outputPath);
    } else {
        generator.GenerateImages(doc, doc.Bounds, minZoom, maxZoom, type, outputPath);
    }
}