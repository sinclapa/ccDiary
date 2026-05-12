// <copyright file="AppUserEnrichmentMiddlewareTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System.Security.Claims;
    using ccDiaryApi.Authorization;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;

    [TestClass]
    public class AppUserEnrichmentMiddlewareTest
    {
        [TestMethod]
        public async Task UseAppUserEnrichment_UnauthenticatedRequest_SkipsEnrichment_CallsNext()
        {
            // Arrange — no auth type means IsAuthenticated == false
            bool nextCalled = false;
            var services = new ServiceCollection();
            var sp = services.BuildServiceProvider();
            var app = new ApplicationBuilder(sp);
            app.UseAppUserEnrichment();
            app.Use((_, next) =>
            {
                nextCalled = true;
                return next();
            });
            var pipeline = app.Build();

            var context = new DefaultHttpContext
            {
                RequestServices = sp,
                User = new ClaimsPrincipal(new ClaimsIdentity()),
            };

            // Act
            await pipeline(context);

            // Assert
            Assert.IsTrue(nextCalled);
            Assert.IsNull(context.User.FindFirst(ClaimTypes.Role));
        }

        [TestMethod]
        public async Task UseAppUserEnrichment_NullIdentity_SkipsEnrichment()
        {
            // Arrange — ClaimsPrincipal with no identities → Identity property returns null
            var mockUserService = new Mock<IUserService>();
            var services = new ServiceCollection();
            services.AddScoped<IUserService>(_ => mockUserService.Object);
            var sp = services.BuildServiceProvider();
            var app = new ApplicationBuilder(sp);
            app.UseAppUserEnrichment();
            var pipeline = app.Build();

            var context = new DefaultHttpContext
            {
                RequestServices = sp,
                User = new ClaimsPrincipal(), // no identities → Identity is null
            };

            // Act
            await pipeline(context);

            // Assert — user service should never be called when Identity is null
            mockUserService.Verify(s => s.GetUserByOidAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task UseAppUserEnrichment_AuthenticatedButEmptyOid_SkipsUserServiceCall()
        {
            // Arrange — authenticated but no oid/objectidentifier claim → GetOid returns null
            var mockUserService = new Mock<IUserService>();
            var services = new ServiceCollection();
            services.AddScoped<IUserService>(_ => mockUserService.Object);
            var sp = services.BuildServiceProvider();
            var app = new ApplicationBuilder(sp);
            app.UseAppUserEnrichment();
            var pipeline = app.Build();

            var context = new DefaultHttpContext
            {
                RequestServices = sp,
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Name, "Test User") },
                    "test")),
            };

            // Act
            await pipeline(context);

            // Assert
            mockUserService.Verify(s => s.GetUserByOidAsync(It.IsAny<string>()), Times.Never);
            Assert.IsNull(context.User.FindFirst(ClaimTypes.Role));
        }

        [TestMethod]
        public async Task UseAppUserEnrichment_AppUserNotFound_DoesNotAddRoleClaim()
        {
            // Arrange — authenticated with OID, but no matching AppUser in DB
            var mockUserService = new Mock<IUserService>();
            mockUserService.Setup(s => s.GetUserByOidAsync("some-oid")).ReturnsAsync((AppUserDto?)null);
            var services = new ServiceCollection();
            services.AddScoped<IUserService>(_ => mockUserService.Object);
            var sp = services.BuildServiceProvider();
            var app = new ApplicationBuilder(sp);
            app.UseAppUserEnrichment();
            var pipeline = app.Build();

            var context = new DefaultHttpContext
            {
                RequestServices = sp,
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim("oid", "some-oid") },
                    "test")),
            };

            // Act
            await pipeline(context);

            // Assert
            Assert.IsNull(context.User.FindFirst(ClaimTypes.Role));
        }

        [TestMethod]
        public async Task UseAppUserEnrichment_AppUserFound_AddsRoleClaim()
        {
            // Arrange — authenticated with OID, AppUser exists in DB
            var appUser = new AppUserDto
            {
                UserId = Guid.NewGuid(),
                EntraObjectId = "some-oid",
                DisplayName = "Test User",
                Email = "test@example.com",
                Role = AppRole.DiaryAdmin,
            };
            var mockUserService = new Mock<IUserService>();
            mockUserService.Setup(s => s.GetUserByOidAsync("some-oid")).ReturnsAsync(appUser);
            var services = new ServiceCollection();
            services.AddScoped<IUserService>(_ => mockUserService.Object);
            var sp = services.BuildServiceProvider();
            var app = new ApplicationBuilder(sp);
            app.UseAppUserEnrichment();
            var pipeline = app.Build();

            var context = new DefaultHttpContext
            {
                RequestServices = sp,
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim("oid", "some-oid") },
                    "test")),
            };

            // Act
            await pipeline(context);

            // Assert
            var roleClaim = context.User.FindFirst(ClaimTypes.Role);
            Assert.IsNotNull(roleClaim);
            Assert.AreEqual(AppRole.DiaryAdmin.ToString(), roleClaim.Value);
        }
    }
}
