// <copyright file="UserService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Data.Storage;
    using global::Azure.Data.Tables;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Application users, keyed by their Entra object id.
    /// </summary>
    /// <remarks>
    /// The row key is the <c>oid</c>, which gives uniqueness for free — replacing the
    /// unique index the relational model needed — and turns
    /// <see cref="GetUserByOidAsync"/> into a point read. That matters because the
    /// middleware calls it on every authenticated request to resolve the caller's role.
    /// </remarks>
    public class UserService : IUserService
    {
        private readonly ITableStore _tables;
        private readonly IConfiguration _configuration;

        /// <summary>Initializes a new instance of the <see cref="UserService"/> class.</summary>
        /// <param name="tables">The table store.</param>
        /// <param name="configuration">The application configuration.</param>
        public UserService(ITableStore tables, IConfiguration configuration)
        {
            _tables = tables;
            _configuration = configuration;
        }

        /// <inheritdoc/>
        public async Task<AppUserDto?> GetUserByOidAsync(string oid)
        {
            var row = await TableJson.GetIfExistsAsync(
                _tables.AppUsers,
                StorageKeys.UserPartition,
                StorageKeys.SanitiseKey(oid));

            return row == null ? null : TableJson.FromEntity<AppUserDto>(row);
        }

        /// <inheritdoc/>
        public async Task<AppUserDto?> GetOrCreateUserAsync(string oid, string email, string displayName)
        {
            var existing = await GetUserByOidAsync(oid);
            if (existing != null)
            {
                return existing;
            }

            var approved = await FindApprovedRequestAsync(email);
            if (approved == null)
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

            await UpsertAsync(newUser);
            return newUser;
        }

        /// <inheritdoc/>
        public async Task SeedBootstrapAdminAsync()
        {
            var objectId = _configuration["BootstrapAdmin:ObjectId"];
            if (string.IsNullOrEmpty(objectId))
            {
                return;
            }

            // Small table, and this runs once per boot: a partition scan projecting only
            // the Role column is cheaper than maintaining a second index.
            var rows = await TableJson.QueryAsync(
                _tables.AppUsers,
                TableClient.CreateQueryFilter($"PartitionKey eq {StorageKeys.UserPartition}"),
                new[] { "Role" });

            var adminExists = rows.Any(r =>
                string.Equals(r.GetString("Role"), AdminRoleValue, StringComparison.Ordinal));

            if (adminExists)
            {
                return;
            }

            var email = _configuration["BootstrapAdmin:Email"] ?? string.Empty;
            var displayName = _configuration["BootstrapAdmin:DisplayName"] ?? email;

            await UpsertAsync(new AppUserDto
            {
                UserId = Guid.NewGuid(),
                EntraObjectId = objectId,
                DisplayName = displayName,
                Email = email,
                Role = AppRole.DiaryAdmin,
                CreatedAt = DateTime.UtcNow,
            });
        }

        /// <summary>Gets the stored form of <see cref="AppRole.DiaryAdmin"/>.</summary>
        private static string AdminRoleValue => AppRole.DiaryAdmin.ToStoredValue();

        private async Task<AccessRequestDto?> FindApprovedRequestAsync(string email)
        {
            // CreateQueryFilter escapes the interpolated values; building the filter by
            // string concatenation would let an address containing a quote alter it.
            var rows = await TableJson.QueryAsync(
                _tables.AccessRequests,
                TableClient.CreateQueryFilter(
                    $"PartitionKey eq {StorageKeys.RequestPartition} and Status eq {RequestStatus.Approved.ToStoredValue()} and Email eq {email}"));

            return rows.Count == 0 ? null : TableJson.FromEntity<AccessRequestDto>(rows[0]);
        }

        private async Task UpsertAsync(AppUserDto user)
        {
            var entity = TableJson.ToEntity(
                StorageKeys.UserPartition,
                StorageKeys.SanitiseKey(user.EntraObjectId),
                user,
                e =>
                {
                    e["UserId"] = user.UserId.ToString();
                    e["Email"] = user.Email;
                    e["Role"] = user.Role.ToStoredValue();
                });

            await _tables.AppUsers.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }
    }
}
