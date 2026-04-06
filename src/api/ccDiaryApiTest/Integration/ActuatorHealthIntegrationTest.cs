// <copyright file="ActuatorHealthIntegrationTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Integration
{
    using System.Net;

    [TestClass]
    public class ActuatorHealthIntegrationTest
    {
        private HttpClient _httpClient = null!;

        [TestInitialize]
        public void TestInit() => _httpClient = SharedTestFactory.Factory.CreateDefaultClient();

        [TestMethod]
        public async Task HealthEndpointReturnsOk()
        {
            // Act
            var response = await _httpClient.GetAsync("/actuator/health");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task HealthEndpointIncludesDatabaseContributor()
        {
            // Act
            var response = await _httpClient.GetAsync("/actuator/health");
            var json = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.IsTrue(json.Contains("\"db\""), "Response should contain db health contributor");
        }

        [TestMethod]
        public async Task HealthEndpointDatabaseReportsUp()
        {
            // Act
            var response = await _httpClient.GetAsync("/actuator/health");
            var json = await response.Content.ReadAsStringAsync();

            // Assert — db details should include status=UP
            Assert.IsTrue(json.Contains("\"db\""), "Response should contain db health contributor");
            Assert.IsTrue(json.Contains("\"status\":\"UP\""), "Database health status should be UP");
        }
    }
}
