// <copyright file="AccessRequestServiceTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
    using Microsoft.EntityFrameworkCore;
    using Moq;

    [TestClass]
    public class AccessRequestServiceTest
    {
        [TestMethod]
        public async Task ApproveAsync_AdminNotFound_ThrowsInvalidOperation()
        {
            using var db = CreateDb();
            var request = new AccessRequestDTO
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Test User",
                Email = "test@example.com",
                Status = RequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
            };
            db.AccessRequests.Add(request);
            await db.SaveChangesAsync();

            var service = new AccessRequestService(db, CreateMockGraph());

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => service.ApproveAsync(request.AccessRequestId, "non-existent-oid"));
        }

        [TestMethod]
        public async Task DeclineAsync_AdminNotFound_ThrowsInvalidOperation()
        {
            using var db = CreateDb();
            var request = new AccessRequestDTO
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = "Test User",
                Email = "test@example.com",
                Status = RequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
            };
            db.AccessRequests.Add(request);
            await db.SaveChangesAsync();

            var service = new AccessRequestService(db, CreateMockGraph());

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => service.DeclineAsync(request.AccessRequestId, "non-existent-oid"));
        }

        private static DiaryDatabaseContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<DiaryDatabaseContext>()
                .UseInMemoryDatabase("AccessRequestServiceTest_" + Guid.NewGuid())
                .Options;
            var db = new DiaryDatabaseContext(options);
            db.Database.EnsureCreated();
            return db;
        }

        private static IGraphService CreateMockGraph(string redeemUrl = "https://test-redeem.example.com")
        {
            var mock = new Mock<IGraphService>();
            mock.Setup(g => g.SendInvitationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(redeemUrl);
            return mock.Object;
        }
    }
}
