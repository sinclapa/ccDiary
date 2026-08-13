// <copyright file="StorageTestFixture.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Storage
{
    using System.Net.Sockets;
    using ccDiaryApi.Data.Storage;
    using ccDiaryApi.Infrastructure;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Gives a test class its own isolated set of tables and containers on Azurite.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tests run against a real emulator rather than a hand-written fake. A fake would
    /// have to reimplement row key ordering, filter evaluation, transaction limits and
    /// the SDK's 404/409 semantics — at which point the tests would be checking the fake
    /// rather than the code that ships.
    /// </para>
    /// <para>
    /// Every fixture gets a unique name prefix, so classes running concurrently and
    /// repeated local runs cannot collide. Note that Azurite allows a table to be
    /// deleted and immediately recreated, whereas real Azure blocks the name for around
    /// 40 seconds; no test may depend on that difference.
    /// </para>
    /// </remarks>
    public sealed class StorageTestFixture : IDisposable
    {
        /// <summary>The well-known Azurite development connection string.</summary>
        public const string AzuriteConnectionString =
            "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;" +
            "AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;" +
            "BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;" +
            "QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;" +
            "TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;";

        private StorageTestFixture(StorageOptions options)
        {
            Options = options;
            Tables = new TableStore(Microsoft.Extensions.Options.Options.Create(options));
            Blobs = new BlobStore(Microsoft.Extensions.Options.Options.Create(options));
        }

        /// <summary>Gets the options these stores were built from.</summary>
        public StorageOptions Options { get; }

        /// <summary>Gets the table store under test.</summary>
        public ITableStore Tables { get; }

        /// <summary>Gets the blob store under test.</summary>
        public IBlobStore Blobs { get; }

        /// <summary>
        /// Creates a fixture with a unique prefix and its tables and containers already made.
        /// </summary>
        /// <returns>The ready fixture.</returns>
        public static async Task<StorageTestFixture> CreateAsync()
        {
            RequireAzurite();

            // Table names must start with a letter and be alphanumeric; container names
            // must be lowercase alphanumeric or hyphen. A lowercase "t" plus hex satisfies both.
            var prefix = "t" + Guid.NewGuid().ToString("N")[..8];
            var fixture = new StorageTestFixture(new StorageOptions
            {
                ConnectionString = AzuriteConnectionString,
                TableNamePrefix = prefix,
                ContainerPrefix = prefix + "-",
            });

            await fixture.BootstrapAsync();
            return fixture;
        }

        /// <summary>Runs the bootstrapper against this fixture's storage.</summary>
        /// <returns>A task representing the operation.</returns>
        public async Task BootstrapAsync()
        {
            var bootstrapper = new StorageBootstrapper(
                Tables,
                Blobs,
                Microsoft.Extensions.Options.Options.Create(Options),
                NullLogger<StorageBootstrapper>.Instance);
            await bootstrapper.StartAsync(CancellationToken.None);
        }

        /// <summary>Removes every table and container this fixture created.</summary>
        public void Dispose()
        {
            foreach (var table in Tables.All)
            {
                try
                {
                    table.Delete();
                }
                catch (Exception)
                {
                    // Teardown is best effort; a leaked emulator table must not fail a green run.
                }
            }

            foreach (var container in new[] { Options.ImagesContainer, Options.MapCacheContainer, Options.ContentContainer })
            {
                try
                {
                    Blobs.Container(container).DeleteIfExists();
                }
                catch (Exception)
                {
                    // As above.
                }
            }
        }

        /// <summary>
        /// Fails with an actionable message when the emulator is not running, rather
        /// than letting every storage test die on a raw socket exception.
        /// </summary>
        private static void RequireAzurite()
        {
            try
            {
                using var client = new TcpClient();
                if (!client.ConnectAsync("127.0.0.1", 10002).Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new SocketException();
                }
            }
            catch (Exception ex)
            {
                // Deliberately a failure, not Assert.Inconclusive: a skip would let CI
                // go green when the Azurite service container never came up.
                Assert.Fail(
                    "Azurite is not reachable on 127.0.0.1:10002. Start it with " +
                    "'docker compose -f src/api/docker-compose.yml up -d azurite'. " +
                    $"({ex.GetType().Name})");
            }
        }
    }
}
