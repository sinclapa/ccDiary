// <copyright file="StorageTestFixture.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Storage
{
    using System.Net.Sockets;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Data.Storage;
    using ccDiaryApi.Infrastructure;
    using ccDiaryApi.Services;
    using global::Azure.Data.Tables;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
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

        /// <summary>Gets these options wrapped for constructor injection.</summary>
        /// <returns>The wrapped options.</returns>
        public IOptions<StorageOptions> AsOptions() => Microsoft.Extensions.Options.Options.Create(Options);

        /// <summary>Writes an application user directly, bypassing the service.</summary>
        /// <param name="oid">The Entra object id, which is also the row key.</param>
        /// <param name="role">The role to grant.</param>
        /// <param name="email">The email address, defaulted from the oid.</param>
        /// <returns>The stored user.</returns>
        public async Task<AppUserDto> SeedUserAsync(string oid, AppRole role, string? email = null)
        {
            var user = new AppUserDto
            {
                UserId = Guid.NewGuid(),
                EntraObjectId = oid,
                DisplayName = $"Test {role}",
                Email = email ?? $"{oid}@test.com",
                Role = role,
                CreatedAt = DateTime.UtcNow,
            };

            var entity = TableJson.ToEntity(
                StorageKeys.UserPartition,
                StorageKeys.SanitiseKey(oid),
                user,
                e =>
                {
                    e["UserId"] = user.UserId.ToString();
                    e["Email"] = user.Email;
                    e["Role"] = user.Role.ToStoredValue();
                });

            await Tables.AppUsers.UpsertEntityAsync(entity, TableUpdateMode.Replace);
            return user;
        }

        /// <summary>Writes an access request directly, bypassing the service.</summary>
        /// <param name="email">The requester's email.</param>
        /// <param name="status">The request status.</param>
        /// <param name="displayName">The requester's display name.</param>
        /// <returns>The stored request.</returns>
        public async Task<AccessRequestDto> SeedAccessRequestAsync(
            string email,
            RequestStatus status,
            string displayName = "Test User")
        {
            var request = new AccessRequestDto
            {
                AccessRequestId = Guid.NewGuid(),
                DisplayName = displayName,
                Email = email,
                Status = status,
                RequestedAt = DateTime.UtcNow,
            };

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

            await Tables.AccessRequests.UpsertEntityAsync(entity, TableUpdateMode.Replace);
            return request;
        }

        /// <summary>Seeds a cached map tile at its blob location.</summary>
        /// <param name="tile">The tile to store.</param>
        /// <returns>A task representing the operation.</returns>
        public async Task SeedAsync(MapTileCacheDto tile)
        {
            await Blobs.PutAsync(
                Options.MapCacheContainer,
                StorageKeys.TileBlobKey(tile.Source, tile.Z, tile.X, tile.Y),
                BinaryData.FromBytes(tile.TileData),
                tile.ContentType);
        }

        /// <summary>Seeds a cached geocoding result.</summary>
        /// <param name="geocode">The result to store.</param>
        /// <returns>A task representing the operation.</returns>
        public async Task SeedAsync(GeocodingCacheDto geocode)
        {
            var entity = new TableEntity(StorageKeys.GeocodePartition, StorageKeys.GeocodeKey(geocode.Query))
            {
                { "Query", geocode.Query },
                { "Lat", geocode.Lat },
                { "Lon", geocode.Lon },
                { "CachedAt", geocode.CachedAt },
            };

            await Tables.GeocodingCache.UpsertEntityAsync(entity, TableUpdateMode.Replace);
        }

        /// <summary>Seeds a cached route at its blob location.</summary>
        /// <param name="route">The route to store.</param>
        /// <returns>A task representing the operation.</returns>
        public async Task SeedAsync(RoutingCacheDto route)
        {
            await Blobs.PutAsync(
                Options.MapCacheContainer,
                StorageKeys.RouteBlobKey(route.Profile, route.FromLat, route.FromLon, route.ToLat, route.ToLon),
                BinaryData.FromString(route.RouteCoords),
                "application/json");
        }

        /// <summary>Runs the bootstrapper against this fixture's storage.</summary>
        /// <returns>A task representing the operation.</returns>
        public async Task BootstrapAsync()
        {
            // The bootstrapper resolves IUserService to seed the first administrator.
            // With no BootstrapAdmin configured that call returns immediately, so a
            // minimal container is enough here.
            var services = new ServiceCollection();
            services.AddSingleton(Tables);
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddScoped<IUserService, UserService>();
            using var provider = services.BuildServiceProvider();

            var bootstrapper = new StorageBootstrapper(
                Tables,
                Blobs,
                AsOptions(),
                provider.GetRequiredService<IServiceScopeFactory>(),
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
        public static void RequireAzurite()
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
