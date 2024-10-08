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

using System.Xml.Linq;
using OSMRender.Geo;
using OSMRender.Logging;
using OSMRender.OSM;
using OSMRender.Rules;

namespace OSMRenderApp;

public class Reader {

    private readonly ILogger Logger;

    public Reader(ILogger logger) {
        Logger = logger;
    }

    public GeoDocument ReadOSM(string path) {
        GeoDocument doc = OsmXmlParser.XmlToGeoDocument(XDocument.Load(path), Logger);

        Logger.Debug($"Found {doc.Points.Count} points");
        Logger.Debug($"Found {doc.Lines.Count} lines");
        Logger.Debug($"Found {doc.Areas.Count} areas");
        Logger.Debug($"Found {doc.Relations.Count} relations");

        return doc;
    }

    public Ruleset ReadRules(string path) {
        var ruleCode = File.ReadAllText(path);
        var rules = Parser.ParseRules(ruleCode, Logger);
        return rules;
    }
}