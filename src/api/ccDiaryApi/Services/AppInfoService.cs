// <copyright file="AppInfoService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Data.Storage;

    /// <summary>Reads the singleton application info row.</summary>
    public class AppInfoService : IAppInfoService
    {
        private readonly ITableStore _tables;

        /// <summary>Initializes a new instance of the <see cref="AppInfoService"/> class.</summary>
        /// <param name="tables">The table store.</param>
        public AppInfoService(ITableStore tables)
        {
            _tables = tables;
        }

        /// <inheritdoc/>
        public async Task<AppInfoDTO?> GetAppInfoAsync()
        {
            var row = await TableJson.GetIfExistsAsync(
                _tables.AppInfo,
                StorageKeys.AppInfoPartition,
                StorageKeys.AppInfoRow);

            return row == null ? null : TableJson.FromEntity<AppInfoDTO>(row);
        }
    }
}
