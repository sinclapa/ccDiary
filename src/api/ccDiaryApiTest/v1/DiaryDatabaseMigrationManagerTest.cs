// <copyright file="DiaryDatabaseMigrationManagerTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Migration;
    using ccDiaryApi.Data.Model;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    [TestClass]
    public class DiaryDatabaseMigrationManagerTest
    {
        private static IHost BuildHost(string dbName)
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddDbContext<DiaryDatabaseContext>(o => o.UseInMemoryDatabase(dbName));
                })
                .Build();
        }

        [TestMethod]
        public void MigrateDatabase_InsertsAppInfo_WhenNoRecordExists()
        {
            // Arrange
            var dbName = "MigrateTest_Insert_" + Guid.NewGuid();
            var host = BuildHost(dbName);

            // Act
            host.MigrateDatabase();

            // Assert
            var options = new DbContextOptionsBuilder<DiaryDatabaseContext>()
                .UseInMemoryDatabase(dbName).Options;
            using var ctx = new DiaryDatabaseContext(options);
            var appInfo = ctx.AppInfo.SingleOrDefault(a => a.Id == 1);
            Assert.IsNotNull(appInfo);
            Assert.IsFalse(string.IsNullOrEmpty(appInfo.InformationalVersion));
        }

        [TestMethod]
        public void MigrateDatabase_UpdatesAppInfo_WhenRecordAlreadyExists()
        {
            // Arrange
            var dbName = "MigrateTest_Update_" + Guid.NewGuid();
            var options = new DbContextOptionsBuilder<DiaryDatabaseContext>()
                .UseInMemoryDatabase(dbName).Options;

            // Pre-seed AppInfo with an old version
            using (var ctx = new DiaryDatabaseContext(options))
            {
                ctx.AppInfo.Add(new AppInfoDTO
                {
                    Id = 1,
                    InformationalVersion = "0.0.1-old",
                    DatabaseLastUpdated = DateTime.UtcNow.AddDays(-1),
                });
                ctx.SaveChanges();
            }

            var host = BuildHost(dbName);

            // Act — should hit the update branch since AppInfo already exists
            host.MigrateDatabase();

            // Assert
            using var verifyCtx = new DiaryDatabaseContext(options);
            var appInfo = verifyCtx.AppInfo.SingleOrDefault(a => a.Id == 1);
            Assert.IsNotNull(appInfo);
            Assert.AreNotEqual("0.0.1-old", appInfo.InformationalVersion);
        }
    }
}
