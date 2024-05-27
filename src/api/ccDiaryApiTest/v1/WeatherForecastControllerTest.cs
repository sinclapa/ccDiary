using ccDiaryApi.Controllers.v1;
using Microsoft.Extensions.Logging;
using Moq;

namespace ccDiaryApiTest.v1
{
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
            Assert.AreEqual(5, response.Count());
        }
    }
}