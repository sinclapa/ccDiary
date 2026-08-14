// <copyright file="ProgramConfigIntegrationTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Integration
{
    using System.Net;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.Configuration;

    [TestClass]
    public class ProgramConfigIntegrationTest
    {
        [TestMethod]
        public async Task App_StartsAndResponds_WhenEnvironmentIsLocal()
        {
            // "Local" exercises the AddUserSecrets configuration branch.
            var factory = new LocalEnvironmentFactory();
            var client = factory.CreateDefaultClient();

            var response = await client.GetAsync("/api/v1/Diary/Get");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public void ValidateStorageConfiguration_Throws_WhenNothingIsConfigured()
        {
            // Startup must fail loudly rather than letting the first request die with an
            // unrelated error deep inside the storage SDK.
            var configuration = new ConfigurationBuilder().Build();

            var ex = Assert.ThrowsException<InvalidOperationException>(
                () => Program.ValidateStorageConfiguration(configuration));

            StringAssert.Contains(ex.Message, "Storage is not configured");
        }

        [TestMethod]
        public void ValidateStorageConfiguration_Accepts_AConnectionString()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Storage:ConnectionString"] = "UseDevelopmentStorage=true",
                })
                .Build();

            Program.ValidateStorageConfiguration(configuration);
        }

        [TestMethod]
        public void ValidateStorageConfiguration_Accepts_AnAccountName()
        {
            // The managed identity path: an account name and no secret at all.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Storage:AccountName"] = "examplestorage",
                })
                .Build();

            Program.ValidateStorageConfiguration(configuration);
        }

        /// <summary>
        /// Factory that runs in "Local" environment to cover the AddUserSecrets branch.
        /// </summary>
        private sealed class LocalEnvironmentFactory : CustomWebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);
                builder.UseEnvironment("Local");
            }
        }
    }
}
