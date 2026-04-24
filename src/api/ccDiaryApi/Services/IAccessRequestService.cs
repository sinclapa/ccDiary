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

        Task<string?> ApproveAsync(Guid requestId, string adminOid);

        Task DeclineAsync(Guid requestId, string adminOid);
    }
}
