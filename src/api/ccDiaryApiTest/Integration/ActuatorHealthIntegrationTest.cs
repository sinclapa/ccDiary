// <copyright file="ActuatorHealthIntegrationTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Integration
{
    using System.Net;

    [TestClass]
    public class ActuatorHealthIntegrationTest
    {
        [TestMethod]
        public async Task HealthEndpointReturnsOk()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();

            // Act
            var response = await httpClient.GetAsync("/actuator/health");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task HealthEndpointIncludesDatabaseContributor()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();

            // Act
            var response = await httpClient.GetAsync("/actuator/health");
            var json = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.IsTrue(json.Contains("\"db\""), "Response should contain db health contributor");
        }

        [TestMethod]
        public async Task HealthEndpointDatabaseReportsUp()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();

            // Act
            var response = await httpClient.GetAsync("/actuator/health");
            var json = await response.Content.ReadAsStringAsync();

            // Assert — db details should include status=UP
            Assert.IsTrue(json.Contains("\"db\""), "Response should contain db health contributor");
            Assert.IsTrue(json.Contains("\"status\":\"UP\""), "Database health status should be UP");
        }
    }
}
