// <copyright file="ObservabilityUserContextExtensionsTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System.Diagnostics;
    using System.Security.Claims;
    using ccDiaryApi.Extensions;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Unit tests for <see cref="ObservabilityUserContextExtensions"/>.
    /// </summary>
    [TestClass]
    public class ObservabilityUserContextExtensionsTest
    {
        [TestMethod]
        public void ResolveUserIdentifier_ReturnsOid_WhenPresent()
        {
            // Arrange
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim("oid", "oid-123"),
                        new Claim(ClaimTypes.NameIdentifier, "name-id-123"),
                        new Claim("sub", "sub-123"),
                    },
                    "test"));

            // Act
            var result = ObservabilityUserContextExtensions.ResolveUserIdentifier(user);

            // Assert
            Assert.AreEqual("oid-123", result);
        }

        [TestMethod]
        public void ResolveUserIdentifier_FallsBackToNameIdentifier_WhenOidMissing()
        {
            // Arrange
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "name-id-123"),
                        new Claim("sub", "sub-123"),
                    },
                    "test"));

            // Act
            var result = ObservabilityUserContextExtensions.ResolveUserIdentifier(user);

            // Assert
            Assert.AreEqual("name-id-123", result);
        }

        [TestMethod]
        public void ResolveUserIdentifier_FallsBackToSub_WhenOnlySubExists()
        {
            // Arrange
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim("sub", "sub-123"),
                    },
                    "test"));

            // Act
            var result = ObservabilityUserContextExtensions.ResolveUserIdentifier(user);

            // Assert
            Assert.AreEqual("sub-123", result);
        }

        [TestMethod]
        public void ResolveUserIdentifier_ReturnsNull_WhenNoKnownClaimsExist()
        {
            // Arrange
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.Name, "test-user"),
                    },
                    "test"));

            // Act
            var result = ObservabilityUserContextExtensions.ResolveUserIdentifier(user);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void CreatePseudonymousUserId_ReturnsDeterministicLowerHex()
        {
            // Arrange
            const string rawUserId = "oid-123";

            // Act
            var first = ObservabilityUserContextExtensions.CreatePseudonymousUserId(rawUserId);
            var second = ObservabilityUserContextExtensions.CreatePseudonymousUserId(rawUserId);

            // Assert
            Assert.AreEqual(first, second);
            Assert.AreEqual(24, first.Length);
            StringAssert.Matches(first, new System.Text.RegularExpressions.Regex("^[0-9a-f]{24}$"));
        }

        [TestMethod]
        public async Task UseObservabilityUserContext_SetsAuthenticatedTagFalse_ForAnonymousUser()
        {
            // Arrange
            using var activity = new Activity("test-anon");
            activity.Start();

            var app = BuildApplicationBuilder();
            app.UseObservabilityUserContext();
            var pipeline = app.Build();

            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()),
            };

            // Act
            await pipeline(context);

            // Assert
            Assert.IsFalse((bool?)activity.GetTagItem("enduser.authenticated") ?? true);
            Assert.IsNull(activity.GetTagItem("enduser.id"));
        }

        [TestMethod]
        public async Task UseObservabilityUserContext_SetsPseudonymousUserId_ForAuthenticatedUser()
        {
            // Arrange
            using var activity = new Activity("test-auth");
            activity.Start();

            var app = BuildApplicationBuilder();
            app.UseObservabilityUserContext();
            var pipeline = app.Build();

            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[]
                        {
                            new Claim("oid", "oid-123"),
                        },
                        "test")),
            };

            // Act
            await pipeline(context);

            // Assert
            Assert.IsTrue((bool?)activity.GetTagItem("enduser.authenticated") ?? false);
            var userIdTag = activity.GetTagItem("enduser.id") as string;
            Assert.IsFalse(string.IsNullOrEmpty(userIdTag));
            Assert.AreEqual(24, userIdTag!.Length);
        }

        [TestMethod]
        public async Task UseObservabilityUserContext_DoesNotSetUserIdTag_WhenIdentifierMissing()
        {
            // Arrange
            using var activity = new Activity("test-auth-no-id");
            activity.Start();

            var app = BuildApplicationBuilder();
            app.UseObservabilityUserContext();
            var pipeline = app.Build();

            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[]
                        {
                            new Claim(ClaimTypes.Name, "display-name"),
                        },
                        "test")),
            };

            // Act
            await pipeline(context);

            // Assert
            Assert.IsTrue((bool?)activity.GetTagItem("enduser.authenticated") ?? false);
            Assert.IsNull(activity.GetTagItem("enduser.id"));
        }

        private static ApplicationBuilder BuildApplicationBuilder()
        {
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            return new ApplicationBuilder(serviceProvider)
            {
                ApplicationServices = serviceProvider,
            };
        }
    }
}
