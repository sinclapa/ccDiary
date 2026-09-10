// <copyright file="StartupProbeIntegrationTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Integration
{
    using System.Net;
    using ccDiaryApi.Endpoints;

    [TestClass]
    public class StartupProbeIntegrationTest
    {
        private HttpClient _httpClient = null!;

        [TestInitialize]
        public void TestInit() => _httpClient = SharedTestFactory.Factory.CreateDefaultClient();

        [TestMethod]
        public async Task StartupProbeReportsReadyOnceTheHostHasStarted()
        {
            // The host only finishes starting once StorageBootstrapper has run, so by the time a
            // client exists the flag must be set. This exercises the real wiring end to end: the
            // pipeline branch, the singleton and the hosted service that marks it.
            var response = await _httpClient.GetAsync(StartupProbeEndpoint.Path);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
