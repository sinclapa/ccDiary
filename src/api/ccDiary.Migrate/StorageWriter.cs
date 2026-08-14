namespace ccDiary.Migrate;

using ccDiaryApi.Data.Model;
using ccDiaryApi.Data.Storage;
using ccDiaryApi.Services;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;

/// <summary>
/// Writes into Table + Blob using the application's own services.
/// </summary>
/// <remarks>
/// Every write goes through the same services the API uses, so key derivation, image
/// blob layout and JSON spill behave identically here. A separate implementation would
/// be free to drift, and a migration that writes data the application cannot then read
/// is the single worst outcome available.
/// </remarks>
internal sealed class StorageWriter
{
    private readonly TableStore _tables;
    private readonly BlobStore _blobs;
    private readonly DiaryService _diaries;
    private readonly DiaryEntryService _entries;

    public StorageWriter(StorageOptions options)
    {
        var wrapped = Options.Create(options);
        _tables = new TableStore(wrapped);
        _blobs = new BlobStore(wrapped);
        _diaries = new DiaryService(_tables, _blobs, wrapped);
        _entries = new DiaryEntryService(_tables, _blobs, wrapped);
    }

    public ITableStore Tables => _tables;

    public DiaryService Diaries => _diaries;

    public DiaryEntryService Entries => _entries;

    /// <summary>Creates the tables and containers if they are not already present.</summary>
    public async Task EnsureCreatedAsync()
    {
        foreach (var table in _tables.All)
        {
            await table.CreateIfNotExistsAsync();
        }

        foreach (var container in new[] { "images", "mapcache", "content" })
        {
            await _blobs.Container(container).CreateIfNotExistsAsync();
        }
    }

    public async Task WriteDiaryAsync(DiaryDTO diary) => await _diaries.UpdateAsync(diary);

    /// <summary>Writes an entry, including decoding and storing its image.</summary>
    /// <remarks>
    /// Entries with no usable date are written rather than rejected. The API refuses to
    /// create one, but a legacy row can still hold null, and dropping data silently
    /// during a migration is worse than carrying an oddity forward.
    /// </remarks>
    public async Task WriteEntryAsync(DiaryEntryDTO entry)
    {
        if (entry.Date == null || entry.Date == DateTime.MinValue)
        {
            Console.WriteLine($"    ! entry {entry.DiaryEntryId} has no date; writing it at the epoch sort position");
            entry.Date = DateTime.SpecifyKind(DateTime.MinValue.AddSeconds(1), DateTimeKind.Utc);
        }

        await _entries.UpdateDiaryEntryAsync(entry);
    }

    public async Task WriteUserAsync(AppUserDto user)
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

    public async Task WriteAccessRequestAsync(AccessRequestDto request)
    {
        var entity = TableJson.ToEntity(
            StorageKeys.RequestPartition,
            request.AccessRequestId.ToString("N"),
            request,
            e =>
            {
                e["Status"] = request.Status.ToStoredValue();
                e["Email"] = request.Email;
                e["RequestedAt"] = request.RequestedAt;
            });

        await _tables.AccessRequests.UpsertEntityAsync(entity, TableUpdateMode.Replace);
    }

    public async Task<List<AppUserDto>> ReadUsersAsync()
    {
        var rows = await TableJson.QueryAsync(_tables.AppUsers);
        return rows.Select(TableJson.FromEntity<AppUserDto>).Where(u => u != null).Select(u => u!).ToList();
    }

    public async Task<List<AccessRequestDto>> ReadAccessRequestsAsync()
    {
        var rows = await TableJson.QueryAsync(_tables.AccessRequests);
        return rows.Select(TableJson.FromEntity<AccessRequestDto>).Where(r => r != null).Select(r => r!).ToList();
    }
}
