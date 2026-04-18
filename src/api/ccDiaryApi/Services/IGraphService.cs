// <copyright file="IGraphService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    public interface IGraphService
    {
        Task<string> SendInvitationAsync(string email, string displayName);
    }
}
