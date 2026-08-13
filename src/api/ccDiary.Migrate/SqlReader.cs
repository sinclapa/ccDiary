namespace ccDiary.Migrate;

using ccDiaryApi.Data.Model;
using Microsoft.Data.SqlClient;

/// <summary>
/// Reads the legacy relational data with hand-written SELECTs.
/// </summary>
/// <remarks>
/// Deliberately not EF. The API's DbContext, entity configuration and migrations are
/// deleted by the same change this tool supports, so binding to them would make the tool
/// stop compiling exactly when it is still needed — including for a rollback.
/// </remarks>
internal sealed class SqlReader(string connectionString)
{
    public async Task<List<DiaryDTO>> ReadDiariesAsync()
    {
        const string sql = "SELECT DiaryId, Title, Author, Description, OwnerId FROM Diary";
        var diaries = new List<DiaryDTO>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            diaries.Add(new DiaryDTO
            {
                DiaryId = reader.GetGuid(0),
                Title = reader.GetString(1),
                Author = reader.GetString(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                OwnerId = reader.IsDBNull(4) ? null : reader.GetString(4),
            });
        }

        return diaries;
    }

    public async Task<List<DiaryEntryDTO>> ReadEntriesAsync(Guid diaryId)
    {
        const string sql = """
            SELECT DiaryEntryId, Date, Location, Entry, MapLocation, ShowMap,
                   FromLocation, ToLocation, ShowJourney, JourneyMode,
                   ImageData, ImageContentType, DiaryId
            FROM DiaryEntry
            WHERE DiaryId = @diaryId
            ORDER BY Date
            """;

        var entries = new List<DiaryEntryDTO>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@diaryId", diaryId);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            entries.Add(new DiaryEntryDTO
            {
                DiaryEntryId = reader.GetGuid(0),

                // Stored as datetime2 with no offset; the application has always treated
                // these as UTC, so the kind is asserted rather than converted.
                Date = reader.IsDBNull(1) ? null : DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc),
                Location = reader.IsDBNull(2) ? null : reader.GetString(2),
                Entry = reader.IsDBNull(3) ? null : reader.GetString(3),
                MapLocation = reader.IsDBNull(4) ? null : reader.GetString(4),
                ShowMap = !reader.IsDBNull(5) && reader.GetBoolean(5),
                FromLocation = reader.IsDBNull(6) ? null : reader.GetString(6),
                ToLocation = reader.IsDBNull(7) ? null : reader.GetString(7),
                ShowJourney = !reader.IsDBNull(8) && reader.GetBoolean(8),

                // Enums were persisted as ints by EF; they are stored as kebab-case
                // strings now, and the DTO carries the conversion.
                JourneyMode = reader.IsDBNull(9) ? JourneyMode.CrowFlies : (JourneyMode)reader.GetInt32(9),
                ImageData = reader.IsDBNull(10) ? null : reader.GetString(10),
                ImageContentType = reader.IsDBNull(11) ? null : reader.GetString(11),
                DiaryId = reader.GetGuid(12),
            });
        }

        return entries;
    }

    public async Task<List<AppUserDto>> ReadUsersAsync()
    {
        const string sql = "SELECT UserId, EntraObjectId, DisplayName, Email, Role, CreatedAt FROM AppUser";
        var users = new List<AppUserDto>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            users.Add(new AppUserDto
            {
                UserId = reader.GetGuid(0),
                EntraObjectId = reader.GetString(1),
                DisplayName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Email = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Role = (AppRole)reader.GetInt32(4),
                CreatedAt = DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc),
            });
        }

        return users;
    }

    public async Task<List<AccessRequestDto>> ReadAccessRequestsAsync()
    {
        const string sql = """
            SELECT AccessRequestId, DisplayName, Email, Status, RequestedAt,
                   ProcessedAt, ProcessedByUserId, InviteRedeemUrl
            FROM AccessRequest
            """;

        var requests = new List<AccessRequestDto>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            requests.Add(new AccessRequestDto
            {
                AccessRequestId = reader.GetGuid(0),
                DisplayName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Email = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Status = (RequestStatus)reader.GetInt32(3),
                RequestedAt = DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc),
                ProcessedAt = reader.IsDBNull(5) ? null : DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc),
                ProcessedByUserId = reader.IsDBNull(6) ? null : reader.GetGuid(6),
                InviteRedeemUrl = reader.IsDBNull(7) ? null : reader.GetString(7),
            });
        }

        return requests;
    }
}
