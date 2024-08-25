# OSMRender

OSMRender is a free and open-source renderer for OpenStreetMap data.
It can render OSM XML to PDF, PNG, SVG, or PNG or SVG tiles.
It also contains a tile server that renders the tiles on-the-fly for local use.

## Compiling

Run `dotnet build` in the OSMRenderApp directory.

## Using

To get started, download some data from the Overpass API:

```sh
wget -O helsinki.osm "https://overpass-api.de/api/map?bbox=24.94,60.16,24.96,60.18"
```

To simply view the map, the easiest way is to start the tile server:

```sh
OSMRenderApp -r Rules/OSMExport.mrules -i helsinki.osm --server
```

The data can also be converted to various image formats.

```sh
OSMRenderApp -r Rules/OSMExport.mrules -i helsinki.osm -o helsinki.pdf
OSMRenderApp -r Rules/OSMExport.mrules -i helsinki.osm -o helsinki%.png -t png
OSMRenderApp -r Rules/OSMExport.mrules -i helsinki.osm -o helsinki%.svg -t svg
```

The percent sign `%` is replaced with the zoom level when multiple zoom levels are generated (by default levels 11-18).
The PDF will contain all zoom levels as separate pages unless the file name contains `%`.

The last alternative is to generate tiles. Note that this is currently very slow!
It is faster to generate one big PNG and slice it to multiple PNGs using another program than use this.

```sh
OSMRenderApp -r Rules/OSMExport.mrules -i helsinki.osm -o helsinki-tiles/ -t pngtiles
OSMRenderApp -r Rules/OSMExport.mrules -i helsinki.osm -o helsinki-tiles/ -t svgtiles
```

## Rulesets

OSMRender is a clone of [Maperitive](http://maperitive.net) and implements the same ruleset syntax as it. Thus, most Maperitive rulesets should work straight away. However, OSMRender is not (currently) a perfect clone, and some features might be missing or only partially implemented.

## License

OSMRender is licensed under GNU GPL version 3 or later. However, the part of the program that renders vector graphics to PNGs uses a library (VectSharp.Raster) licensed under AGPLv3. Due to this reason, **the code under OSMRenderApp, as a whole, is licensed under AGPLv3.** If you only use the OSMRender library and not anything under OSMRenderApp directory, you can safely use the GPLv3 license. Also, since my code is licensed under GPLv3, you are free to make a fork of OSMRenderApp that removes that dependency if you wish.

OSMExport.mrules is a modified version of Default.mrules by Igor Brejc and is licensed under the CC BY-SA 3.0 license.

The example image below is based on OpenStreetMap data and follows their [license conditions](https://www.openstreetmap.org/copyright).

## Example

This is helsinki.osm rendered at zoom level 18.
Note that sea is not included in the data.

![Helsinki at Zoom level 18](doc/helsinki18.png)