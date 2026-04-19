// <copyright file="MapTileControllerTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using ccDiaryApi.Controllers.v1;
    using ccDiaryApi.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Moq;

    [TestClass]
    public class MapTileControllerTest
    {
        private static readonly List<double[]> SampleRouteCoords =
            new List<double[]> { new double[] { 51.5, -0.1 }, new double[] { 48.8, 2.3 } };

        [TestMethod]
        public async Task Tile_ReturnsFileResult_WhenServiceReturnsData()
        {
            // Arrange
            var data = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
            var service = new Mock<IMapTileService>();
            service.Setup(s => s.GetTileAsync("osm", 10, 512, 342))
                .ReturnsAsync((data, "image/png"));
            var controller = CreateController(service.Object);

            // Act
            var result = await controller.Tile("osm", 10, 512, 342);

            // Assert
            Assert.IsInstanceOfType(result, typeof(FileContentResult));
            var file = (FileContentResult)result;
            Assert.AreEqual("image/png", file.ContentType);
            CollectionAssert.AreEqual(data, file.FileContents);
        }

        [TestMethod]
        public async Task Tile_ReturnsNotFound_WhenServiceReturnsNull()
        {
            // Arrange
            var service = new Mock<IMapTileService>();
            service.Setup(s => s.GetTileAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(default((byte[], string)?));
            var controller = CreateController(service.Object);

            // Act
            var result = await controller.Tile("unknown", 1, 0, 0);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
            Assert.AreEqual("no-store", controller.Response.Headers.CacheControl.ToString());
        }

        [TestMethod]
        public async Task Geocode_ReturnsLatLon_WhenServiceReturnsCoords()
        {
            // Arrange
            var service = new Mock<IMapTileService>();
            service.Setup(s => s.GeocodeAsync("london"))
                .ReturnsAsync((51.5074, -0.1278));
            var controller = CreateController(service.Object);

            // Act
            var result = await controller.Geocode("london");

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        }

        [TestMethod]
        public async Task Geocode_ReturnsNotFound_WhenServiceReturnsNull()
        {
            // Arrange
            var service = new Mock<IMapTileService>();
            service.Setup(s => s.GeocodeAsync(It.IsAny<string>()))
                .ReturnsAsync(default((double, double)?));
            var controller = CreateController(service.Object);

            // Act
            var result = await controller.Geocode("nonexistent place xyz");

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
            Assert.AreEqual("no-store", controller.Response.Headers.CacheControl.ToString());
        }

        [TestMethod]
        public async Task Geocode_ReturnsBadRequest_WhenQueryIsEmpty()
        {
            // Arrange
            var service = new Mock<IMapTileService>();
            var controller = CreateController(service.Object);

            // Act
            var result = await controller.Geocode(string.Empty);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestResult));
        }

        [TestMethod]
        public async Task Geocode_ReturnsBadRequest_WhenQueryIsWhitespace()
        {
            // Arrange
            var service = new Mock<IMapTileService>();
            var controller = CreateController(service.Object);

            // Act
            var result = await controller.Geocode("   ");

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestResult));
        }

        [TestMethod]
        public async Task Route_ReturnsCoords_WhenServiceReturnsRoute()
        {
            // Arrange
            var service = new Mock<IMapTileService>();
            service.Setup(s => s.GetRouteAsync(51.5, -0.1, 48.8, 2.3, "driving"))
                .ReturnsAsync(SampleRouteCoords);
            var controller = CreateController(service.Object);

            // Act
            var result = await controller.Route(51.5, -0.1, 48.8, 2.3, "driving");

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        }

        [TestMethod]
        public async Task Route_ReturnsNotFound_WhenServiceReturnsNull()
        {
            // Arrange
            var service = new Mock<IMapTileService>();
            service.Setup(s => s.GetRouteAsync(
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<string>()))
                .ReturnsAsync((IReadOnlyList<double[]>?)null);
            var controller = CreateController(service.Object);

            // Act
            var result = await controller.Route(51.5, -0.1, 48.8, 2.3, "invalid");

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        private static MapTileController CreateController(IMapTileService service)
        {
            var controller = new MapTileController(service);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            };
            return controller;
        }
    }
}
