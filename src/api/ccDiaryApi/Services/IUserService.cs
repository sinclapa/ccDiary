// <copyright file="IUserService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Model;

    public interface IUserService
    {
        Task<AppUserDto?> GetUserByOidAsync(string oid);

        Task<AppUserDto?> GetOrCreateUserAsync(string oid, string email, string displayName);

        Task SeedBootstrapAdminAsync();
    }
}
