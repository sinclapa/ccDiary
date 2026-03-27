// <copyright file="AppInfoIntegrationTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Integration
{
    using System.Net;
    using System.Net.Http.Json;
    using ccDiaryApi.Data.Model;

    [TestClass]
    public class AppInfoIntegrationTest
    {
        [TestMethod]
        public async Task GetAppInfo_ReturnsOk()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();

            // Act
            var response = await httpClient.GetAsync("/api/v1/AppInfo/Get");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task GetAppInfo_ReturnsExpectedFields()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();

            // Act
            var response = await httpClient.GetAsync("/api/v1/AppInfo/Get");
            var result = await response.Content.ReadFromJsonAsync<AppInfoDTO>();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(string.IsNullOrEmpty(result.InformationalVersion));
        }

        [TestMethod]
        public async Task AssemblyInfoEndpoint_ReturnsOk()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();

            // Act
            var response = await httpClient.GetAsync("/api/assembly-info");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task AssemblyInfoEndpoint_ReturnsAssemblyNameAndVersion()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();

            // Act
            var response = await httpClient.GetAsync("/api/assembly-info");
            var json = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.IsTrue(json.Contains("assemblyName"), "Response should contain assemblyName");
            Assert.IsTrue(json.Contains("assemblyVersion"), "Response should contain assemblyVersion");
        }
    }
}
