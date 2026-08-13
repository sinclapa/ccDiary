// <copyright file="DiaryService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Data.Storage;
    using global::Azure.Data.Tables;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Diaries, held in a single partition.
    /// </summary>
    /// <remarks>
    /// The listing endpoint is global — search and sort across every diary — so
    /// partitioning by owner would turn one query into a fan-out. The set is small
    /// enough that one partition is comfortable; see docs/data-model.md for the limit.
    /// </remarks>
    public class DiaryService : IDiaryService
    {
        private static readonly string[] EntryLocatorColumns = new[] { "RowKey", "DiaryEntryId" };

        private readonly ITableStore _tables;
        private readonly IBlobStore _blobs;
        private readonly StorageOptions _options;

        /// <summary>Initializes a new instance of the <see cref="DiaryService"/> class.</summary>
        /// <param name="tables">The table store.</param>
        /// <param name="blobs">The blob store.</param>
        /// <param name="options">The storage options.</param>
        public DiaryService(ITableStore tables, IBlobStore blobs, IOptions<StorageOptions> options)
        {
            _tables = tables;
            _blobs = blobs;
            _options = options.Value;
        }

        /// <inheritdoc/>
        public async Task<DiaryDTO> CreateAsync(DiaryDTO diary)
        {
            diary.DiaryId ??= Guid.NewGuid();
            await UpsertAsync(diary);
            return diary;
        }

        /// <inheritdoc/>
        public async Task<DiaryDTO> UpdateAsync(DiaryDTO diary)
        {
            diary.DiaryId ??= Guid.NewGuid();
            await UpsertAsync(diary);
            return diary;
        }

        /// <summary>
        /// Deletes a diary, its entries and their blobs.
        /// </summary>
        /// <param name="diary">The diary to delete.</param>
        /// <returns>A task representing the operation.</returns>
        /// <remarks>
        /// The relational model did this with a foreign key cascade. Here it is
        /// application code, ordered children-first so a failure part way through leaves
        /// a diary that can simply be deleted again rather than an unreachable set of
        /// orphaned entries.
        /// </remarks>
        public async Task DeleteAsync(DiaryDTO diary)
        {
            if (diary.DiaryId is not Guid diaryId)
            {
                return;
            }

            var partition = diaryId.ToString("N");

            var entryRows = await TableJson.QueryAsync(
                _tables.DiaryEntries,
                TableClient.CreateQueryFilter($"PartitionKey eq {partition}"),
                EntryLocatorColumns);

            await _blobs.DeleteByPrefixAsync(_options.ImagesContainer, $"{partition}/");

            foreach (var row in entryRows)
            {
                var entryId = row.GetString("DiaryEntryId");
                if (Guid.TryParse(entryId, out var parsed))
                {
                    await _blobs.DeleteIfExistsAsync(
                        _options.ContentContainer,
                        StorageKeys.EntryJsonBlobKey(parsed));
                }
            }

            await TableJson.DeleteBatchAsync(
                _tables.DiaryEntries,
                partition,
                entryRows.Select(r => r.RowKey));

            await _tables.Diaries.DeleteEntityAsync(StorageKeys.DiaryPartition, partition);
        }

        /// <inheritdoc/>
        public async Task<DiaryDTO?> GetDiaryAsync(Guid diaryId)
        {
            var row = await TableJson.GetIfExistsAsync(
                _tables.Diaries,
                StorageKeys.DiaryPartition,
                diaryId.ToString("N"));

            return row == null ? null : TableJson.FromEntity<DiaryDTO>(row);
        }

        /// <inheritdoc/>
        public async Task<PagedResultDTO<DiaryDTO>> GetDiariesAsync(int page, int pageSize, string? search = null)
        {
            // The Table filter grammar has no substring operator, so the search and the
            // paging both happen here rather than server-side.
            var rows = await TableJson.QueryAsync(
                _tables.Diaries,
                TableClient.CreateQueryFilter($"PartitionKey eq {StorageKeys.DiaryPartition}"));

            var diaries = rows
                .Select(TableJson.FromEntity<DiaryDTO>)
                .Where(d => d != null)
                .Select(d => d!)
                .AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                diaries = diaries.Where(d =>
                    (d.Title != null && d.Title.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (d.Description != null && d.Description.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            var matched = diaries.ToList();

            var items = matched
                .OrderBy(d => d.Author, StringComparer.Ordinal)
                .ThenBy(d => d.Title, StringComparer.Ordinal)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResultDTO<DiaryDTO>
            {
                Items = items,
                TotalCount = matched.Count,
                Page = page,
                PageSize = pageSize,
            };
        }

        private async Task UpsertAsync(DiaryDTO diary)
        {
            var entity = TableJson.ToEntity(
                StorageKeys.DiaryPartition,
                diary.DiaryId!.Value.ToString("N"),
                diary,
                e =>
                {
                    e["DiaryId"] = diary.DiaryId!.Value.ToString();
                    e["Title"] = diary.Title;
                    e["Author"] = diary.Author;
                    e["OwnerId"] = diary.OwnerId;
                });

            await _tables.Diaries.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }
    }
}
