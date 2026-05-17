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
        private readonly IEmailService? _emailService;
        private readonly ILogger<AccessRequestService> _logger;

        public AccessRequestService(
            DiaryDatabaseContext context,
            IGraphService graphService,
            ILogger<AccessRequestService> logger,
            IEmailService? emailService = null)
        {
            _context = context;
            _graphService = graphService;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task SubmitAsync(string displayName, string email)
        {
            var hasPending = await _context.AccessRequests
                .AnyAsync(r => r.Email == email && r.Status == RequestStatus.Pending);

            if (hasPending)
            {
                throw new InvalidOperationException("A pending request already exists for this email address.");
            }

            _context.AccessRequests.Add(new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = displayName,
                Email = email,
                Status = RequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
            });

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<AccessRequestDto>> GetPendingAsync()
        {
            return await _context.AccessRequests
                .Where(r => r.Status == RequestStatus.Pending)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<AccessRequestDto>> GetAllAsync()
        {
            return await _context.AccessRequests
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

            if (!string.IsNullOrEmpty(redeemUrl))
            {
                request.InviteRedeemUrl = redeemUrl;
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(redeemUrl) && _emailService != null)
            {
                try
                {
                    await _emailService.SendInvitationAsync(request.Email, request.DisplayName, redeemUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send invitation email to {Email} — Entra invite succeeded. Share redeemUrl manually.", request.Email);
                }
            }
            else if (_emailService == null)
            {
                _logger.LogWarning("Email service not configured — invitation email not sent for {Email}.", request.Email);
            }

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

        public async Task DeleteAsync(Guid requestId)
        {
            var request = await _context.AccessRequests.FindAsync(requestId)
                ?? throw new KeyNotFoundException($"Access request {requestId} not found.");

            if (request.Status == RequestStatus.Pending)
            {
                throw new InvalidOperationException("Pending requests cannot be deleted. Approve or decline first.");
            }

            _context.AccessRequests.Remove(request);
            await _context.SaveChangesAsync();
        }

        public async Task<string?> ResendInvitationAsync(Guid requestId)
        {
            var request = await _context.AccessRequests.FindAsync(requestId)
                ?? throw new KeyNotFoundException($"Access request {requestId} not found.");

            if (string.IsNullOrEmpty(request.InviteRedeemUrl))
            {
                return null;
            }

            if (_emailService != null)
            {
                await _emailService.SendInvitationAsync(request.Email, request.DisplayName, request.InviteRedeemUrl);
            }
            else
            {
                _logger.LogWarning("Email service not configured — resend invitation email not sent for {Email}.", request.Email);
            }

            return request.InviteRedeemUrl;
        }
    }
}
