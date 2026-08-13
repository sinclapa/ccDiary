namespace ccDiary.Migrate;

using ccDiaryApi.Data.Storage;

internal sealed class CommandLineOptions
{
    public string? SourceConnectionString { get; private init; }

    public string? DestinationAccountName { get; private init; }

    public string? DestinationConnectionString { get; private init; }

    public string? ArchiveFile { get; private init; }

    public bool DryRun { get; private init; }

    public bool Verify { get; private init; }

    public StorageOptions ToStorageOptions() => new()
    {
        AccountName = DestinationAccountName,
        ConnectionString = DestinationConnectionString,
    };

    public static CommandLineOptions? Parse(string[] args)
    {
        string? source = null;
        string? dest = null;
        string? archive = null;
        var dryRun = false;
        var verify = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--source" when i + 1 < args.Length:
                    source = args[++i];
                    break;
                case "--dest" when i + 1 < args.Length:
                    dest = args[++i];
                    break;
                case "--from-archive" when i + 1 < args.Length:
                    archive = args[++i];
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--verify":
                    verify = true;
                    break;
                default:
                    Console.Error.WriteLine($"Unrecognised argument: {args[i]}");
                    return null;
            }
        }

        if (dest == null)
        {
            Console.Error.WriteLine("--dest is required.");
            return null;
        }

        if (source == null && archive == null)
        {
            Console.Error.WriteLine("One of --source or --from-archive is required.");
            return null;
        }

        if (archive != null && !File.Exists(archive))
        {
            Console.Error.WriteLine($"Archive file not found: {archive}");
            return null;
        }

        // A value containing '=' is a connection string; anything else is an account
        // name, which means managed identity or the signed-in developer's credential.
        var destIsConnectionString = dest.Contains('=', StringComparison.Ordinal);

        return new CommandLineOptions
        {
            SourceConnectionString = source,
            DestinationAccountName = destIsConnectionString ? null : dest,
            DestinationConnectionString = destIsConnectionString ? dest : null,
            ArchiveFile = archive,
            DryRun = dryRun,
            Verify = verify,
        };
    }

    public static void PrintUsage()
    {
        Console.Error.WriteLine("""

            ccDiary.Migrate — moves diary data from Azure SQL into Table + Blob storage.

              --source <connection-string>   Read from this SQL database.
              --from-archive <file>          Read from a DiaryArchive JSON file instead
                                             (disaster recovery; no database needed).
              --dest <account-name|conn>     Storage destination. An account name uses
                                             your Azure credential; a connection string
                                             is for Azurite.
              --dry-run                      Report what would move; write nothing.
              --verify                       After writing, read everything back and diff
                                             it against the source. Images are compared
                                             by SHA-256. This is the acceptance gate.

            Examples:

              # See what is there, touching nothing
              ccDiary.Migrate --source "<sql>" --dest stccdiarydevcog5wcxyf3cz --dry-run

              # Migrate and prove it
              ccDiary.Migrate --source "<sql>" --dest stccdiarydevcog5wcxyf3cz --verify

              # Rebuild storage from the in-repo archive
              ccDiary.Migrate --from-archive data/ww1-diary.json --dest <account> --verify

            Every write is an upsert keyed by the source identifiers, so an interrupted
            run is repaired by running it again.

            """);
    }
}
