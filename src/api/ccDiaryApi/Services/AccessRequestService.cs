// <copyright file="AccessRequestService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;
    using Microsoft.EntityFrameworkCore;

    public class AccessRequestService : IAccessRequestService
    {
        private readonly DiaryDatabaseContext _context;
        private readonly IGraphService _graphService;

        public AccessRequestService(DiaryDatabaseContext context, IGraphService graphService)
        {
            _context = context;
            _graphService = graphService;
        }

        public async Task SubmitAsync(string displayName, string email)
        {
            var hasPending = await _context.AccessRequests
                .AnyAsync(r => r.Email == email && r.Status == RequestStatus.Pending);

            if (hasPending)
            {
                throw new InvalidOperationException("A pending request already exists for this email address.");
            }

            _context.AccessRequests.Add(new AccessRequestDTO
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = displayName,
                Email = email,
                Status = RequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
            });

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<AccessRequestDTO>> GetPendingAsync()
        {
            return await _context.AccessRequests
                .Where(r => r.Status == RequestStatus.Pending)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<string?> ApproveAsync(Guid requestId, string adminOid)
        {
            var request = await _context.AccessRequests.FindAsync(requestId)
                ?? throw new KeyNotFoundException($"Access request {requestId} not found.");

            var admin = await _context.AppUsers.FirstOrDefaultAsync(u => u.EntraObjectId == adminOid)
                ?? throw new InvalidOperationException("Admin user not found.");

            request.Status = RequestStatus.Approved;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedByUserId = admin.UserId;

            await _context.SaveChangesAsync();

            var redeemUrl = await _graphService.SendInvitationAsync(request.Email, request.DisplayName);
            return string.IsNullOrEmpty(redeemUrl) ? null : redeemUrl;
        }

        public async Task DeclineAsync(Guid requestId, string adminOid)
        {
            var request = await _context.AccessRequests.FindAsync(requestId)
                ?? throw new KeyNotFoundException($"Access request {requestId} not found.");

            var admin = await _context.AppUsers.FirstOrDefaultAsync(u => u.EntraObjectId == adminOid)
                ?? throw new InvalidOperationException("Admin user not found.");

            request.Status = RequestStatus.Declined;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedByUserId = admin.UserId;

            await _context.SaveChangesAsync();
        }
    }
}
