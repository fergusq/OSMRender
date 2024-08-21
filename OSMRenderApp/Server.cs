using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using OSMRender.Geo;
using OSMRender.Logging;
using OSMRender.Render;
using VectSharp.SVG;
using VectSharp.Raster;
using System.Globalization;
using System.Diagnostics;

namespace OSMRenderApp;

public partial class Server {
    private HttpListener? Listener;
    private readonly GeoDocument Document;
    private readonly Bounds Bounds;
    private readonly Logger Logger;
    private readonly string IndexPage;
    private static readonly Regex TILE_URL_PATTERN = TileUrlPattern();
    private const string PAGE = """
<!doctype html>
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"
    integrity="sha256-p4NxAoJBhIIN+hmNHrzRCf9tD/miZyoHS5obTRR9BMY="
    crossorigin=""/>
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"
    integrity="sha256-20nQCchB9co0qIjJZRGuk2/Z9VM+kNiyxNV1lvTlZBo="
    crossorigin=""></script>

<style>
#map { height: 100vh; }
body { margin: 0; }
</style>

<div id="map"></div>

<script>
var map = L.map('map').setView([<LAT>, <LON>], 13);
L.tileLayer('{z}/{x}/{y}.<FILETYPE>', {
    minZoom: 11,
    maxZoom: 18,
    attribution: 'OSM Export'
}).addTo(map);
</script>
""";

    public enum TileType {
        Png,
        Svg,
    }

    public Server(GeoDocument doc, Bounds bounds, TileType type, Logger logger) {
        Document = doc;
        Bounds = bounds;
        Logger = logger;
        IndexPage = PAGE
            .Replace("<LAT>", ((bounds.MinLatitude + bounds.MaxLatitude) / 2).ToString(CultureInfo.InvariantCulture))
            .Replace("<LON>", ((bounds.MinLongitude + bounds.MaxLongitude) / 2).ToString(CultureInfo.InvariantCulture))
            .Replace("<FILETYPE>", type switch { TileType.Png => "png", TileType.Svg => "svg", _ => throw new NotImplementedException() });
    }

    public void StartServer(string url) {
        Listener = new HttpListener();
        Listener.Prefixes.Add(url);
        Listener.Start();
        Console.WriteLine($"Server running at {url}");
        var listenTask = HandleIncomingConnections();
        listenTask.GetAwaiter().GetResult();
    }

    public async Task HandleIncomingConnections() {
        if (Listener == null) {
            throw new NullReferenceException();
        }
        while (true) {
            try {
                var ctx = await Listener.GetContextAsync();
                var req = ctx.Request;
                var resp = ctx.Response;
                if (req.Url == null) continue;
                if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/") {
                    byte[] data = Encoding.UTF8.GetBytes(IndexPage);
                    resp.ContentType = "text/html; charset=utf8";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;
                    await resp.OutputStream.WriteAsync(data);
                    resp.Close();
                    continue;
                }
                var match = TILE_URL_PATTERN.Match(req.Url.AbsolutePath);
                if (req.HttpMethod == "GET" && match.Success) {
                    int zoom = int.Parse(match.Groups[1].Value);
                    int x = int.Parse(match.Groups[2].Value);
                    int y = int.Parse(match.Groups[3].Value);
                    string type = match.Groups[4].Value;

                    var renderer = new Renderer(Bounds, zoom, Logger);
                    var page = renderer.RenderTile(Document, x, y);

                    byte[] data;
                    if (type == "svg") {
                        var xml = SVGContextInterpreter.SaveAsSVG(page);
                        data = Encoding.UTF8.GetBytes(xml.OuterXml);
                        resp.ContentType = "image/svg+xml; charset=utf8";
                        resp.ContentEncoding = Encoding.UTF8;
                    } else if (type == "png") {
                        using (MemoryStream stream = new()) {
                            Raster.SaveAsPNG(page, stream);
                            data = stream.ToArray();
                        }
                        resp.ContentType = "image/png; charset=binary";
                        resp.ContentEncoding = Encoding.UTF8;
                    } else {
                        await Error(resp, 400);
                        continue;
                    }

                    resp.ContentLength64 = data.LongLength;
                    await resp.OutputStream.WriteAsync(data);
                    resp.Close();
                } else {
                    await Error(resp, 404);
                    resp.Close();
                }
            } catch (HttpListenerException e) {
                Debug.WriteLine(e.ToString());
            }
        }
    }

    private static async Task Error(HttpListenerResponse resp, int statusCode) {
        byte[] data = Encoding.UTF8.GetBytes($"Error {statusCode}");
        resp.ContentType = "text/plain";
        resp.ContentEncoding = Encoding.UTF8;
        await resp.OutputStream.WriteAsync(data);
        resp.Close();
    }

    [GeneratedRegex(@"^/(\d+)/(\d+)/(\d+).(png|svg)$")]
    private static partial Regex TileUrlPattern();
}