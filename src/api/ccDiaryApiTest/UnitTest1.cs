using ccDiaryApi.Controllers.v1;
using Microsoft.Extensions.Logging;
using Moq;

namespace ccDiaryApiTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            // Arrange                  
            var loggerMock = new Mock<ILogger<WeatherForecastController>>();
            
            var controller = new WeatherForecastController(loggerMock.Object);

            // Act
            var response = controller.Get();
        }
    }
}