// <copyright file="IAccessRequestService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Model;

    public interface IAccessRequestService
    {
        Task SubmitAsync(string displayName, string email);

        Task<IEnumerable<AccessRequestDto>> GetPendingAsync();

        Task<IEnumerable<AccessRequestDto>> GetAllAsync();

        Task<string?> ApproveAsync(Guid requestId, string adminOid);

        Task DeclineAsync(Guid requestId, string adminOid);

        Task<string?> ResendInvitationAsync(Guid requestId);

        Task DeleteAsync(Guid requestId);
    }
}
