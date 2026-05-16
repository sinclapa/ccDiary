// <copyright file="IEmailService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    public interface IEmailService
    {
        Task SendInvitationAsync(string toEmail, string toName, string inviteRedeemUrl);
    }
}
