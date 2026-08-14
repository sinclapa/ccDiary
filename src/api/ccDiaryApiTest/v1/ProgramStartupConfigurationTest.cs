// <copyright file="ProgramStartupConfigurationTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System.Diagnostics;
    using Asp.Versioning;
    using Asp.Versioning.ApiExplorer;
    using ccDiaryApi;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Unit tests for startup configuration helpers in <see cref="Program"/>.
    /// </summary>
    [TestClass]
    public class ProgramStartupConfigurationTest
    {
        [TestMethod]
        public void ConfigureJwtBearer_SetsEvents()
        {
            // Arrange
            var options = new JwtBearerOptions();

            // Act
            Program.ConfigureJwtBearer(options);

            // Assert
            Assert.IsNotNull(options.Events);
            Assert.IsNotNull(options.Events.OnAuthenticationFailed);
            Assert.IsNotNull(options.Events.OnChallenge);
            Assert.IsNotNull(options.Events.OnForbidden);
        }

        [TestMethod]
        public void ConfigureApiVersioning_SetsExpectedOptions()
        {
            // Arrange
            var options = new ApiVersioningOptions();

            // Act
            Program.ConfigureApiVersioning(options);

            // Assert
            Assert.IsTrue(options.ReportApiVersions);
            Assert.IsInstanceOfType(options.ApiVersionReader, typeof(UrlSegmentApiVersionReader));
        }

        [TestMethod]
        public void ConfigureApiExplorer_SetsExpectedOptions()
        {
            // Arrange
            var options = new ApiExplorerOptions();

            // Act
            Program.ConfigureApiExplorer(options);

            // Assert
            Assert.AreEqual("'v'VVV", options.GroupNameFormat);
            Assert.IsTrue(options.SubstituteApiVersionInUrl);
        }
    }
}
