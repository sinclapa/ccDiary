// <copyright file="DiaryEntryService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Data.Storage;
    using global::Azure;
    using global::Azure.Data.Tables;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Diary entries, partitioned by diary and keyed by date.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The row key is a fixed-width UTC timestamp followed by the entry id. Because
    /// Table Storage sorts row keys lexicographically within a partition, that single
    /// choice provides date ordering and date-range filtering server-side, with no
    /// secondary index.
    /// </para>
    /// <para>
    /// Images do not live in the row: a Table entity caps at 1 MB and a single string
    /// property at 64 KB, while real images reach several megabytes of base64. They are
    /// stored as blobs and re-encoded on read, so the HTTP contract is unchanged.
    /// </para>
    /// </remarks>
    public class DiaryEntryService : IDiaryEntryService
    {
        private const string ColumnEntryId = "DiaryEntryId";
        private const string ColumnDate = "Date";
        private const string ColumnHasImage = "HasImage";
        private const string ColumnImageContentType = "ImageContentType";
        private const string ColumnJsonInBlob = "JsonInBlob";

        private static readonly string[] KeysAndDate = new[] { "RowKey", ColumnDate };
        private static readonly string[] LocatorColumns = new[] { "PartitionKey", "RowKey", ColumnEntryId };

        private readonly ITableStore _tables;
        private readonly IBlobStore _blobs;
        private readonly StorageOptions _options;

        /// <summary>Initializes a new instance of the <see cref="DiaryEntryService"/> class.</summary>
        /// <param name="tables">The table store.</param>
        /// <param name="blobs">The blob store.</param>
        /// <param name="options">The storage options.</param>
        public DiaryEntryService(ITableStore tables, IBlobStore blobs, IOptions<StorageOptions> options)
        {
            _tables = tables;
            _blobs = blobs;
            _options = options.Value;
        }

        /// <inheritdoc/>
        public async Task<DiaryEntryDTO> CreateDiaryEntryAsync(DiaryEntryDTO diaryEntry)
        {
            if (diaryEntry.Date == null || diaryEntry.Date == DateTime.MinValue)
            {
                throw new ArgumentException($"Date has to be not null and greater than {DateTime.MinValue}.");
            }

            diaryEntry.DiaryEntryId ??= Guid.NewGuid();
            await WriteAsync(diaryEntry);
            return diaryEntry;
        }

        /// <inheritdoc/>
        public async Task<DiaryEntryDTO> UpdateDiaryEntryAsync(DiaryEntryDTO diaryEntry)
        {
            if (diaryEntry.Date == null || diaryEntry.Date == DateTime.MinValue)
            {
                throw new ArgumentException($"Date has to be not null and greater than {DateTime.MinValue}.");
            }

            diaryEntry.DiaryEntryId ??= Guid.NewGuid();

            // The row key encodes the date, and row keys are immutable, so an edit that
            // moves the date has to land at a new key. Both keys share a partition, so
            // the write and the delete go in one transaction and cannot half-apply.
            var existing = await FindLocatorAsync(diaryEntry.DiaryEntryId.Value);
            var newRowKey = StorageKeys.EntryRowKey(diaryEntry.Date, diaryEntry.DiaryEntryId.Value);

            if (existing != null && existing.Value.RowKey != newRowKey)
            {
                var entity = await BuildEntityAsync(diaryEntry);
                await _tables.DiaryEntries.SubmitTransactionAsync(new[]
                {
                    new TableTransactionAction(TableTransactionActionType.UpsertReplace, entity),
                    new TableTransactionAction(
                        TableTransactionActionType.Delete,
                        new TableEntity(existing.Value.PartitionKey, existing.Value.RowKey) { ETag = ETag.All }),
                });
                return diaryEntry;
            }

            await WriteAsync(diaryEntry);
            return diaryEntry;
        }

        /// <inheritdoc/>
        public async Task DeleteDiaryEntryAsync(DiaryEntryDTO diaryEntry)
        {
            if (diaryEntry.DiaryEntryId is not Guid entryId)
            {
                return;
            }

            var locator = await FindLocatorAsync(entryId);
            if (locator == null)
            {
                return;
            }

            await _blobs.DeleteIfExistsAsync(
                _options.ImagesContainer,
                StorageKeys.ImageBlobKey(diaryEntry.DiaryId, entryId));
            await _blobs.DeleteIfExistsAsync(
                _options.ContentContainer,
                StorageKeys.EntryJsonBlobKey(entryId));

            await _tables.DiaryEntries.DeleteEntityAsync(locator.Value.PartitionKey, locator.Value.RowKey);
        }

        /// <inheritdoc/>
        public async Task<DiaryEntryDTO?> GetDiaryEntryAsync(Guid id)
        {
            // The route carries no diary id, so this is a cross-partition filter on the
            // broken-out column. One request at this volume.
            // Note: only values may be interpolated here — CreateQueryFilter quotes every
            // hole, so a column name has to be written literally.
            var entryId = id.ToString("N");
            var rows = await TableJson.QueryAsync(
                _tables.DiaryEntries,
                TableClient.CreateQueryFilter($"DiaryEntryId eq {entryId}"));

            if (rows.Count == 0)
            {
                return null;
            }

            return await HydrateAsync(rows[0], withImage: true);
        }

        /// <inheritdoc/>
        public async Task<List<DiaryEntryDTO>> GetDiaryEntriesAsync(Guid diaryId)
        {
            var rows = await TableJson.QueryAsync(
                _tables.DiaryEntries,
                TableClient.CreateQueryFilter($"PartitionKey eq {Partition(diaryId)}"));

            return await HydrateAllAsync(rows, withImage: true);
        }

        /// <inheritdoc/>
        public async Task<List<DiaryEntryDTO>> GetDiaryEntriesAsync(Guid diaryId, DateTime from, DateTime until)
        {
            var rows = await TableJson.QueryAsync(_tables.DiaryEntries, RangeFilter(diaryId, from, until));
            return await HydrateAllAsync(rows, withImage: true);
        }

        /// <inheritdoc/>
        public async Task<List<int>> SearchDiaryEntriesAsync(
            Guid diaryId,
            DateTime from,
            DateTime until,
            SearchType searchType,
            int utcOffsetMinutes = 0)
        {
            Func<DateTime, int> part = searchType switch
            {
                SearchType.Year => d => d.AddMinutes(utcOffsetMinutes).Year,
                SearchType.Month => d => d.AddMinutes(utcOffsetMinutes).Month,
                SearchType.Day => d => d.AddMinutes(utcOffsetMinutes).Day,
                _ => throw new ArgumentException($"Unhandled SearchType [{searchType}]"),
            };

            // Only the dates are needed, so nothing else comes off the wire. The
            // relational version accidentally materialised whole entities here,
            // including their images.
            var rows = await TableJson.QueryAsync(
                _tables.DiaryEntries,
                RangeFilter(diaryId, from, until),
                KeysAndDate);

            return rows
                .Select(r => r.GetDateTime(ColumnDate))
                .Where(d => d.HasValue)
                .Select(d => part(DateTime.SpecifyKind(d!.Value, DateTimeKind.Utc)))
                .Distinct()
                .OrderBy(v => v)
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<PagedResultDTO<DiaryEntryDTO>> TextSearchDiaryEntriesAsync(
            Guid diaryId,
            string search,
            int page = 1,
            int pageSize = 20)
        {
            // No substring operator exists in the Table filter grammar, so matching is
            // done here. Images are already out of the row, so the scan stays cheap, and
            // only the returned page has its images fetched.
            var rows = await TableJson.QueryAsync(
                _tables.DiaryEntries,
                TableClient.CreateQueryFilter($"PartitionKey eq {Partition(diaryId)}"));

            var matches = new List<TableEntity>();
            foreach (var row in rows)
            {
                var entry = await ReadPayloadAsync(row);
                if (entry != null && Matches(entry, search))
                {
                    matches.Add(row);
                }
            }

            var pageRows = matches.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new PagedResultDTO<DiaryEntryDTO>
            {
                Items = await HydrateAllAsync(pageRows, withImage: true),
                TotalCount = matches.Count,
                Page = page,
                PageSize = pageSize,
            };
        }

        /// <inheritdoc/>
        public async Task<DiaryDateRange> GetDiaryDateRangeAsync(Guid diaryId)
        {
            var dates = await DatesAsync(diaryId);

            return new DiaryDateRange
            {
                MaxDateTime = dates.Count == 0 ? DateTime.MaxValue : dates[^1],
                MinDateTime = dates.Count == 0 ? DateTime.MinValue : dates[0],
            };
        }

        /// <inheritdoc/>
        public async Task<DateTime> MinDiaryEntryDateAsync(Guid diaryId)
        {
            var dates = await DatesAsync(diaryId);
            return dates.Count == 0 ? DateTime.UtcNow : dates[0];
        }

        /// <inheritdoc/>
        public async Task<DateTime> MaxDiaryEntryDateAsync(Guid diaryId)
        {
            var dates = await DatesAsync(diaryId);
            return dates.Count == 0 ? DateTime.UtcNow : dates[^1];
        }

        private static string Partition(Guid diaryId) => diaryId.ToString("N");

        private static bool Matches(DiaryEntryDTO entry, string search)
        {
            return Contains(entry.Entry, search)
                || Contains(entry.Location, search)
                || Contains(entry.FromLocation, search)
                || Contains(entry.ToLocation, search);
        }

        private static bool Contains(string? value, string search) =>
            value != null && value.Contains(search, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Builds the row key range covering an inclusive date span.
        /// </summary>
        /// <remarks>
        /// The upper bound is exclusive of the next representable instant, which makes it
        /// inclusive of <paramref name="until"/> itself while still matching every entry
        /// id that shares that timestamp.
        /// </remarks>
        private static string RangeFilter(Guid diaryId, DateTime from, DateTime until)
        {
            var lower = StorageKeys.EntryRowKeyPrefix(from);
            var upperDate = until >= DateTime.MaxValue.AddTicks(-1) ? DateTime.MaxValue : until.AddTicks(1);
            var upper = StorageKeys.EntryRowKeyPrefix(upperDate);

            return TableClient.CreateQueryFilter(
                $"PartitionKey eq {Partition(diaryId)} and RowKey ge {lower} and RowKey lt {upper}");
        }

        private async Task<List<DateTime>> DatesAsync(Guid diaryId)
        {
            // Rows arrive in row key order, which is date order, so first and last are
            // the min and max. Table Storage cannot sort descending, so the max needs the
            // whole (key-only) partition rather than a single row.
            var rows = await TableJson.QueryAsync(
                _tables.DiaryEntries,
                TableClient.CreateQueryFilter($"PartitionKey eq {Partition(diaryId)}"),
                KeysAndDate);

            return rows
                .Select(r => r.GetDateTime(ColumnDate))
                .Where(d => d.HasValue)
                .Select(d => DateTime.SpecifyKind(d!.Value, DateTimeKind.Utc))
                .ToList();
        }

        private async Task<(string PartitionKey, string RowKey)?> FindLocatorAsync(Guid entryId)
        {
            var id = entryId.ToString("N");
            var rows = await TableJson.QueryAsync(
                _tables.DiaryEntries,
                TableClient.CreateQueryFilter($"DiaryEntryId eq {id}"),
                LocatorColumns);

            return rows.Count == 0 ? null : (rows[0].PartitionKey, rows[0].RowKey);
        }

        private async Task WriteAsync(DiaryEntryDTO entry)
        {
            var entity = await BuildEntityAsync(entry);
            await _tables.DiaryEntries.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        private async Task<TableEntity> BuildEntityAsync(DiaryEntryDTO entry)
        {
            var entryId = entry.DiaryEntryId!.Value;
            var hasImage = !string.IsNullOrEmpty(entry.ImageData);

            if (hasImage)
            {
                await _blobs.PutAsync(
                    _options.ImagesContainer,
                    StorageKeys.ImageBlobKey(entry.DiaryId, entryId),
                    BinaryData.FromBytes(Convert.FromBase64String(entry.ImageData!)),
                    entry.ImageContentType);
            }

            // The image is never part of the serialised row.
            var imageData = entry.ImageData;
            entry.ImageData = null;
            var json = TableJson.Serialize(entry);
            entry.ImageData = imageData;

            var spill = TableJson.ByteSize(json) > _options.JsonSpillThresholdBytes;
            if (spill)
            {
                await _blobs.PutAsync(
                    _options.ContentContainer,
                    StorageKeys.EntryJsonBlobKey(entryId),
                    BinaryData.FromString(json),
                    "application/json");
            }

            var entity = new TableEntity(Partition(entry.DiaryId), StorageKeys.EntryRowKey(entry.Date, entryId))
            {
                { TableJson.JsonColumn, spill ? string.Empty : json },
                { TableJson.SchemaVersionColumn, TableJson.CurrentSchemaVersion },
                { ColumnEntryId, entryId.ToString("N") },
                { "DiaryId", entry.DiaryId.ToString("N") },
                { ColumnHasImage, hasImage },
                { ColumnJsonInBlob, spill },
            };

            if (entry.Date.HasValue)
            {
                entity[ColumnDate] = DateTime.SpecifyKind(entry.Date.Value, DateTimeKind.Utc);
            }

            if (hasImage)
            {
                entity[ColumnImageContentType] = entry.ImageContentType;
            }

            return entity;
        }

        private async Task<DiaryEntryDTO?> ReadPayloadAsync(TableEntity row)
        {
            if (row.GetBoolean(ColumnJsonInBlob) == true)
            {
                var entryId = row.GetString(ColumnEntryId);
                if (Guid.TryParse(entryId, out var parsed))
                {
                    var json = await _blobs.TryGetStringAsync(
                        _options.ContentContainer,
                        StorageKeys.EntryJsonBlobKey(parsed));
                    return TableJson.Deserialize<DiaryEntryDTO>(json);
                }
            }

            return TableJson.FromEntity<DiaryEntryDTO>(row);
        }

        private async Task<DiaryEntryDTO?> HydrateAsync(TableEntity row, bool withImage)
        {
            var entry = await ReadPayloadAsync(row);
            if (entry == null)
            {
                return null;
            }

            if (withImage && row.GetBoolean(ColumnHasImage) == true)
            {
                var stored = await _blobs.TryGetAsync(
                    _options.ImagesContainer,
                    StorageKeys.ImageBlobKey(entry.DiaryId, entry.DiaryEntryId ?? Guid.Empty));

                if (stored != null)
                {
                    entry.ImageData = Convert.ToBase64String(stored.Content.ToArray());
                    entry.ImageContentType = stored.ContentType ?? row.GetString(ColumnImageContentType);
                }
            }

            return entry;
        }

        private async Task<List<DiaryEntryDTO>> HydrateAllAsync(IEnumerable<TableEntity> rows, bool withImage)
        {
            var entries = new List<DiaryEntryDTO>();
            foreach (var row in rows)
            {
                var entry = await HydrateAsync(row, withImage);
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }
    }
}
