// This file is part of OSMRender.
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

using System.Xml.Serialization;
using OSMRender.Geo;
using OSMRender.Logging;
using OSMRender.Rules;
using OsmSharp.API;

namespace OSMRenderApp;

public class Reader {

    private readonly ILogger Logger;

    public Reader(ILogger logger) {
        Logger = logger;
    }

    public (GeoDocument, OSMRender.Geo.Bounds) ReadOSM(string path) {
        Osm osm;
        GeoDocument doc;

        using (var stream = new FileStream(path, FileMode.Open)) {
            XmlSerializer serializer = new(typeof(Osm));
            osm = (Osm)(serializer.Deserialize(stream) ?? throw new NullReferenceException());
            doc = GeoDocument.FromOSM(osm);
        }

        Logger.Debug($"Found {doc.Points.Count} points");
        Logger.Debug($"Found {doc.Lines.Count} lines");
        Logger.Debug($"Found {doc.Areas.Count} areas");
        Logger.Debug($"Found {doc.Relations.Count} relations");

        var bounds = osm.Bounds is not null && osm.Bounds.MinLatitude is not null ? OSMRender.Geo.Bounds.FromOsmBounds(osm.Bounds) : doc.Bounds;

        return (doc, bounds);
    }

    public Ruleset ReadRules(string path) {
        var ruleCode = File.ReadAllText(path);
        var rules = Parser.ParseRules(ruleCode, Logger);
        return rules;
    }
}