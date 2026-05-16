// <copyright file="GraphServiceTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using ccDiaryApi.Services;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Moq;

    [TestClass]
    public class GraphServiceTest
    {
        [TestMethod]
        public async Task SendInvitationAsync_TenantIdMissing_ReturnsEmpty()
        {
            var config = BuildConfig(tenantId: null);
            var service = CreateService(config);
            var result = await service.SendInvitationAsync("test@example.com", "Test User");
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public async Task SendInvitationAsync_ClientIdMissing_ReturnsEmpty()
        {
            var config = BuildConfig(clientId: null);
            var service = CreateService(config);
            var result = await service.SendInvitationAsync("test@example.com", "Test User");
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public async Task SendInvitationAsync_ClientSecretMissing_ReturnsEmpty()
        {
            var config = BuildConfig(clientSecret: null);
            var service = CreateService(config);
            var result = await service.SendInvitationAsync("test@example.com", "Test User");
            Assert.AreEqual(string.Empty, result);
        }

        private static IConfiguration BuildConfig(
            string? tenantId = "test-tenant-id",
            string? clientId = "test-client-id",
            string? clientSecret = "test-client-secret",
            string? redirectUrl = "https://app.test.example",
            string? smtpHost = null)
        {
            var dict = new Dictionary<string, string?>();
            if (tenantId != null)
            {
                dict["Graph:TenantId"] = tenantId;
            }

            if (clientId != null)
            {
                dict["Graph:ClientId"] = clientId;
            }

            if (clientSecret != null)
            {
                dict["Graph:ClientSecret"] = clientSecret;
            }

            if (redirectUrl != null)
            {
                dict["Graph:InviteRedirectUrl"] = redirectUrl;
            }

            if (smtpHost != null)
            {
                dict["Smtp:Host"] = smtpHost;
            }

            return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        }

        private static GraphService CreateService(IConfiguration config)
        {
            var httpFactory = new Mock<IHttpClientFactory>().Object;
            var logger = new Mock<ILogger<GraphService>>().Object;
            return new GraphService(config, httpFactory, logger);
        }
    }
}
