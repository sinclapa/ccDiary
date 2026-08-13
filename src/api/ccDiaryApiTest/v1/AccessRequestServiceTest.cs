// <copyright file="AccessRequestServiceTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Data.Storage;
    using ccDiaryApi.Services;
    using ccDiaryApiTest.Storage;
    using global::Azure.Data.Tables;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Moq;

    [TestClass]
    public class AccessRequestServiceTest
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
        public async Task ApproveAsync_AdminNotFound_ThrowsInvalidOperation()
        {
            // storage comes from the fixture
            var request = new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Test User",
                Email = "test@example.com",
                Status = RequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
            };
            await SeedRequestAsync(request);

            var service = CreateService();

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => service.ApproveAsync(request.AccessRequestId, "non-existent-oid"));
        }

        [TestMethod]
        public async Task DeclineAsync_AdminNotFound_ThrowsInvalidOperation()
        {
            // storage comes from the fixture
            var request = new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Test User",
                Email = "test@example.com",
                Status = RequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
            };
            await SeedRequestAsync(request);

            var service = CreateService();

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => service.DeclineAsync(request.AccessRequestId, "non-existent-oid"));
        }

        [TestMethod]
        public async Task ApproveAsync_NoEmailService_LogsWarningAndReturnsRedeemUrl()
        {
            // storage comes from the fixture
            await _fixture.SeedUserAsync("admin-oid", AppRole.DiaryAdmin);
            var request = CreatePendingRequest();
            await SeedRequestAsync(request);

            var service = CreateService();
            var result = await service.ApproveAsync(request.AccessRequestId, "admin-oid");

            Assert.AreEqual("https://test-redeem.example.com", result);
        }

        [TestMethod]
        public async Task ApproveAsync_EmailServiceThrows_LogsErrorAndReturnsRedeemUrl()
        {
            // storage comes from the fixture
            await _fixture.SeedUserAsync("admin-oid", AppRole.DiaryAdmin);
            var request = CreatePendingRequest();
            await SeedRequestAsync(request);

            var emailMock = new Mock<IEmailService>();
            emailMock
                .Setup(e => e.SendInvitationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("SMTP failed"));

            var service = CreateService(emailMock.Object);
            var result = await service.ApproveAsync(request.AccessRequestId, "admin-oid");

            Assert.AreEqual("https://test-redeem.example.com", result);
        }

        [TestMethod]
        public async Task GetAllAsync_ReturnsAllRequests()
        {
            // storage comes from the fixture
            await SeedRequestAsync(CreatePendingRequest("req1@example.com"));
            await SeedRequestAsync(new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Approved User",
                Email = "req2@example.com",
                Status = RequestStatus.Approved,
                RequestedAt = DateTime.UtcNow,
            });

            var service = CreateService();
            var result = (await service.GetAllAsync()).ToList();

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public async Task ResendInvitationAsync_WithEmailService_SendsEmailAndReturnsUrl()
        {
            // storage comes from the fixture
            var request = new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Test User",
                Email = "test@example.com",
                Status = RequestStatus.Approved,
                RequestedAt = DateTime.UtcNow,
                InviteRedeemUrl = "https://test-redeem.example.com",
            };
            await SeedRequestAsync(request);

            var emailMock = new Mock<IEmailService>();
            emailMock
                .Setup(e => e.SendInvitationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var service = CreateService(emailMock.Object);
            var result = await service.ResendInvitationAsync(request.AccessRequestId);

            Assert.AreEqual("https://test-redeem.example.com", result);
            emailMock.Verify(e => e.SendInvitationAsync("test@example.com", "Test User", "https://test-redeem.example.com"), Times.Once);
        }

        [TestMethod]
        public async Task ResendInvitationAsync_NoEmailService_LogsWarningAndReturnsUrl()
        {
            // storage comes from the fixture
            var request = new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Test User",
                Email = "test@example.com",
                Status = RequestStatus.Approved,
                RequestedAt = DateTime.UtcNow,
                InviteRedeemUrl = "https://test-redeem.example.com",
            };
            await SeedRequestAsync(request);

            var service = CreateService();
            var result = await service.ResendInvitationAsync(request.AccessRequestId);

            Assert.AreEqual("https://test-redeem.example.com", result);
        }

        [TestMethod]
        public async Task DeleteAsync_NotFound_ThrowsKeyNotFoundException()
        {
            // storage comes from the fixture
            var service = CreateService();

            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
                () => service.DeleteAsync(Guid.NewGuid()));
        }

        [TestMethod]
        public async Task DeleteAsync_PendingRequest_ThrowsInvalidOperation()
        {
            // storage comes from the fixture
            var request = CreatePendingRequest();
            await SeedRequestAsync(request);

            var service = CreateService();

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => service.DeleteAsync(request.AccessRequestId));
        }

        [TestMethod]
        public async Task DeleteAsync_ApprovedRequest_DeletesRecord()
        {
            // storage comes from the fixture
            var request = new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Test User",
                Email = "test@example.com",
                Status = RequestStatus.Approved,
                RequestedAt = DateTime.UtcNow,
            };
            await SeedRequestAsync(request);

            var service = CreateService();
            await service.DeleteAsync(request.AccessRequestId);

            Assert.AreEqual(0, await CountRequestsAsync());
        }

        [TestMethod]
        public async Task DeleteAsync_DeclinedRequest_DeletesRecord()
        {
            // storage comes from the fixture
            var request = new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Test User",
                Email = "test@example.com",
                Status = RequestStatus.Declined,
                RequestedAt = DateTime.UtcNow,
            };
            await SeedRequestAsync(request);

            var service = CreateService();
            await service.DeleteAsync(request.AccessRequestId);

            Assert.AreEqual(0, await CountRequestsAsync());
        }

        [TestMethod]
        public async Task SubmitAsync_NewEmail_AddsRequest()
        {
            // storage comes from the fixture
            var service = CreateService();

            await service.SubmitAsync("New User", "new@example.com");

            Assert.AreEqual(1, await CountRequestsAsync());
            var saved = (await CreateService().GetAllAsync()).Single();
            Assert.AreEqual("new@example.com", saved.Email);
            Assert.AreEqual(RequestStatus.Pending, saved.Status);
        }

        [TestMethod]
        public async Task SubmitAsync_DuplicatePendingEmail_ThrowsInvalidOperation()
        {
            // storage comes from the fixture
            await SeedRequestAsync(CreatePendingRequest("dup@example.com"));

            var service = CreateService();

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => service.SubmitAsync("Dup User", "dup@example.com"));
        }

        [TestMethod]
        public async Task GetPendingAsync_ReturnsOnlyPendingRequests()
        {
            // storage comes from the fixture
            await SeedRequestAsync(CreatePendingRequest("pending@example.com"));
            await SeedRequestAsync(new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Approved User",
                Email = "approved@example.com",
                Status = RequestStatus.Approved,
                RequestedAt = DateTime.UtcNow,
            });

            var service = CreateService();
            var result = (await service.GetPendingAsync()).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("pending@example.com", result[0].Email);
        }

        [TestMethod]
        public async Task ApproveAsync_RequestNotFound_ThrowsKeyNotFoundException()
        {
            // storage comes from the fixture
            var service = CreateService();

            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
                () => service.ApproveAsync(Guid.NewGuid(), "admin-oid"));
        }

        [TestMethod]
        public async Task ApproveAsync_WithEmailService_SendsEmailAndReturnsRedeemUrl()
        {
            // storage comes from the fixture
            await _fixture.SeedUserAsync("admin-oid", AppRole.DiaryAdmin);
            var request = CreatePendingRequest();
            await SeedRequestAsync(request);

            var emailMock = new Mock<IEmailService>();
            emailMock
                .Setup(e => e.SendInvitationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var service = CreateService(emailMock.Object);
            var result = await service.ApproveAsync(request.AccessRequestId, "admin-oid");

            Assert.AreEqual("https://test-redeem.example.com", result);
            emailMock.Verify(
                e => e.SendInvitationAsync(request.Email, request.DisplayName, "https://test-redeem.example.com"),
                Times.Once);
        }

        [TestMethod]
        public async Task DeclineAsync_RequestNotFound_ThrowsKeyNotFoundException()
        {
            // storage comes from the fixture
            var service = CreateService();

            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
                () => service.DeclineAsync(Guid.NewGuid(), "admin-oid"));
        }

        [TestMethod]
        public async Task DeclineAsync_HappyPath_SetsDeclinedStatus()
        {
            // storage comes from the fixture
            await _fixture.SeedUserAsync("admin-oid", AppRole.DiaryAdmin);
            var request = CreatePendingRequest();
            await SeedRequestAsync(request);

            var service = CreateService();
            await service.DeclineAsync(request.AccessRequestId, "admin-oid");

            var updated = (await CreateService().GetAllAsync()).SingleOrDefault(r => r.AccessRequestId == request.AccessRequestId);
            Assert.IsNotNull(updated);
            Assert.AreEqual(RequestStatus.Declined, updated.Status);
        }

        [TestMethod]
        public async Task ResendInvitationAsync_RequestNotFound_ThrowsKeyNotFoundException()
        {
            // storage comes from the fixture
            var service = CreateService();

            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(
                () => service.ResendInvitationAsync(Guid.NewGuid()));
        }

        [TestMethod]
        public async Task ResendInvitationAsync_NoInviteUrl_ReturnsNull()
        {
            // storage comes from the fixture
            var request = new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Test User",
                Email = "test@example.com",
                Status = RequestStatus.Approved,
                RequestedAt = DateTime.UtcNow,
                InviteRedeemUrl = null,
            };
            await SeedRequestAsync(request);

            var service = CreateService();
            var result = await service.ResendInvitationAsync(request.AccessRequestId);

            Assert.IsNull(result);
        }

        private static AccessRequestDto CreatePendingRequest(string email = "test@example.com") => new AccessRequestDto
        {
            AccessRequestId = Guid.NewGuid(),
            DisplayName = "Test User",
            Email = email,
            Status = RequestStatus.Pending,
            RequestedAt = DateTime.UtcNow,
        };

        private static ILogger<AccessRequestService> CreateLogger()
            => new Mock<ILogger<AccessRequestService>>().Object;

        private static IGraphService CreateMockGraph(string redeemUrl = "https://test-redeem.example.com")
        {
            var mock = new Mock<IGraphService>();
            mock.Setup(g => g.SendInvitationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(redeemUrl);
            return mock.Object;
        }

        private AccessRequestService CreateService(IEmailService? emailService = null) =>
            new AccessRequestService(
                _fixture.Tables,
                new UserService(_fixture.Tables, new ConfigurationBuilder().Build()),
                CreateMockGraph(),
                CreateLogger(),
                emailService);

        private async Task SeedRequestAsync(AccessRequestDto request)
        {
            var entity = TableJson.ToEntity(
                StorageKeys.RequestPartition,
                request.AccessRequestId.ToString("N"),
                request,
                e =>
                {
                    e["Status"] = request.Status.ToStoredValue();
                    e["Email"] = request.Email;
                    e["RequestedAt"] = request.RequestedAt;
                });

            await _fixture.Tables.AccessRequests.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        private async Task<int> CountRequestsAsync() =>
            (await TableJson.QueryAsync(_fixture.Tables.AccessRequests)).Count;
    }
}
