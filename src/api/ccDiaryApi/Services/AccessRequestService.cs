// <copyright file="AccessRequestService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Data.Storage;
    using global::Azure.Data.Tables;

    /// <summary>
    /// Registration requests and the invitation flow around them.
    /// </summary>
    /// <remarks>
    /// Rows sit in one constant partition with <c>Status</c> broken out as a column,
    /// rather than partitioned by status. Status is mutable, and using it as the
    /// partition key would make every approval a cross-partition write-then-delete that
    /// cannot be transactional. A constant partition keeps the status filter server-side
    /// and each transition a single atomic upsert.
    /// </remarks>
    public class AccessRequestService : IAccessRequestService
    {
        private readonly ITableStore _tables;
        private readonly IUserService _userService;
        private readonly IGraphService _graphService;
        private readonly IEmailService? _emailService;
        private readonly ILogger<AccessRequestService> _logger;

        /// <summary>Initializes a new instance of the <see cref="AccessRequestService"/> class.</summary>
        /// <param name="tables">The table store.</param>
        /// <param name="userService">Used to resolve the acting administrator.</param>
        /// <param name="graphService">Sends the Entra invitation.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="emailService">Optional email sender.</param>
        public AccessRequestService(
            ITableStore tables,
            IUserService userService,
            IGraphService graphService,
            ILogger<AccessRequestService> logger,
            IEmailService? emailService = null)
        {
            _tables = tables;
            _userService = userService;
            _graphService = graphService;
            _logger = logger;
            _emailService = emailService;
        }

        /// <inheritdoc/>
        public async Task SubmitAsync(string displayName, string email)
        {
            var pending = await QueryAsync(
                TableClient.CreateQueryFilter(
                    $"PartitionKey eq {StorageKeys.RequestPartition} and Status eq {RequestStatus.Pending.ToStoredValue()} and Email eq {email}"));

            if (pending.Count > 0)
            {
                throw new InvalidOperationException("A pending request already exists for this email address.");
            }

            await UpsertAsync(new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = displayName,
                Email = email,
                Status = RequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
            });
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AccessRequestDto>> GetPendingAsync()
        {
            var requests = await QueryAsync(
                TableClient.CreateQueryFilter(
                    $"PartitionKey eq {StorageKeys.RequestPartition} and Status eq {RequestStatus.Pending.ToStoredValue()}"));

            return requests.OrderBy(r => r.RequestedAt).ToList();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AccessRequestDto>> GetAllAsync()
        {
            var requests = await QueryAsync(
                TableClient.CreateQueryFilter($"PartitionKey eq {StorageKeys.RequestPartition}"));

            return requests.OrderBy(r => r.RequestedAt).ToList();
        }

        /// <inheritdoc/>
        public async Task<string?> ApproveAsync(Guid requestId, string adminOid)
        {
            var request = await RequireAsync(requestId);
            var admin = await RequireAdminAsync(adminOid);

            request.Status = RequestStatus.Approved;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedByUserId = admin.UserId;
            await UpsertAsync(request);

            var redeemUrl = await _graphService.SendInvitationAsync(request.Email, request.DisplayName);

            if (!string.IsNullOrEmpty(redeemUrl))
            {
                request.InviteRedeemUrl = redeemUrl;
                await UpsertAsync(request);
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

        /// <inheritdoc/>
        public async Task DeclineAsync(Guid requestId, string adminOid)
        {
            var request = await RequireAsync(requestId);
            var admin = await RequireAdminAsync(adminOid);

            request.Status = RequestStatus.Declined;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedByUserId = admin.UserId;

            await UpsertAsync(request);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(Guid requestId)
        {
            var request = await RequireAsync(requestId);

            if (request.Status == RequestStatus.Pending)
            {
                throw new InvalidOperationException("Pending requests cannot be deleted. Approve or decline first.");
            }

            await _tables.AccessRequests.DeleteEntityAsync(
                StorageKeys.RequestPartition,
                RowKey(requestId));
        }

        /// <inheritdoc/>
        public async Task<string?> ResendInvitationAsync(Guid requestId)
        {
            var request = await RequireAsync(requestId);

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

        private static string RowKey(Guid requestId) => requestId.ToString("N");

        private async Task<List<AccessRequestDto>> QueryAsync(string filter)
        {
            var rows = await TableJson.QueryAsync(_tables.AccessRequests, filter);
            return rows
                .Select(TableJson.FromEntity<AccessRequestDto>)
                .Where(r => r != null)
                .Select(r => r!)
                .ToList();
        }

        private async Task<AccessRequestDto> RequireAsync(Guid requestId)
        {
            var row = await TableJson.GetIfExistsAsync(
                _tables.AccessRequests,
                StorageKeys.RequestPartition,
                RowKey(requestId));

            var request = row == null ? null : TableJson.FromEntity<AccessRequestDto>(row);
            return request ?? throw new KeyNotFoundException($"Access request {requestId} not found.");
        }

        private async Task<AppUserDto> RequireAdminAsync(string adminOid)
        {
            return await _userService.GetUserByOidAsync(adminOid)
                ?? throw new InvalidOperationException("Admin user not found.");
        }

        private async Task UpsertAsync(AccessRequestDto request)
        {
            var entity = TableJson.ToEntity(
                StorageKeys.RequestPartition,
                RowKey(request.AccessRequestId),
                request,
                e =>
                {
                    e["Status"] = request.Status.ToStoredValue();
                    e["Email"] = request.Email;
                    e["RequestedAt"] = request.RequestedAt;
                });

            await _tables.AccessRequests.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }
    }
}
