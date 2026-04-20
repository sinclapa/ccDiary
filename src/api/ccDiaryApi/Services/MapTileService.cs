// <copyright file="MapTileService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using System.Globalization;
    using System.Net.Http.Json;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;

    public class MapTileService : IMapTileService
    {
        private static readonly TimeSpan TileTtl = TimeSpan.FromDays(90);
        private static readonly TimeSpan GeocodingTtl = TimeSpan.FromDays(180);
        private static readonly TimeSpan RoutingTtl = TimeSpan.FromDays(90);
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

        private static readonly Dictionary<string, string> SourceUrls =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["osm"] = "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
                ["openseamap"] = "https://tiles.openseamap.org/seamark/{z}/{x}/{y}.png",
            };

        private readonly DiaryDatabaseContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MapTileService> _logger;

        public MapTileService(
            DiaryDatabaseContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<MapTileService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<(byte[] Data, string ContentType)?> GetTileAsync(string source, int z, int x, int y)
        {
            if (!SourceUrls.TryGetValue(source, out var urlTemplate))
            {
                return null;
            }

            var cutoff = DateTime.UtcNow - TileTtl;
            var cached = _context.MapTileCache
                .Where(t => t.Source == source && t.Z == z && t.X == x && t.Y == y && t.CachedAt >= cutoff)
                .Select(t => new { t.TileData, t.ContentType })
                .FirstOrDefault();

            if (cached != null)
            {
                return (cached.TileData, cached.ContentType);
            }

            var url = urlTemplate
                .Replace("{z}", z.ToString(CultureInfo.InvariantCulture))
                .Replace("{x}", x.ToString(CultureInfo.InvariantCulture))
                .Replace("{y}", y.ToString(CultureInfo.InvariantCulture));

            try
            {
                var client = _httpClientFactory.CreateClient("MapTileProxy");
                using var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Upstream tile fetch failed. Source={Source} Z={Z} X={X} Y={Y} Status={Status}",
                        SanitizeForLog(source),
                        z,
                        x,
                        y,
                        response.StatusCode);
                    return null;
                }

                var data = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";

                await PersistTileAsync(source, z, x, y, data, contentType);
                return (data, contentType);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error fetching tile. Source={Source} Z={Z} X={X} Y={Y}", SanitizeForLog(source), z, x, y);
                return null;
            }
        }

        public async Task<(double Lat, double Lon)?> GeocodeAsync(string query)
        {
            var parsed = TryParseCoordinates(query.Trim());
            if (parsed.HasValue)
            {
                return parsed;
            }

            var normalised = query.Trim().ToLowerInvariant();
            var cutoff = DateTime.UtcNow - GeocodingTtl;

            var cached = _context.GeocodingCache
                .Where(g => g.Query == normalised && g.CachedAt >= cutoff)
                .Select(g => new { g.Lat, g.Lon })
                .FirstOrDefault();

            if (cached != null)
            {
                return (cached.Lat, cached.Lon);
            }

            var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(normalised)}&format=json&limit=1";

            try
            {
                var client = _httpClientFactory.CreateClient("MapTileProxy");
                using var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Nominatim fetch failed. Query={Query} Status={Status}", SanitizeForLog(normalised), response.StatusCode);
                    return null;
                }

                var results = await response.Content.ReadFromJsonAsync<NominatimResult[]>();
                if (results == null || results.Length == 0)
                {
                    return null;
                }

                var lat = double.Parse(results[0].Lat, CultureInfo.InvariantCulture);
                var lon = double.Parse(results[0].Lon, CultureInfo.InvariantCulture);

                await PersistGeocodingAsync(normalised, lat, lon);
                return (lat, lon);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error geocoding. Query={Query}", SanitizeForLog(normalised));
                return null;
            }
        }

        public async Task<IReadOnlyList<double[]>?> GetRouteAsync(
            double fromLat, double fromLon, double toLat, double toLon, string profile)
        {
            if (profile != "foot" && profile != "driving")
            {
                return null;
            }

            var rFromLat = Round6(fromLat);
            var rFromLon = Round6(fromLon);
            var rToLat = Round6(toLat);
            var rToLon = Round6(toLon);
            var cutoff = DateTime.UtcNow - RoutingTtl;

            var cached = _context.RoutingCache
                .Where(r => Math.Abs(r.FromLat - rFromLat) < 1e-9 && Math.Abs(r.FromLon - rFromLon) < 1e-9
                         && Math.Abs(r.ToLat - rToLat) < 1e-9 && Math.Abs(r.ToLon - rToLon) < 1e-9
                         && r.Profile == profile && r.CachedAt >= cutoff)
                .Select(r => r.RouteCoords)
                .FirstOrDefault();

            if (cached != null)
            {
                return JsonSerializer.Deserialize<List<double[]>>(cached);
            }

            var url = BuildOsrmUrl(profile, rFromLon, rFromLat, rToLon, rToLat);

            try
            {
                var client = _httpClientFactory.CreateClient("MapTileProxy");
                using var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "OSRM fetch failed. Profile={Profile} Status={Status}", SanitizeForLog(profile), response.StatusCode);
                    return null;
                }

                var data = await response.Content.ReadFromJsonAsync<OsrmResponse>();
                if (data?.Code != "Ok" || data.Routes == null || data.Routes.Length == 0)
                {
                    return null;
                }

                // OSRM returns [lon, lat] pairs; convert to [lat, lon] for Leaflet
                var coords = data.Routes[0].Geometry.Coordinates
                    .Select(c => new double[] { c[1], c[0] })
                    .ToList();

                var json = JsonSerializer.Serialize(coords);
                await PersistRoutingAsync(rFromLat, rFromLon, rToLat, rToLon, profile, json);
                return coords;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error fetching route. Profile={Profile}", SanitizeForLog(profile));
                return null;
            }
        }

        private static double Round6(double v) => Math.Round(v, 6);

        private static string SanitizeForLog(string value) => value.ReplaceLineEndings(string.Empty);

        private static string BuildOsrmUrl(string profile, double fromLon, double fromLat, double toLon, double toLat) =>
            string.Create(
                CultureInfo.InvariantCulture,
                $"https://router.project-osrm.org/route/v1/{profile}/{fromLon},{fromLat};{toLon},{toLat}?overview=full&geometries=geojson");

        private static (double Lat, double Lon)? TryParseCoordinates(string input)
        {
            // DMS: 10°00'05.0"S 39°43'11.9"E — accepts common Unicode variants for each separator:
            //   degree:  ° (U+00B0) or d
            //   minutes: ' (U+0027), ' (U+2019 right-single-quote), ′ (U+2032 prime)
            //   seconds: " (U+0022), " (U+201D right-double-quote), ″ (U+2033 double-prime)
            var dms = Regex.Match(
                input,
                "(\\d+)[\u00b0d]\\s*(\\d+)[\u0027\u2019\u2032]\\s*(\\d+\\.?\\d*)[\u0022\u201d\u2033]\\s*([NS])[,\\s]+(\\d+)[\u00b0d]\\s*(\\d+)[\u0027\u2019\u2032]\\s*(\\d+\\.?\\d*)[\u0022\u201d\u2033]\\s*([EW])",
                RegexOptions.IgnoreCase,
                RegexTimeout);
            if (dms.Success)
            {
                var lat = DmsToDegrees(dms.Groups[1].Value, dms.Groups[2].Value, dms.Groups[3].Value, dms.Groups[4].Value);
                var lon = DmsToDegrees(dms.Groups[5].Value, dms.Groups[6].Value, dms.Groups[7].Value, dms.Groups[8].Value);
                return (lat, lon);
            }

            // Decimal degrees: -10.001389, 39.719972  or  -10.001389 39.719972
            var dec = Regex.Match(input, @"^(-?\d+\.?\d*)[,\s]+(-?\d+\.?\d*)$", RegexOptions.None, RegexTimeout);
            if (dec.Success
                && double.TryParse(dec.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var dlat)
                && double.TryParse(dec.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var dlon)
                && dlat is >= -90 and <= 90
                && dlon is >= -180 and <= 180)
            {
                return (dlat, dlon);
            }

            return null;
        }

        private static double DmsToDegrees(string deg, string min, string sec, string direction)
        {
            var d = double.Parse(deg, CultureInfo.InvariantCulture);
            var m = double.Parse(min, CultureInfo.InvariantCulture);
            var s = double.Parse(sec, CultureInfo.InvariantCulture);
            var value = d + (m / 60) + (s / 3600);
            return direction.Equals("S", StringComparison.OrdinalIgnoreCase)
                   || direction.Equals("W", StringComparison.OrdinalIgnoreCase)
                ? -value
                : value;
        }

        private async Task PersistTileAsync(string source, int z, int x, int y, byte[] data, string contentType)
        {
            var existing = _context.MapTileCache
                .Where(t => t.Source == source && t.Z == z && t.X == x && t.Y == y)
                .FirstOrDefault();

            if (existing != null)
            {
                existing.TileData = data;
                existing.ContentType = contentType;
                existing.CachedAt = DateTime.UtcNow;
                _context.Update(existing);
            }
            else
            {
                _context.MapTileCache.Add(new MapTileCacheDTO
                {
                    Source = source,
                    Z = z,
                    X = x,
                    Y = y,
                    TileData = data,
                    ContentType = contentType,
                    CachedAt = DateTime.UtcNow,
                });
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist tile. Source={Source} Z={Z} X={X} Y={Y}", SanitizeForLog(source), z, x, y);
            }
        }

        private async Task PersistGeocodingAsync(string query, double lat, double lon)
        {
            var existing = _context.GeocodingCache
                .Where(g => g.Query == query)
                .FirstOrDefault();

            if (existing != null)
            {
                existing.Lat = lat;
                existing.Lon = lon;
                existing.CachedAt = DateTime.UtcNow;
                _context.Update(existing);
            }
            else
            {
                _context.GeocodingCache.Add(new GeocodingCacheDTO
                {
                    Query = query,
                    Lat = lat,
                    Lon = lon,
                    CachedAt = DateTime.UtcNow,
                });
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist geocoding result. Query={Query}", SanitizeForLog(query));
            }
        }

        private async Task PersistRoutingAsync(
            double fromLat, double fromLon, double toLat, double toLon, string profile, string json)
        {
            var existing = _context.RoutingCache
                .Where(r => Math.Abs(r.FromLat - fromLat) < 1e-9 && Math.Abs(r.FromLon - fromLon) < 1e-9
                         && Math.Abs(r.ToLat - toLat) < 1e-9 && Math.Abs(r.ToLon - toLon) < 1e-9
                         && r.Profile == profile)
                .FirstOrDefault();

            if (existing != null)
            {
                existing.RouteCoords = json;
                existing.CachedAt = DateTime.UtcNow;
                _context.Update(existing);
            }
            else
            {
                _context.RoutingCache.Add(new RoutingCacheDTO
                {
                    FromLat = fromLat,
                    FromLon = fromLon,
                    ToLat = toLat,
                    ToLon = toLon,
                    Profile = profile,
                    RouteCoords = json,
                    CachedAt = DateTime.UtcNow,
                });
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist route. Profile={Profile}", SanitizeForLog(profile));
            }
        }

        private sealed class NominatimResult
        {
            public string Lat { get; set; } = string.Empty;

            public string Lon { get; set; } = string.Empty;
        }

        private sealed class OsrmResponse
        {
            public string Code { get; set; } = string.Empty;

#pragma warning disable SA1011
            public OsrmRoute[]? Routes { get; set; }
#pragma warning restore SA1011
        }

        private sealed class OsrmRoute
        {
            public OsrmGeometry Geometry { get; set; } = new OsrmGeometry();
        }

        private sealed class OsrmGeometry
        {
            public double[][] Coordinates { get; set; } = Array.Empty<double[]>();
        }
    }
}
