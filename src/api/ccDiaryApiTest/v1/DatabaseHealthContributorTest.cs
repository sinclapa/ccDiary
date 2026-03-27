// <copyright file="DatabaseHealthContributorTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Health;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Steeltoe.Common.HealthChecks;

    [TestClass]
    public class DatabaseHealthContributorTest
    {
        [TestMethod]
        public void Health_ReturnsDown_WhenDatabaseThrows()
        {
            // Arrange — InMemory provider does not support ExecuteSqlRaw, causing it to throw
            var dbName = "HealthContributorTest_Down_" + Guid.NewGuid();
            var services = new ServiceCollection();
            services.AddDbContext<DiaryDatabaseContext>(o => o.UseInMemoryDatabase(dbName));
            var sp = services.BuildServiceProvider();
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

            var contributor = new DatabaseHealthContributor(scopeFactory);

            // Act
            var result = contributor.Health();

            // Assert
            Assert.AreEqual(HealthStatus.DOWN, result.Status);
            Assert.IsTrue(result.Details.ContainsKey("error"));
        }
    }
}
