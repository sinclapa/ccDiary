// <copyright file="AppInfoControllerTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using ccDiaryApi.Controllers.v1;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
    using Microsoft.AspNetCore.Mvc;
    using Moq;

    [TestClass]
    public class AppInfoControllerTest
    {
        [TestMethod]
        public async Task Get_ReturnsOk_WhenAppInfoExists()
        {
            // Arrange
            var appInfo = new AppInfoDto
            {
                Id = 1,
                InformationalVersion = "1.2.3",
                DatabaseLastUpdated = DateTime.UtcNow,
            };
            var service = new Mock<IAppInfoService>();
            service.Setup(x => x.GetAppInfoAsync()).ReturnsAsync(appInfo);
            var controller = new AppInfoController(service.Object);

            // Act
            var response = await controller.Get();

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = (response.Result as OkObjectResult)?.Value as AppInfoDto;
            Assert.IsNotNull(result);
            Assert.AreEqual("1.2.3", result.InformationalVersion);
        }

        [TestMethod]
        public async Task Get_ReturnsNotFound_WhenAppInfoIsNull()
        {
            // Arrange
            var service = new Mock<IAppInfoService>();
            service.Setup(x => x.GetAppInfoAsync()).ReturnsAsync((AppInfoDto?)null);
            var controller = new AppInfoController(service.Object);

            // Act
            var response = await controller.Get();

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(NotFoundResult));
        }
    }
}
