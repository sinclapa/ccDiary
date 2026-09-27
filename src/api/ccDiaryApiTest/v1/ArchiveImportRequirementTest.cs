// <copyright file="ArchiveImportRequirementTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System.Security.Claims;
    using ccDiaryApi.Authorization;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Extensions;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Configuration;

    [TestClass]
    public class ArchiveImportRequirementTest
    {
        private const string AppClientId = "11111111-2222-3333-4444-555555555555";

        [TestMethod]
        public async Task AnAdminMayImport()
        {
            Assert.IsTrue(await Allowed(User(new Claim(ClaimTypes.Role, AppRole.DiaryAdmin.ToString()))));
        }

        [TestMethod]
        public async Task AContributorMayNot()
        {
            Assert.IsFalse(await Allowed(User(new Claim(ClaimTypes.Role, AppRole.DiaryContributor.ToString()))));
        }

        [TestMethod]
        public async Task TheApplicationsOwnAppOnlyTokenMayImport()
        {
            // how the deploy pipeline seeds each environment before its end-to-end run
            Assert.IsTrue(await Allowed(User(new Claim("azp", AppClientId))));
        }

        [TestMethod]
        public async Task AnotherApplicationMayNot()
        {
            Assert.IsFalse(await Allowed(User(new Claim("azp", "99999999-0000-0000-0000-000000000000"))));
        }

        [TestMethod]
        public async Task NothingMatchesWhenNoClientIdIsConfigured()
        {
            Assert.IsFalse(await Allowed(User(new Claim("azp", AppClientId)), clientId: null));
        }

        [TestMethod]
        public void AV1TokenNamesTheClientInAppid()
        {
            Assert.IsTrue(User(new Claim("appid", AppClientId)).IsApplicationItself(AppClientId));
        }

        [TestMethod]
        public void TheClientIdIsComparedWithoutCase()
        {
            Assert.IsTrue(User(new Claim("azp", AppClientId.ToUpperInvariant())).IsApplicationItself(AppClientId));
        }

        [TestMethod]
        public void AUserSigningInThroughTheApplicationIsNotTheApplication()
        {
            // a delegated token also names the client in azp, but carries the user's scopes
            Assert.IsFalse(User(new Claim("azp", AppClientId), new Claim("scp", "access_as_user")).IsApplicationItself(AppClientId));
            Assert.IsFalse(User(
                new Claim("azp", AppClientId),
                new Claim("http://schemas.microsoft.com/identity/claims/scope", "access_as_user")).IsApplicationItself(AppClientId));
        }

        [TestMethod]
        public void ABlankClientIdMatchesNothing()
        {
            Assert.IsFalse(User(new Claim("azp", string.Empty)).IsApplicationItself(" "));
        }

        private static ClaimsPrincipal User(params Claim[] claims) =>
            new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        private static async Task<bool> Allowed(ClaimsPrincipal user, string? clientId = AppClientId)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Entra:ClientId"] = clientId })
                .Build();
            var requirement = new ArchiveImportRequirement();
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);
            await new ArchiveImportHandler(configuration).HandleAsync(context);
            return context.HasSucceeded;
        }
    }
}
