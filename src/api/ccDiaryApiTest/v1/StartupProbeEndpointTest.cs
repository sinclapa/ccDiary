// <copyright file="StartupProbeEndpointTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using ccDiaryApi.Endpoints;
    using ccDiaryApi.Infrastructure;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Tests for the endpoint the Container App startup and readiness probes poll.
    /// </summary>
    [TestClass]
    public class StartupProbeEndpointTest
    {
        [TestMethod]
        public void ReadinessStartsNotReady()
        {
            Assert.IsFalse(new StartupReadiness().IsReady);
        }

        [TestMethod]
        public void MarkReadyIsIdempotent()
        {
            var readiness = new StartupReadiness();

            readiness.MarkReady();
            readiness.MarkReady();

            Assert.IsTrue(readiness.IsReady);
        }

        [TestMethod]
        public async Task ReturnsServiceUnavailableUntilBootstrapCompletes()
        {
            var context = CreateContext(new StartupReadiness());

            await StartupProbeEndpoint.RespondAsync(context);

            // Any non-2xx reads as "not yet" to the probe; 503 says so without implying a fault.
            Assert.AreEqual(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        }

        [TestMethod]
        public async Task ReturnsOkOnceBootstrapCompletes()
        {
            var readiness = new StartupReadiness();
            readiness.MarkReady();
            var context = CreateContext(readiness);

            await StartupProbeEndpoint.RespondAsync(context);

            Assert.AreEqual(StatusCodes.Status200OK, context.Response.StatusCode);
            Assert.AreEqual("no-store", context.Response.Headers.CacheControl.ToString());
        }

        private static DefaultHttpContext CreateContext(StartupReadiness readiness)
        {
            var services = new ServiceCollection();
            services.AddSingleton(readiness);
            return new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        }
    }
}
