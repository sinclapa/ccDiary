// <copyright file="UserServiceTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Data.Storage;
    using ccDiaryApi.Services;
    using ccDiaryApiTest.Storage;
    using Microsoft.Extensions.Configuration;

    [TestClass]
    public class UserServiceTest
    {
        private StorageTestFixture _fixture = null!;

        [TestInitialize]
        public async Task Init()
        {
            _fixture = await StorageTestFixture.CreateAsync();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _fixture?.Dispose();
        }

        [TestMethod]
        public async Task SeedBootstrapAdminAsync_NoObjectId_DoesNothing()
        {
            var service = CreateService(CreateConfig());

            await service.SeedBootstrapAdminAsync();

            Assert.AreEqual(0, await CountUsersAsync());
        }

        [TestMethod]
        public async Task SeedBootstrapAdminAsync_AdminAlreadyExists_DoesNotSeedAgain()
        {
            await _fixture.SeedUserAsync("existing-admin", AppRole.DiaryAdmin, "admin@test.com");

            var service = CreateService(CreateConfig(objectId: "new-admin-oid", email: "new@test.com", displayName: "New Admin"));
            await service.SeedBootstrapAdminAsync();

            Assert.AreEqual(1, await CountUsersAsync());
        }

        [TestMethod]
        public async Task SeedBootstrapAdminAsync_NoAdminExists_CreatesAdmin()
        {
            var service = CreateService(CreateConfig(objectId: "seed-oid", email: "seed@test.com", displayName: "Seed Admin"));

            await service.SeedBootstrapAdminAsync();

            var admin = await service.GetUserByOidAsync("seed-oid");
            Assert.IsNotNull(admin);
            Assert.AreEqual("seed-oid", admin.EntraObjectId);
            Assert.AreEqual(AppRole.DiaryAdmin, admin.Role);
        }

        [TestMethod]
        public async Task SeedBootstrapAdminAsync_IsIdempotent_AcrossRestarts()
        {
            // Runs on every boot, so a second run must not create a second admin.
            var service = CreateService(CreateConfig(objectId: "seed-oid", email: "seed@test.com"));

            await service.SeedBootstrapAdminAsync();
            await service.SeedBootstrapAdminAsync();

            Assert.AreEqual(1, await CountUsersAsync());
        }

        [TestMethod]
        public async Task SeedBootstrapAdminAsync_EmailFallsBackToObjectId_WhenEmailMissing()
        {
            var service = CreateService(CreateConfig(objectId: "seed-oid"));

            await service.SeedBootstrapAdminAsync();

            var admin = await service.GetUserByOidAsync("seed-oid");
            Assert.IsNotNull(admin);
            Assert.AreEqual(string.Empty, admin.Email);
        }

        [TestMethod]
        public async Task GetOrCreateUserAsync_ExistingUser_ReturnsUser()
        {
            var user = await _fixture.SeedUserAsync("existing-oid", AppRole.DiaryContributor, "existing@test.com");
            var service = CreateService(CreateConfig());

            var result = await service.GetOrCreateUserAsync("existing-oid", "existing@test.com", "Existing");

            Assert.IsNotNull(result);
            Assert.AreEqual(user.UserId, result.UserId);
        }

        [TestMethod]
        public async Task GetOrCreateUserAsync_NoApprovedRequest_ReturnsNull()
        {
            var service = CreateService(CreateConfig());

            var result = await service.GetOrCreateUserAsync("new-oid", "nobody@test.com", "Nobody");

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetOrCreateUserAsync_ApprovedRequest_CreatesAndReturnsContributor()
        {
            await _fixture.SeedAccessRequestAsync("invited@test.com", RequestStatus.Approved, "Invited User");
            var service = CreateService(CreateConfig());

            var result = await service.GetOrCreateUserAsync("invited-oid", "invited@test.com", "Invited User");

            Assert.IsNotNull(result);
            Assert.AreEqual(AppRole.DiaryContributor, result.Role);
            Assert.AreEqual("invited-oid", result.EntraObjectId);
            Assert.AreEqual(1, await CountUsersAsync());
        }

        [TestMethod]
        public async Task GetOrCreateUserAsync_PendingRequestOnly_ReturnsNull()
        {
            await _fixture.SeedAccessRequestAsync("pending@test.com", RequestStatus.Pending, "Pending User");
            var service = CreateService(CreateConfig());

            var result = await service.GetOrCreateUserAsync("pending-oid", "pending@test.com", "Pending User");

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetUserByOidAsync_ReturnsNull_ForUnknownOid()
        {
            var service = CreateService(CreateConfig());

            Assert.IsNull(await service.GetUserByOidAsync("no-such-oid"));
        }

        [TestMethod]
        public async Task GetOrCreateUserAsync_MatchesTheApprovedRequestByEmailOnly()
        {
            // An approval for one address must not admit a different one.
            await _fixture.SeedAccessRequestAsync("invited@test.com", RequestStatus.Approved);
            var service = CreateService(CreateConfig());

            Assert.IsNull(await service.GetOrCreateUserAsync("other-oid", "someone.else@test.com", "Other"));
        }

        private static IConfiguration CreateConfig(string? objectId = null, string? email = null, string? displayName = null)
        {
            var data = new Dictionary<string, string?>();
            if (objectId != null)
            {
                data["BootstrapAdmin:ObjectId"] = objectId;
            }

            if (email != null)
            {
                data["BootstrapAdmin:Email"] = email;
            }

            if (displayName != null)
            {
                data["BootstrapAdmin:DisplayName"] = displayName;
            }

            return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        }

        private UserService CreateService(IConfiguration configuration) =>
            new UserService(_fixture.Tables, configuration);

        private async Task<int> CountUsersAsync()
        {
            var rows = await TableJson.QueryAsync(_fixture.Tables.AppUsers);
            return rows.Count;
        }
    }
}
