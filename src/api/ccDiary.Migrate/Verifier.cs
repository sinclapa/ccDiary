namespace ccDiary.Migrate;

using System.Security.Cryptography;
using System.Text;
using ccDiaryApi.Data.Model;

/// <summary>
/// Compares what was read from SQL against what can be read back out of storage.
/// </summary>
/// <remarks>
/// This is the acceptance gate. A migration that reports success while having quietly
/// dropped or corrupted an entry is indistinguishable from one that worked, until
/// somebody opens the diary months later. Images are compared by hash rather than by
/// length, because a truncated base64 string still has a plausible length.
/// </remarks>
internal sealed class Verifier(StorageWriter storage)
{
    private readonly List<string> _problems = [];

    public IReadOnlyList<string> Problems => _problems;

    public async Task<bool> VerifyDiaryAsync(DiaryDTO expectedDiary, List<DiaryEntryDTO> expectedEntries)
    {
        var diaryId = expectedDiary.DiaryId!.Value;
        var actualDiary = await storage.Diaries.GetDiaryAsync(diaryId);

        if (actualDiary == null)
        {
            _problems.Add($"diary {diaryId} is missing from storage");
            return false;
        }

        CompareField(diaryId, "title", expectedDiary.Title, actualDiary.Title);
        CompareField(diaryId, "author", expectedDiary.Author, actualDiary.Author);
        CompareField(diaryId, "description", expectedDiary.Description, actualDiary.Description);
        CompareField(diaryId, "ownerId", expectedDiary.OwnerId, actualDiary.OwnerId);

        var actualEntries = await storage.Entries.GetDiaryEntriesAsync(diaryId);
        var actualById = actualEntries
            .Where(e => e.DiaryEntryId.HasValue)
            .ToDictionary(e => e.DiaryEntryId!.Value);

        if (actualEntries.Count != expectedEntries.Count)
        {
            _problems.Add(
                $"diary {diaryId}: expected {expectedEntries.Count} entries, storage returned {actualEntries.Count}");
        }

        // Checking only that every source row arrived leaves the other direction blind:
        // a row in storage with no counterpart in the source is either a leftover from a
        // previous migration or something else writing to the same tables, and either way
        // the operator needs to know before treating this as a faithful copy.
        var expectedIds = expectedEntries
            .Where(e => e.DiaryEntryId.HasValue)
            .Select(e => e.DiaryEntryId!.Value)
            .ToHashSet();

        foreach (var extra in actualEntries.Where(e => e.DiaryEntryId.HasValue && !expectedIds.Contains(e.DiaryEntryId!.Value)))
        {
            _problems.Add($"entry {extra.DiaryEntryId} is in storage but not in the source");
        }

        foreach (var expected in expectedEntries)
        {
            var id = expected.DiaryEntryId!.Value;
            if (!actualById.TryGetValue(id, out var actual))
            {
                _problems.Add($"entry {id} is missing from storage");
                continue;
            }

            CompareField(id, "location", expected.Location, actual.Location);
            CompareField(id, "entry", expected.Entry, actual.Entry);
            CompareField(id, "mapLocation", expected.MapLocation, actual.MapLocation);
            CompareField(id, "fromLocation", expected.FromLocation, actual.FromLocation);
            CompareField(id, "toLocation", expected.ToLocation, actual.ToLocation);
            CompareField(id, "imageContentType", expected.ImageContentType, actual.ImageContentType);

            if (expected.ShowMap != actual.ShowMap)
            {
                _problems.Add($"entry {id}: showMap {expected.ShowMap} became {actual.ShowMap}");
            }

            if (expected.ShowJourney != actual.ShowJourney)
            {
                _problems.Add($"entry {id}: showJourney {expected.ShowJourney} became {actual.ShowJourney}");
            }

            if (expected.JourneyMode != actual.JourneyMode)
            {
                _problems.Add($"entry {id}: journeyMode {expected.JourneyMode} became {actual.JourneyMode}");
            }

            if (expected.Date.HasValue && actual.Date.HasValue
                && Math.Abs((expected.Date.Value - actual.Date.Value).TotalSeconds) > 1)
            {
                _problems.Add($"entry {id}: date {expected.Date:O} became {actual.Date:O}");
            }

            CompareImage(id, expected.ImageData, actual.ImageData);
        }

        return _problems.Count == 0;
    }

    public async Task VerifyUsersAndRequestsAsync(
        List<AppUserDto> expectedUsers,
        List<AccessRequestDto> expectedRequests)
    {
        var actualUsers = await storage.ReadUsersAsync();
        var byOid = actualUsers.ToDictionary(u => u.EntraObjectId, StringComparer.Ordinal);

        foreach (var expected in expectedUsers)
        {
            if (!byOid.TryGetValue(expected.EntraObjectId, out var actual))
            {
                _problems.Add($"user {expected.EntraObjectId} is missing from storage");
                continue;
            }

            // The role is the security-relevant field: losing it silently downgrades
            // an administrator to a reader.
            if (expected.Role != actual.Role)
            {
                _problems.Add($"user {expected.EntraObjectId}: role {expected.Role} became {actual.Role}");
            }

            if (expected.UserId != actual.UserId)
            {
                _problems.Add($"user {expected.EntraObjectId}: userId changed, breaking ProcessedByUserId references");
            }
        }

        // A user in storage that the source does not have is the more alarming direction:
        // it means somebody holds an account, and a role, that the migration did not put
        // there.
        var expectedOids = expectedUsers.Select(u => u.EntraObjectId).ToHashSet(StringComparer.Ordinal);
        foreach (var extra in actualUsers.Where(u => !expectedOids.Contains(u.EntraObjectId)))
        {
            _problems.Add($"user {extra.EntraObjectId} ({extra.Role}) is in storage but not in the source");
        }

        var actualRequests = await storage.ReadAccessRequestsAsync();
        var byId = actualRequests.ToDictionary(r => r.AccessRequestId);

        foreach (var expected in expectedRequests)
        {
            if (!byId.TryGetValue(expected.AccessRequestId, out var actual))
            {
                _problems.Add($"access request {expected.AccessRequestId} is missing from storage");
                continue;
            }

            if (expected.Status != actual.Status)
            {
                _problems.Add($"access request {expected.AccessRequestId}: status {expected.Status} became {actual.Status}");
            }

            CompareField(expected.AccessRequestId, "inviteRedeemUrl", expected.InviteRedeemUrl, actual.InviteRedeemUrl);
        }

        var expectedRequestIds = expectedRequests.Select(r => r.AccessRequestId).ToHashSet();
        foreach (var extra in actualRequests.Where(r => !expectedRequestIds.Contains(r.AccessRequestId)))
        {
            _problems.Add($"access request {extra.AccessRequestId} ({extra.Email}) is in storage but not in the source");
        }
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private void CompareField(object id, string field, string? expected, string? actual)
    {
        // Null and empty are not distinguished: the relational columns were nullable and
        // the serializer omits nulls, so round-tripping "" as null is expected and benign.
        if (!string.Equals(expected ?? string.Empty, actual ?? string.Empty, StringComparison.Ordinal))
        {
            _problems.Add($"{id}: {field} differs (expected '{Truncate(expected)}', got '{Truncate(actual)}')");
        }
    }

    private void CompareImage(Guid id, string? expected, string? actual)
    {
        if (string.IsNullOrEmpty(expected))
        {
            if (!string.IsNullOrEmpty(actual))
            {
                _problems.Add($"entry {id}: storage has an image where the database had none");
            }

            return;
        }

        if (string.IsNullOrEmpty(actual))
        {
            _problems.Add($"entry {id}: image is missing from storage");
            return;
        }

        var expectedHash = Hash(expected);
        var actualHash = Hash(actual);
        if (!string.Equals(expectedHash, actualHash, StringComparison.Ordinal))
        {
            _problems.Add(
                $"entry {id}: image content differs (sha256 {expectedHash[..12]} vs {actualHash[..12]}, "
                + $"{expected.Length} vs {actual.Length} base64 chars)");
        }
    }

    private static string Truncate(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= 60 ? value : value[..60] + "...";
    }
}
