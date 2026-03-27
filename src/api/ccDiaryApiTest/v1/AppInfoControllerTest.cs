// <copyright file="AppInfoControllerTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using ccDiaryApi.Controllers.v1;
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    [TestClass]
    public class AppInfoControllerTest
    {
        private static DiaryDatabaseContext GetMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<DiaryDatabaseContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new DiaryDatabaseContext(options);
        }

        [TestMethod]
        public void Get_ReturnsOk_WhenAppInfoExists()
        {
            // Arrange
            var db = GetMemoryContext("AppInfoTest_Ok_" + Guid.NewGuid());
            db.AppInfo.Add(new AppInfoDTO
            {
                Id = 1,
                InformationalVersion = "1.2.3",
                DatabaseLastUpdated = DateTime.UtcNow,
            });
            db.SaveChanges();

            var service = new AppInfoService(db);
            var controller = new AppInfoController(service);

            // Act
            var response = controller.Get();

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = (response.Result as OkObjectResult)?.Value as AppInfoDTO;
            Assert.IsNotNull(result);
            Assert.AreEqual("1.2.3", result.InformationalVersion);
        }

        [TestMethod]
        public void Get_ReturnsNotFound_WhenAppInfoIsNull()
        {
            // Arrange
            var db = GetMemoryContext("AppInfoTest_NotFound_" + Guid.NewGuid());
            var service = new AppInfoService(db);
            var controller = new AppInfoController(service);

            // Act
            var response = controller.Get();

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(NotFoundResult));
        }
    }
}
