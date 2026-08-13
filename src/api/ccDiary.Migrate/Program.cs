namespace ccDiary.Migrate;

using System.Text.Json;
using ccDiaryApi.Data.Model;
using ccDiaryApi.Data.Storage;

/// <summary>
/// Moves ccDiary's data from Azure SQL into Azure Table + Blob storage.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately not built on the DiaryArchive export/import endpoints. Those cover only
/// diaries and entries, so users would lose their roles and the access request history
/// would be dropped; they also push the whole dataset through one request against a
/// 0.5 GiB container.
/// </para>
/// <para>
/// Every write is an upsert keyed by the source identifiers, so an interrupted run is
/// repaired by running it again rather than by cleaning up first.
/// </para>
/// </remarks>
internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var options = CommandLineOptions.Parse(args);
        if (options == null)
        {
            CommandLineOptions.PrintUsage();
            return 1;
        }

        try
        {
            return await RunAsync(options);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine($"FAILED: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> RunAsync(CommandLineOptions options)
    {
        var storage = new StorageWriter(options.ToStorageOptions());

        if (!options.DryRun)
        {
            await storage.EnsureCreatedAsync();
        }

        var (diaries, entriesByDiary, users, requests) = options.ArchiveFile != null
            ? LoadFromArchive(options.ArchiveFile)
            : await LoadFromSqlAsync(options.SourceConnectionString!);

        Console.WriteLine();
        Console.WriteLine($"Source: {diaries.Count} diaries, "
            + $"{entriesByDiary.Values.Sum(e => e.Count)} entries, "
            + $"{users.Count} users, {requests.Count} access requests");

        if (options.DryRun)
        {
            Console.WriteLine();
            Console.WriteLine("--dry-run: nothing was written.");
            ReportImageStats(entriesByDiary);
            return 0;
        }

        await WriteAllAsync(storage, diaries, entriesByDiary, users, requests);

        if (!options.Verify)
        {
            Console.WriteLine();
            Console.WriteLine("Done. Re-run with --verify to check the result before cutting over.");
            return 0;
        }

        return await VerifyAllAsync(storage, diaries, entriesByDiary, users, requests);
    }

    private static async Task WriteAllAsync(
        StorageWriter storage,
        List<DiaryDTO> diaries,
        Dictionary<Guid, List<DiaryEntryDTO>> entriesByDiary,
        List<AppUserDto> users,
        List<AccessRequestDto> requests)
    {
        Console.WriteLine();
        Console.WriteLine("Writing...");

        foreach (var diary in diaries)
        {
            var diaryId = diary.DiaryId!.Value;
            var entries = entriesByDiary[diaryId];
            Console.WriteLine($"  diary {diaryId} \"{diary.Title}\" ({entries.Count} entries)");

            await storage.WriteDiaryAsync(diary);

            var written = 0;
            foreach (var entry in entries)
            {
                await storage.WriteEntryAsync(entry);
                written++;
                if (written % 25 == 0)
                {
                    Console.WriteLine($"    {written}/{entries.Count}");
                }
            }
        }

        foreach (var user in users)
        {
            await storage.WriteUserAsync(user);
        }

        Console.WriteLine($"  {users.Count} users");

        foreach (var request in requests)
        {
            await storage.WriteAccessRequestAsync(request);
        }

        Console.WriteLine($"  {requests.Count} access requests");
    }

    private static async Task<int> VerifyAllAsync(
        StorageWriter storage,
        List<DiaryDTO> diaries,
        Dictionary<Guid, List<DiaryEntryDTO>> entriesByDiary,
        List<AppUserDto> users,
        List<AccessRequestDto> requests)
    {
        Console.WriteLine();
        Console.WriteLine("Verifying (reading everything back out of storage)...");

        var verifier = new Verifier(storage);
        foreach (var diary in diaries)
        {
            await verifier.VerifyDiaryAsync(diary, entriesByDiary[diary.DiaryId!.Value]);
        }

        await verifier.VerifyUsersAndRequestsAsync(users, requests);

        Console.WriteLine();
        if (verifier.Problems.Count == 0)
        {
            Console.WriteLine("VERIFIED: storage matches the source, images compared by SHA-256.");
            return 0;
        }

        Console.Error.WriteLine($"VERIFICATION FAILED with {verifier.Problems.Count} problem(s):");
        foreach (var problem in verifier.Problems.Take(50))
        {
            Console.Error.WriteLine($"  - {problem}");
        }

        if (verifier.Problems.Count > 50)
        {
            Console.Error.WriteLine($"  ... and {verifier.Problems.Count - 50} more");
        }

        return 1;
    }

    private static async Task<(List<DiaryDTO>, Dictionary<Guid, List<DiaryEntryDTO>>, List<AppUserDto>, List<AccessRequestDto>)>
        LoadFromSqlAsync(string connectionString)
    {
        Console.WriteLine("Reading from SQL...");
        var reader = new SqlReader(connectionString);

        var diaries = await reader.ReadDiariesAsync();
        var entriesByDiary = new Dictionary<Guid, List<DiaryEntryDTO>>();
        foreach (var diary in diaries)
        {
            entriesByDiary[diary.DiaryId!.Value] = await reader.ReadEntriesAsync(diary.DiaryId!.Value);
        }

        return (diaries, entriesByDiary, await reader.ReadUsersAsync(), await reader.ReadAccessRequestsAsync());
    }

    /// <summary>
    /// Rebuilds the dataset from a DiaryArchive JSON file, with no database involved.
    /// </summary>
    /// <remarks>
    /// This is the disaster recovery path and the replacement for data/data.sql: the
    /// repository already carries the real diary as an archive, so a storage account can
    /// be repopulated from a clean checkout alone.
    /// </remarks>
    private static (List<DiaryDTO>, Dictionary<Guid, List<DiaryEntryDTO>>, List<AppUserDto>, List<AccessRequestDto>)
        LoadFromArchive(string path)
    {
        Console.WriteLine($"Reading archive {path}...");

        var json = File.ReadAllText(path);
        var archive = JsonSerializer.Deserialize<DiaryArchiveDTO>(json, ArchiveJsonOptions)
            ?? throw new InvalidOperationException($"{path} did not deserialise into a diary archive.");

        archive.Diary.DiaryId ??= Guid.NewGuid();
        var diaryId = archive.Diary.DiaryId.Value;

        foreach (var entry in archive.DiaryEntries)
        {
            entry.DiaryEntryId ??= Guid.NewGuid();
            if (entry.DiaryId == Guid.Empty)
            {
                entry.DiaryId = diaryId;
            }
        }

        return (
            [archive.Diary],
            new Dictionary<Guid, List<DiaryEntryDTO>> { [diaryId] = archive.DiaryEntries },
            [],
            []);
    }

    private static void ReportImageStats(Dictionary<Guid, List<DiaryEntryDTO>> entriesByDiary)
    {
        var withImages = entriesByDiary.Values
            .SelectMany(e => e)
            .Where(e => !string.IsNullOrEmpty(e.ImageData))
            .ToList();

        if (withImages.Count == 0)
        {
            Console.WriteLine("No images to move.");
            return;
        }

        var totalBytes = withImages.Sum(e => (long)e.ImageData!.Length);
        var largest = withImages.Max(e => e.ImageData!.Length);
        Console.WriteLine(
            $"{withImages.Count} images totalling {totalBytes / 1024 / 1024} MB base64, largest {largest / 1024} KB.");
        Console.WriteLine("These move to blobs; a table row could not hold them.");
    }

    private static JsonSerializerOptions ArchiveJsonOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower) },
    };
}
