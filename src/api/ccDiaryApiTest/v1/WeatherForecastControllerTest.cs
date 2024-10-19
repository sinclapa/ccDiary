// <copyright file="WeatherForecastControllerTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using ccDiaryApi.Controllers.v1;
    using Microsoft.Extensions.Logging;
    using Moq;

    [TestClass]
    public class WeatherForecastControllerTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<WeatherForecastController>>();
            var controller = new WeatherForecastController(loggerMock.Object);

            // Act
            var response = controller.Get();

            // Assert
            Assert.AreEqual(5, response.Count());
        }

        [TestMethod]
        public void Tes()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<WeatherForecastController>>();
            var controller = new WeatherForecastController(loggerMock.Object);

            // Act
            var response = controller.Get();

            // Assert
            Assert.AreEqual(5, response.Count());
            Assert.AreEqual(32 + (int)(response.First().TemperatureC / 0.5556), response.First().TemperatureF);
        }
    }
}