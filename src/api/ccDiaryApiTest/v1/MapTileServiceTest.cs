// <copyright file="MapTileServiceTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using Moq.Protected;

    [TestClass]
    public class MapTileServiceTest
    {
        private static readonly List<double[]> SampleRouteCoords =
            new List<double[]> { new double[] { 51.5, -0.1 }, new double[] { 48.8, 2.3 } };

        [TestMethod]
        public async Task GetTileAsync_ReturnsNull_ForUnknownSource()
        {
            // Arrange
            var db = GetMemoryContext();
            var service = CreateService(db);

            // Act
            var result = await service.GetTileAsync("unknown", 1, 0, 0);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetTileAsync_ReturnsCachedTile_WhenFreshCacheExists()
        {
            // Arrange
            var db = GetMemoryContext();
            var tileData = new byte[] { 0x89, 0x50 };
            db.MapTileCache.Add(new MapTileCacheDto
            {
                Source = "osm",
                Z = 10,
                X = 512,
                Y = 342,
                TileData = tileData,
                ContentType = "image/png",
                CachedAt = DateTime.UtcNow,
            });
            db.SaveChanges();

            var factory = new Mock<IHttpClientFactory>();
            var service = CreateService(db, factory);

            // Act
            var result = await service.GetTileAsync("osm", 10, 512, 342);

            // Assert
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(tileData, result.Value.Data);
            factory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task GetTileAsync_TreatsExpiredRowAsCacheMiss()
        {
            // Arrange
            var db = GetMemoryContext();
            db.MapTileCache.Add(new MapTileCacheDto
            {
                Source = "osm",
                Z = 10,
                X = 1,
                Y = 1,
                TileData = new byte[] { 0x01 },
                ContentType = "image/png",
                CachedAt = DateTime.UtcNow.AddDays(-91),
            });
            db.SaveChanges();

            var freshData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
            var service = CreateService(db, MockHttpFactory(HttpStatusCode.OK, freshData, "image/png"));

            // Act
            var result = await service.GetTileAsync("osm", 10, 1, 1);

            // Assert
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(freshData, result.Value.Data);
        }

        [TestMethod]
        public async Task GetTileAsync_ReturnsNull_WhenUpstreamFails()
        {
            // Arrange
            var db = GetMemoryContext();
            var service = CreateService(db, MockHttpFactory(HttpStatusCode.ServiceUnavailable));

            // Act
            var result = await service.GetTileAsync("osm", 10, 512, 342);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetTileAsync_PersistsTileAndReturnsBytesOnCacheMiss()
        {
            // Arrange
            var db = GetMemoryContext();
            var tileData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
            var service = CreateService(db, MockHttpFactory(HttpStatusCode.OK, tileData, "image/png"));

            // Act
            var result = await service.GetTileAsync("osm", 10, 5, 5);

            // Assert
            Assert.IsNotNull(result);
            CollectionAssert.AreEqual(tileData, result.Value.Data);
        }

        [TestMethod]
        public async Task GeocodeAsync_ParsesDmsCoordinates_WithoutNetworkCall()
        {
            // Arrange
            var db = GetMemoryContext();
            var factory = new Mock<IHttpClientFactory>();
            var service = CreateService(db, factory);

            // Act — 10°00'05.0"S 39°43'11.9"E → lat=-10.001389, lon=39.719972
            var result = await service.GeocodeAsync(@"10°00'05.0""S 39°43'11.9""E");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(-10.001389, result.Value.Lat, 0.0001);
            Assert.AreEqual(39.719972, result.Value.Lon, 0.0001);
            factory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task GeocodeAsync_ParsesDecimalCoordinates_WithoutNetworkCall()
        {
            // Arrange
            var db = GetMemoryContext();
            var factory = new Mock<IHttpClientFactory>();
            var service = CreateService(db, factory);

            // Act
            var result = await service.GeocodeAsync("-10.001389, 39.719972");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(-10.001389, result.Value.Lat, 0.0001);
            Assert.AreEqual(39.719972, result.Value.Lon, 0.0001);
            factory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task GeocodeAsync_ReturnsCachedResult_WhenFreshCacheExists()
        {
            // Arrange
            var db = GetMemoryContext();
            db.GeocodingCache.Add(new GeocodingCacheDto
            {
                Query = "london",
                Lat = 51.5074,
                Lon = -0.1278,
                CachedAt = DateTime.UtcNow,
            });
            db.SaveChanges();

            var factory = new Mock<IHttpClientFactory>();
            var service = CreateService(db, factory);

            // Act
            var result = await service.GeocodeAsync("London");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(51.5074, result.Value.Lat, 0.0001);
            Assert.AreEqual(-0.1278, result.Value.Lon, 0.0001);
            factory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task GeocodeAsync_NormalisesQueryBeforeLookup()
        {
            // Arrange
            var db = GetMemoryContext();
            db.GeocodingCache.Add(new GeocodingCacheDto
            {
                Query = "paris, france",
                Lat = 48.8566,
                Lon = 2.3522,
                CachedAt = DateTime.UtcNow,
            });
            db.SaveChanges();

            var factory = new Mock<IHttpClientFactory>();
            var service = CreateService(db, factory);

            // Act
            var result = await service.GeocodeAsync("  Paris, France  ");

            // Assert
            Assert.IsNotNull(result);
            factory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task GeocodeAsync_TreatsExpiredRowAsCacheMiss()
        {
            // Arrange
            var db = GetMemoryContext();
            db.GeocodingCache.Add(new GeocodingCacheDto
            {
                Query = "berlin",
                Lat = 52.52,
                Lon = 13.405,
                CachedAt = DateTime.UtcNow.AddDays(-181),
            });
            db.SaveChanges();

            var nominatimResponse = new[] { new { lat = "52.5200", lon = "13.4050" } };
            var service = CreateService(db, MockHttpFactory(HttpStatusCode.OK, nominatimResponse));

            // Act
            var result = await service.GeocodeAsync("berlin");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(52.52, result.Value.Lat, 0.01);
        }

        [TestMethod]
        public async Task GeocodeAsync_ReturnsNull_WhenUpstreamReturnsEmptyArray()
        {
            // Arrange
            var db = GetMemoryContext();
            var service = CreateService(db, MockHttpFactory(HttpStatusCode.OK, Array.Empty<object>()));

            // Act
            var result = await service.GeocodeAsync("zzz_nonexistent_xyz");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GeocodeAsync_PersistsResult_OnCacheMiss()
        {
            // Arrange
            var db = GetMemoryContext();
            var nominatim = new[] { new { lat = "51.5074", lon = "-0.1278" } };
            var service = CreateService(db, MockHttpFactory(HttpStatusCode.OK, nominatim));

            // Act
            var result = await service.GeocodeAsync("london");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, db.GeocodingCache.Count());
            var row = db.GeocodingCache.Single();
            Assert.AreEqual("london", row.Query);
            Assert.AreEqual(51.5074, row.Lat, 0.0001);
        }

        [TestMethod]
        public async Task GetRouteAsync_ReturnsNull_ForInvalidProfile()
        {
            // Arrange
            var db = GetMemoryContext();
            var service = CreateService(db);

            // Act
            var result = await service.GetRouteAsync(51.5, -0.1, 48.8, 2.3, "bike");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetRouteAsync_ReturnsCachedRoute_WhenFreshCacheExists()
        {
            // Arrange
            var db = GetMemoryContext();
            db.RoutingCache.Add(new RoutingCacheDto
            {
                FromLat = 51.5,
                FromLon = -0.1,
                ToLat = 48.8,
                ToLon = 2.3,
                Profile = "driving",
                RouteCoords = JsonSerializer.Serialize(SampleRouteCoords),
                CachedAt = DateTime.UtcNow,
            });
            db.SaveChanges();

            var factory = new Mock<IHttpClientFactory>();
            var service = CreateService(db, factory);

            // Act
            var result = await service.GetRouteAsync(51.5, -0.1, 48.8, 2.3, "driving");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            factory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task GetRouteAsync_RoundsCoordinatesToSixDecimalPlaces()
        {
            // Arrange
            var db = GetMemoryContext();
            db.RoutingCache.Add(new RoutingCacheDto
            {
                FromLat = 51.500001,
                FromLon = -0.100001,
                ToLat = 48.800001,
                ToLon = 2.300001,
                Profile = "foot",
                RouteCoords = JsonSerializer.Serialize(SampleRouteCoords),
                CachedAt = DateTime.UtcNow,
            });
            db.SaveChanges();

            var factory = new Mock<IHttpClientFactory>();
            var service = CreateService(db, factory);

            // Act — slightly different coords that round to the same 6dp values
            var result = await service.GetRouteAsync(51.5000013, -0.1000013, 48.8000013, 2.3000013, "foot");

            // Assert
            Assert.IsNotNull(result);
            factory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task GetRouteAsync_ReturnsNull_WhenUpstreamFails()
        {
            // Arrange
            var db = GetMemoryContext();
            var service = CreateService(db, MockHttpFactory(HttpStatusCode.ServiceUnavailable));

            // Act
            var result = await service.GetRouteAsync(51.5, -0.1, 48.8, 2.3, "driving");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetRouteAsync_PersistsRoute_OnCacheMiss()
        {
            // Arrange
            var db = GetMemoryContext();
            var osrm = new
            {
                code = "Ok",
                routes = new[]
                {
                    new
                    {
                        geometry = new
                        {
                            coordinates = new[] { new[] { -0.1, 51.5 }, new[] { 2.3, 48.8 } },
                        },
                    },
                },
            };
            var service = CreateService(db, MockHttpFactory(HttpStatusCode.OK, osrm));

            // Act
            var result = await service.GetRouteAsync(51.5, -0.1, 48.8, 2.3, "driving");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, db.RoutingCache.Count());
            var row = db.RoutingCache.Single();
            Assert.AreEqual("driving", row.Profile);
        }

        [TestMethod]
        public async Task GetRouteAsync_ReturnsNull_WhenOsrmResponseCodeIsNotOk()
        {
            // Arrange
            var db = GetMemoryContext();
#pragma warning disable SA1011
            object[]? noRoutes = null;
#pragma warning restore SA1011
            var osrm = new { code = "NoRoute", routes = noRoutes };
            var service = CreateService(db, MockHttpFactory(HttpStatusCode.OK, osrm));

            // Act
            var result = await service.GetRouteAsync(51.5, -0.1, 48.8, 2.3, "driving");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetRouteAsync_UpdatesExpiredCacheRow()
        {
            // Arrange — seed an expired row so PersistRoutingAsync takes the update branch
            var db = GetMemoryContext();
            db.RoutingCache.Add(new RoutingCacheDto
            {
                FromLat = 51.5,
                FromLon = -0.1,
                ToLat = 48.8,
                ToLon = 2.3,
                Profile = "driving",
                RouteCoords = "[]",
                CachedAt = DateTime.UtcNow.AddDays(-200),
            });
            db.SaveChanges();

            var osrm = new
            {
                code = "Ok",
                routes = new[]
                {
                    new { geometry = new { coordinates = new[] { new[] { -0.1, 51.5 }, new[] { 2.3, 48.8 } } } },
                },
            };
            var service = CreateService(db, MockHttpFactory(HttpStatusCode.OK, osrm));

            // Act
            var result = await service.GetRouteAsync(51.5, -0.1, 48.8, 2.3, "driving");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, db.RoutingCache.Count());
            Assert.AreNotEqual("[]", db.RoutingCache.Single().RouteCoords);
        }

        private static DiaryDatabaseContext GetMemoryContext()
        {
            var options = new DbContextOptionsBuilder<DiaryDatabaseContext>()
                .UseInMemoryDatabase(databaseName: "MapTileServiceTest_" + Guid.NewGuid())
                .Options;
            return new DiaryDatabaseContext(options);
        }

        private static MapTileService CreateService(
            DiaryDatabaseContext db,
            Mock<IHttpClientFactory>? factoryMock = null)
        {
            return new MapTileService(
                db,
                factoryMock?.Object ?? new Mock<IHttpClientFactory>().Object,
                NullLogger<MapTileService>.Instance);
        }

#pragma warning disable SA1011
        private static Mock<IHttpClientFactory> MockHttpFactory(HttpStatusCode status, byte[]? body = null, string? contentType = null)
#pragma warning restore SA1011
        {
            var handler = new Mock<HttpMessageHandler>();
            var content = body != null
                ? new ByteArrayContent(body)
                : new ByteArrayContent(Array.Empty<byte>());
            if (contentType != null)
            {
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            }

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(status) { Content = content });

            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(handler.Object) { BaseAddress = new Uri("https://example.com") });
            return factory;
        }

        private static Mock<IHttpClientFactory> MockHttpFactory<T>(HttpStatusCode status, T jsonBody)
        {
            var json = JsonSerializer.Serialize(jsonBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(status) { Content = content });

            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(handler.Object) { BaseAddress = new Uri("https://example.com") });
            return factory;
        }
    }
}
