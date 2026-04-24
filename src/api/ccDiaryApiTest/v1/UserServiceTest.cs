// <copyright file="UserServiceTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;

    [TestClass]
    public class UserServiceTest
    {
        [TestMethod]
        public async Task SeedBootstrapAdminAsync_NoObjectId_DoesNothing()
        {
            using var db = CreateDb();
            var service = new UserService(db, CreateConfig());

            await service.SeedBootstrapAdminAsync();

            Assert.AreEqual(0, db.AppUsers.Count());
        }

        [TestMethod]
        public async Task SeedBootstrapAdminAsync_AdminAlreadyExists_DoesNotSeedAgain()
        {
            using var db = CreateDb();
            db.AppUsers.Add(new AppUserDto
            {
                UserId = Guid.NewGuid(),
                EntraObjectId = "existing-admin",
                DisplayName = "Existing Admin",
                Email = "admin@test.com",
                Role = AppRole.DiaryAdmin,
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            var service = new UserService(db, CreateConfig(objectId: "new-admin-oid", email: "new@test.com", displayName: "New Admin"));
            await service.SeedBootstrapAdminAsync();

            Assert.AreEqual(1, db.AppUsers.Count());
        }

        [TestMethod]
        public async Task SeedBootstrapAdminAsync_NoAdminExists_CreatesAdmin()
        {
            using var db = CreateDb();
            var service = new UserService(db, CreateConfig(objectId: "seed-oid", email: "seed@test.com", displayName: "Seed Admin"));

            await service.SeedBootstrapAdminAsync();

            var admin = await db.AppUsers.SingleAsync();
            Assert.AreEqual("seed-oid", admin.EntraObjectId);
            Assert.AreEqual(AppRole.DiaryAdmin, admin.Role);
        }

        [TestMethod]
        public async Task SeedBootstrapAdminAsync_EmailFallsBackToObjectId_WhenEmailMissing()
        {
            using var db = CreateDb();
            var service = new UserService(db, CreateConfig(objectId: "seed-oid"));

            await service.SeedBootstrapAdminAsync();

            var admin = await db.AppUsers.SingleAsync();
            Assert.AreEqual(string.Empty, admin.Email);
        }

        [TestMethod]
        public async Task GetOrCreateUserAsync_ExistingUser_ReturnsUser()
        {
            using var db = CreateDb();
            var user = new AppUserDto
            {
                UserId = Guid.NewGuid(),
                EntraObjectId = "existing-oid",
                DisplayName = "Existing",
                Email = "existing@test.com",
                Role = AppRole.DiaryContributor,
                CreatedAt = DateTime.UtcNow,
            };
            db.AppUsers.Add(user);
            await db.SaveChangesAsync();

            var service = new UserService(db, CreateConfig());
            var result = await service.GetOrCreateUserAsync("existing-oid", "existing@test.com", "Existing");

            Assert.IsNotNull(result);
            Assert.AreEqual(user.UserId, result.UserId);
        }

        [TestMethod]
        public async Task GetOrCreateUserAsync_NoApprovedRequest_ReturnsNull()
        {
            using var db = CreateDb();
            var service = new UserService(db, CreateConfig());

            var result = await service.GetOrCreateUserAsync("new-oid", "nobody@test.com", "Nobody");

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetOrCreateUserAsync_ApprovedRequest_CreatesAndReturnsContributor()
        {
            using var db = CreateDb();
            db.AccessRequests.Add(new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Invited User",
                Email = "invited@test.com",
                Status = RequestStatus.Approved,
                RequestedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            var service = new UserService(db, CreateConfig());
            var result = await service.GetOrCreateUserAsync("invited-oid", "invited@test.com", "Invited User");

            Assert.IsNotNull(result);
            Assert.AreEqual(AppRole.DiaryContributor, result.Role);
            Assert.AreEqual("invited-oid", result.EntraObjectId);
            Assert.AreEqual(1, db.AppUsers.Count());
        }

        [TestMethod]
        public async Task GetOrCreateUserAsync_PendingRequestOnly_ReturnsNull()
        {
            using var db = CreateDb();
            db.AccessRequests.Add(new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Pending User",
                Email = "pending@test.com",
                Status = RequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            var service = new UserService(db, CreateConfig());
            var result = await service.GetOrCreateUserAsync("pending-oid", "pending@test.com", "Pending User");

            Assert.IsNull(result);
        }

        private static DiaryDatabaseContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<DiaryDatabaseContext>()
                .UseInMemoryDatabase("UserServiceTest_" + Guid.NewGuid())
                .Options;
            var db = new DiaryDatabaseContext(options);
            db.Database.EnsureCreated();
            return db;
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
    }
}
