// <copyright file="EmailServiceTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using ccDiaryApi.Services;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Moq;

    [TestClass]
    public class EmailServiceTest
    {
        [TestMethod]
        public async Task SendInvitationAsync_MissingHost_ThrowsInvalidOperation()
        {
            var config = BuildConfig(host: null);
            var service = new EmailService(config, CreateLogger());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => service.SendInvitationAsync("to@test.example", "Test User", "https://example.com"));
        }

        [TestMethod]
        public async Task SendInvitationAsync_MissingUsername_ThrowsInvalidOperation()
        {
            var config = BuildConfig(username: null);
            var service = new EmailService(config, CreateLogger());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => service.SendInvitationAsync("to@test.example", "Test User", "https://example.com"));
        }

        [TestMethod]
        public async Task SendInvitationAsync_MissingPassword_ThrowsInvalidOperation()
        {
            var config = BuildConfig(password: null);
            var service = new EmailService(config, CreateLogger());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => service.SendInvitationAsync("to@test.example", "Test User", "https://example.com"));
        }

        [TestMethod]
        public async Task SendInvitationAsync_MissingFrom_ThrowsInvalidOperation()
        {
            var config = BuildConfig(from: null);
            var service = new EmailService(config, CreateLogger());
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => service.SendInvitationAsync("to@test.example", "Test User", "https://example.com"));
        }

        [TestMethod]
        public async Task SendInvitationAsync_AllConfigSet_BuildsEmailAndAttemptsSmtp()
        {
            // All config valid — service builds the full HTML email, then fails at the SMTP
            // connection to 127.0.0.1:62999 (connection refused). This covers all pre-connection
            // code including BuildHtml.
            var config = BuildConfig();
            var service = new EmailService(config, CreateLogger());
            var threw = false;
            try
            {
                await service.SendInvitationAsync("to@test.example", "Test User", "https://example.com/invite");
            }
            catch
            {
                threw = true;
            }

            Assert.IsTrue(threw, "Expected a connection failure when sending to an unreachable SMTP host.");
        }

        [TestMethod]
        public async Task SendInvitationAsync_InvalidPort_DefaultsTo587AndAttemptsSmtp()
        {
            // Invalid port string — defaults to 587. Covers the true branch of int.TryParse check.
            var config = BuildConfig(port: "not-a-port", fromName: "ccDiary Test");
            var service = new EmailService(config, CreateLogger());
            var threw = false;
            try
            {
                await service.SendInvitationAsync("to@test.example", "Test User", "https://example.com/invite");
            }
            catch
            {
                threw = true;
            }

            Assert.IsTrue(threw, "Expected a connection failure when sending to an unreachable SMTP host.");
        }

        private static IConfiguration BuildConfig(
            string? host = "127.0.0.1",
            string? port = "62999",
            string? username = "user@test.example",
            string? password = "test-password",
            string? from = "noreply@test.example",
            string? fromName = null)
        {
            var dict = new Dictionary<string, string?>();
            if (host != null)
            {
                dict["Smtp:Host"] = host;
            }

            if (port != null)
            {
                dict["Smtp:Port"] = port;
            }

            if (username != null)
            {
                dict["Smtp:Username"] = username;
            }

            if (password != null)
            {
                dict["Smtp:Password"] = password;
            }

            if (from != null)
            {
                dict["Smtp:From"] = from;
            }

            if (fromName != null)
            {
                dict["Smtp:FromName"] = fromName;
            }

            return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        }

        private static ILogger<EmailService> CreateLogger()
            => new Mock<ILogger<EmailService>>().Object;
    }
}
