// <copyright file="AccessRequestServiceTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Moq;

    [TestClass]
    public class AccessRequestServiceTest
    {
        [TestMethod]
        public async Task ApproveAsync_AdminNotFound_ThrowsInvalidOperation()
        {
            using var db = CreateDb();
            var request = new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Test User",
                Email = "test@example.com",
                Status = RequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
            };
            db.AccessRequests.Add(request);
            await db.SaveChangesAsync();

            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger());

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => service.ApproveAsync(request.AccessRequestId, "non-existent-oid"));
        }

        [TestMethod]
        public async Task DeclineAsync_AdminNotFound_ThrowsInvalidOperation()
        {
            using var db = CreateDb();
            var request = new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Test User",
                Email = "test@example.com",
                Status = RequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
            };
            db.AccessRequests.Add(request);
            await db.SaveChangesAsync();

            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger());

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => service.DeclineAsync(request.AccessRequestId, "non-existent-oid"));
        }

        [TestMethod]
        public async Task ApproveAsync_NoEmailService_LogsWarningAndReturnsRedeemUrl()
        {
            using var db = CreateDb();
            db.AppUsers.Add(CreateAdminUser("admin-oid"));
            var request = CreatePendingRequest();
            db.AccessRequests.Add(request);
            await db.SaveChangesAsync();

            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger());
            var result = await service.ApproveAsync(request.AccessRequestId, "admin-oid");

            Assert.AreEqual("https://test-redeem.example.com", result);
        }

        [TestMethod]
        public async Task ApproveAsync_EmailServiceThrows_LogsErrorAndReturnsRedeemUrl()
        {
            using var db = CreateDb();
            db.AppUsers.Add(CreateAdminUser("admin-oid"));
            var request = CreatePendingRequest();
            db.AccessRequests.Add(request);
            await db.SaveChangesAsync();

            var emailMock = new Mock<IEmailService>();
            emailMock
                .Setup(e => e.SendInvitationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("SMTP failed"));

            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger(), emailMock.Object);
            var result = await service.ApproveAsync(request.AccessRequestId, "admin-oid");

            Assert.AreEqual("https://test-redeem.example.com", result);
        }

        [TestMethod]
        public async Task GetAllAsync_ReturnsAllRequests()
        {
            using var db = CreateDb();
            db.AccessRequests.Add(CreatePendingRequest("req1@example.com"));
            db.AccessRequests.Add(new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Approved User",
                Email = "req2@example.com",
                Status = RequestStatus.Approved,
                RequestedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger());
            var result = (await service.GetAllAsync()).ToList();

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public async Task ResendInvitationAsync_WithEmailService_SendsEmailAndReturnsUrl()
        {
            using var db = CreateDb();
            var request = new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Test User",
                Email = "test@example.com",
                Status = RequestStatus.Approved,
                RequestedAt = DateTime.UtcNow,
                InviteRedeemUrl = "https://test-redeem.example.com",
            };
            db.AccessRequests.Add(request);
            await db.SaveChangesAsync();

            var emailMock = new Mock<IEmailService>();
            emailMock
                .Setup(e => e.SendInvitationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger(), emailMock.Object);
            var result = await service.ResendInvitationAsync(request.AccessRequestId);

            Assert.AreEqual("https://test-redeem.example.com", result);
            emailMock.Verify(e => e.SendInvitationAsync("test@example.com", "Test User", "https://test-redeem.example.com"), Times.Once);
        }

        [TestMethod]
        public async Task ResendInvitationAsync_NoEmailService_LogsWarningAndReturnsUrl()
        {
            using var db = CreateDb();
            var request = new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Test User",
                Email = "test@example.com",
                Status = RequestStatus.Approved,
                RequestedAt = DateTime.UtcNow,
                InviteRedeemUrl = "https://test-redeem.example.com",
            };
            db.AccessRequests.Add(request);
            await db.SaveChangesAsync();

            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger());
            var result = await service.ResendInvitationAsync(request.AccessRequestId);

            Assert.AreEqual("https://test-redeem.example.com", result);
        }

        [TestMethod]
        public async Task DeleteAsync_NotFound_ThrowsKeyNotFoundException()
        {
            using var db = CreateDb();
            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger());

            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
                () => service.DeleteAsync(Guid.NewGuid()));
        }

        [TestMethod]
        public async Task DeleteAsync_PendingRequest_ThrowsInvalidOperation()
        {
            using var db = CreateDb();
            var request = CreatePendingRequest();
            db.AccessRequests.Add(request);
            await db.SaveChangesAsync();

            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger());

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => service.DeleteAsync(request.AccessRequestId));
        }

        [TestMethod]
        public async Task DeleteAsync_ApprovedRequest_DeletesRecord()
        {
            using var db = CreateDb();
            var request = new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Test User",
                Email = "test@example.com",
                Status = RequestStatus.Approved,
                RequestedAt = DateTime.UtcNow,
            };
            db.AccessRequests.Add(request);
            await db.SaveChangesAsync();

            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger());
            await service.DeleteAsync(request.AccessRequestId);

            Assert.AreEqual(0, db.AccessRequests.Count());
        }

        [TestMethod]
        public async Task DeleteAsync_DeclinedRequest_DeletesRecord()
        {
            using var db = CreateDb();
            var request = new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Test User",
                Email = "test@example.com",
                Status = RequestStatus.Declined,
                RequestedAt = DateTime.UtcNow,
            };
            db.AccessRequests.Add(request);
            await db.SaveChangesAsync();

            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger());
            await service.DeleteAsync(request.AccessRequestId);

            Assert.AreEqual(0, db.AccessRequests.Count());
        }

        [TestMethod]
        public async Task SubmitAsync_NewEmail_AddsRequest()
        {
            using var db = CreateDb();
            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger());

            await service.SubmitAsync("New User", "new@example.com");

            Assert.AreEqual(1, db.AccessRequests.Count());
            var saved = db.AccessRequests.First();
            Assert.AreEqual("new@example.com", saved.Email);
            Assert.AreEqual(RequestStatus.Pending, saved.Status);
        }

        [TestMethod]
        public async Task SubmitAsync_DuplicatePendingEmail_ThrowsInvalidOperation()
        {
            using var db = CreateDb();
            db.AccessRequests.Add(CreatePendingRequest("dup@example.com"));
            await db.SaveChangesAsync();

            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger());

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => service.SubmitAsync("Dup User", "dup@example.com"));
        }

        [TestMethod]
        public async Task GetPendingAsync_ReturnsOnlyPendingRequests()
        {
            using var db = CreateDb();
            db.AccessRequests.Add(CreatePendingRequest("pending@example.com"));
            db.AccessRequests.Add(new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Approved User",
                Email = "approved@example.com",
                Status = RequestStatus.Approved,
                RequestedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger());
            var result = (await service.GetPendingAsync()).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("pending@example.com", result[0].Email);
        }

        [TestMethod]
        public async Task ApproveAsync_RequestNotFound_ThrowsKeyNotFoundException()
        {
            using var db = CreateDb();
            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger());

            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
                () => service.ApproveAsync(Guid.NewGuid(), "admin-oid"));
        }

        [TestMethod]
        public async Task ApproveAsync_WithEmailService_SendsEmailAndReturnsRedeemUrl()
        {
            using var db = CreateDb();
            db.AppUsers.Add(CreateAdminUser("admin-oid"));
            var request = CreatePendingRequest();
            db.AccessRequests.Add(request);
            await db.SaveChangesAsync();

            var emailMock = new Mock<IEmailService>();
            emailMock
                .Setup(e => e.SendInvitationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger(), emailMock.Object);
            var result = await service.ApproveAsync(request.AccessRequestId, "admin-oid");

            Assert.AreEqual("https://test-redeem.example.com", result);
            emailMock.Verify(
                e => e.SendInvitationAsync(request.Email, request.DisplayName, "https://test-redeem.example.com"),
                Times.Once);
        }

        [TestMethod]
        public async Task DeclineAsync_RequestNotFound_ThrowsKeyNotFoundException()
        {
            using var db = CreateDb();
            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger());

            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
                () => service.DeclineAsync(Guid.NewGuid(), "admin-oid"));
        }

        [TestMethod]
        public async Task DeclineAsync_HappyPath_SetsDeclinedStatus()
        {
            using var db = CreateDb();
            db.AppUsers.Add(CreateAdminUser("admin-oid"));
            var request = CreatePendingRequest();
            db.AccessRequests.Add(request);
            await db.SaveChangesAsync();

            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger());
            await service.DeclineAsync(request.AccessRequestId, "admin-oid");

            var updated = await db.AccessRequests.FindAsync(request.AccessRequestId);
            Assert.IsNotNull(updated);
            Assert.AreEqual(RequestStatus.Declined, updated.Status);
        }

        [TestMethod]
        public async Task ResendInvitationAsync_RequestNotFound_ThrowsKeyNotFoundException()
        {
            using var db = CreateDb();
            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger());

            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
                () => service.ResendInvitationAsync(Guid.NewGuid()));
        }

        [TestMethod]
        public async Task ResendInvitationAsync_NoInviteUrl_ReturnsNull()
        {
            using var db = CreateDb();
            var request = new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Test User",
                Email = "test@example.com",
                Status = RequestStatus.Approved,
                RequestedAt = DateTime.UtcNow,
                InviteRedeemUrl = null,
            };
            db.AccessRequests.Add(request);
            await db.SaveChangesAsync();

            var service = new AccessRequestService(db, CreateMockGraph(), CreateLogger());
            var result = await service.ResendInvitationAsync(request.AccessRequestId);

            Assert.IsNull(result);
        }

        private static AppUserDto CreateAdminUser(string oid) => new AppUserDto
        {
            UserId = Guid.NewGuid(),
            EntraObjectId = oid,
            DisplayName = "Test Admin",
            Email = $"{oid}@test.com",
            Role = AppRole.DiaryAdmin,
            CreatedAt = DateTime.UtcNow,
        };

        private static AccessRequestDto CreatePendingRequest(string email = "test@example.com") => new AccessRequestDto
        {
            AccessRequestId = Guid.NewGuid(),
            DisplayName = "Test User",
            Email = email,
            Status = RequestStatus.Pending,
            RequestedAt = DateTime.UtcNow,
        };

        private static DiaryDatabaseContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<DiaryDatabaseContext>()
                .UseInMemoryDatabase("AccessRequestServiceTest_" + Guid.NewGuid())
                .Options;
            var db = new DiaryDatabaseContext(options);
            db.Database.EnsureCreated();
            return db;
        }

        private static ILogger<AccessRequestService> CreateLogger()
            => new Mock<ILogger<AccessRequestService>>().Object;

        private static IGraphService CreateMockGraph(string redeemUrl = "https://test-redeem.example.com")
        {
            var mock = new Mock<IGraphService>();
            mock.Setup(g => g.SendInvitationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(redeemUrl);
            return mock.Object;
        }
    }
}
