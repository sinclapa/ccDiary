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
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(
    Justification = "Reads a live SQL Server; there is no database left in the test environment to read from.")]
internal sealed class SqlReader(string connectionString)
{
    public async Task<List<DiaryDto>> ReadDiariesAsync()
    {
        const string sql = "SELECT DiaryId, Title, Author, Description, OwnerId FROM Diary";
        var diaries = new List<DiaryDto>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            diaries.Add(new DiaryDto
            {
                DiaryId = reader.GetGuid(0),
                Title = reader.GetString(1),
                Author = reader.GetString(2),
                Description = StringOrNull(reader, 3),
                OwnerId = StringOrNull(reader, 4),
            });
        }

        return diaries;
    }

    public async Task<List<DiaryEntryDto>> ReadEntriesAsync(Guid diaryId)
    {
        const string sql = """
            SELECT DiaryEntryId, Date, Location, Entry, MapLocation, ShowMap,
                   FromLocation, ToLocation, ShowJourney, JourneyMode,
                   ImageData, ImageContentType, DiaryId
            FROM DiaryEntry
            WHERE DiaryId = @diaryId
            ORDER BY Date
            """;

        var entries = new List<DiaryEntryDto>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@diaryId", diaryId);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            entries.Add(new DiaryEntryDto
            {
                DiaryEntryId = reader.GetGuid(0),

                // Stored as datetime2 with no offset; the application has always treated
                // these as UTC, so the kind is asserted rather than converted.
                Date = reader.IsDBNull(1) ? null : DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc),
                Location = StringOrNull(reader, 2),
                Entry = StringOrNull(reader, 3),
                MapLocation = StringOrNull(reader, 4),
                ShowMap = FlagOrFalse(reader, 5),
                FromLocation = StringOrNull(reader, 6),
                ToLocation = StringOrNull(reader, 7),
                ShowJourney = FlagOrFalse(reader, 8),

                // Enums were persisted as ints by EF; they are stored as kebab-case
                // strings now, and the DTO carries the conversion.
                JourneyMode = reader.IsDBNull(9) ? JourneyMode.CrowFlies : (JourneyMode)reader.GetInt32(9),
                ImageData = StringOrNull(reader, 10),
                ImageContentType = StringOrNull(reader, 11),
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
                InviteRedeemUrl = StringOrNull(reader, 7),
            });
        }

        return requests;
    }

    /// <summary>Reads a nullable text column.</summary>
    private static string? StringOrNull(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    /// <summary>Reads a nullable bit column, treating NULL as false.</summary>
    private static bool FlagOrFalse(SqlDataReader reader, int ordinal) =>
        !reader.IsDBNull(ordinal) && reader.GetBoolean(ordinal);
}
