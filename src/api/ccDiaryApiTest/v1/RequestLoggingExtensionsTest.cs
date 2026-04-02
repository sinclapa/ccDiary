// <copyright file="RequestLoggingExtensionsTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System.Diagnostics;
    using ccDiaryApi.Extensions;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Unit tests for <see cref="RequestLoggingExtensions"/>.
    /// </summary>
    [TestClass]
    public class RequestLoggingExtensionsTest
    {
        [TestMethod]
        public async Task UseRequestCompletionLogging_SkipsLowValuePath_ButStillInvokesNext()
        {
            // Arrange
            var app = BuildApplicationBuilder();
            var nextCalled = false;

            app.UseRequestCompletionLogging();
            app.Run(ctx =>
            {
                nextCalled = true;
                ctx.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            });

            var pipeline = app.Build();
            var context = new DefaultHttpContext();
            context.Request.Path = "/swagger/index.html";

            // Act
            await pipeline(context);

            // Assert
            Assert.IsTrue(nextCalled);
            Assert.AreEqual(StatusCodes.Status204NoContent, context.Response.StatusCode);
        }

        [TestMethod]
        public async Task UseRequestCompletionLogging_ProcessesTracedPath_WithNoCurrentActivity()
        {
            // Arrange
            var app = BuildApplicationBuilder();

            app.UseRequestCompletionLogging();
            app.Run(ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            });

            var pipeline = app.Build();
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/api/v1/Diary/Get";

            // Act
            await pipeline(context);

            // Assert
            Assert.AreEqual(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [TestMethod]
        public async Task UseRequestCompletionLogging_ProcessesServerErrorPath_WithCurrentActivity()
        {
            // Arrange
            var app = BuildApplicationBuilder();

            app.UseRequestCompletionLogging();
            app.Run(ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Task.CompletedTask;
            });

            var pipeline = app.Build();
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Post;
            context.Request.Path = "/api/v1/DiaryEntry/Create";

            using var activity = new Activity("request-logging-test");
            activity.Start();

            // Act
            await pipeline(context);

            // Assert
            Assert.AreEqual(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
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
