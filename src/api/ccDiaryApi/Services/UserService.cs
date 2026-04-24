// <copyright file="UserService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;

    public class UserService : IUserService
    {
        private readonly DiaryDatabaseContext _context;
        private readonly IConfiguration _configuration;

        public UserService(DiaryDatabaseContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AppUserDto?> GetUserByOidAsync(string oid)
        {
            return await _context.AppUsers
                .FirstOrDefaultAsync(u => u.EntraObjectId == oid);
        }

        public async Task<AppUserDto?> GetOrCreateUserAsync(string oid, string email, string displayName)
        {
            var existing = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.EntraObjectId == oid);
            if (existing != null)
            {
                return existing;
            }

            var approvedRequest = await _context.AccessRequests
                .FirstOrDefaultAsync(r => r.Email == email && r.Status == RequestStatus.Approved);
            if (approvedRequest == null)
            {
                return null;
            }

            var newUser = new AppUserDto
            {
                UserId = Guid.NewGuid(),
                EntraObjectId = oid,
                DisplayName = displayName,
                Email = email,
                Role = AppRole.DiaryContributor,
                CreatedAt = DateTime.UtcNow,
            };

            _context.AppUsers.Add(newUser);
            await _context.SaveChangesAsync();
            return newUser;
        }

        public async Task SeedBootstrapAdminAsync()
        {
            var objectId = _configuration["BootstrapAdmin:ObjectId"];
            if (string.IsNullOrEmpty(objectId))
            {
                return;
            }

            var adminExists = await _context.AppUsers
                .AnyAsync(u => u.Role == AppRole.DiaryAdmin);

            if (adminExists)
            {
                return;
            }

            var email = _configuration["BootstrapAdmin:Email"] ?? string.Empty;
            var displayName = _configuration["BootstrapAdmin:DisplayName"] ?? email;

            _context.AppUsers.Add(new AppUserDto
            {
                UserId = Guid.NewGuid(),
                EntraObjectId = objectId,
                DisplayName = displayName,
                Email = email,
                Role = AppRole.DiaryAdmin,
                CreatedAt = DateTime.UtcNow,
            });

            await _context.SaveChangesAsync();
        }
    }
}
