// <copyright file="IUserService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Model;

    public interface IUserService
    {
        Task<AppUserDTO?> GetUserByOidAsync(string oid);

        Task<AppUserDTO?> GetOrCreateUserAsync(string oid, string email, string displayName);

        Task SeedBootstrapAdminAsync();
    }
}
