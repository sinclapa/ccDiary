// <copyright file="StorageBootstrapperTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Storage
{
    using ccDiaryApi.Data.Storage;
    using ccDiaryApi.Infrastructure;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Tests for the startup step that replaces EF migrations.
    /// </summary>
    [TestClass]
    public class StorageBootstrapperTest
    {
        private StorageTestFixture _fixture = null!;

        [TestInitialize]
        public async Task Init()
        {
            _fixture = await StorageTestFixture.CreateAsync();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _fixture?.Dispose();
        }

        [TestMethod]
        public async Task CreatesEveryTable()
        {
            // The fixture has already bootstrapped; a write proves each table exists,
            // because Table Storage rejects a write to a table that was never created.
            foreach (var table in _fixture.Tables.All)
            {
                var entity = new Azure.Data.Tables.TableEntity("p", Guid.NewGuid().ToString("N"));
                await table.AddEntityAsync(entity);
            }
        }

        [TestMethod]
        public async Task CreatesEveryContainer()
        {
            foreach (var name in new[]
            {
                _fixture.Options.ImagesContainer,
                _fixture.Options.MapCacheContainer,
                _fixture.Options.ContentContainer,
            })
            {
                Assert.IsTrue(
                    await _fixture.Blobs.Container(name).ExistsAsync(),
                    $"container missing: {name}");
            }
        }

        [TestMethod]
        public async Task IsIdempotent_SoRestartsAndScaleOutAreSafe()
        {
            // Every replica runs this on boot, so a second run must be a no-op rather
            // than a conflict.
            await _fixture.BootstrapAsync();
            await _fixture.BootstrapAsync();

            Assert.IsTrue(await _fixture.Blobs.Container(_fixture.Options.ImagesContainer).ExistsAsync());
        }

        [TestMethod]
        public async Task DoesNothingWhenStorageIsNotConfigured()
        {
            // Absent configuration must not throw here; Program.cs validates and fails
            // fast with a clear message instead.
            var options = Options.Create(new StorageOptions());
            var bootstrapper = new StorageBootstrapper(
                new TableStore(options),
                new BlobStore(options),
                options,
                NullLogger<StorageBootstrapper>.Instance);

            await bootstrapper.StartAsync(CancellationToken.None);
        }

        [TestMethod]
        public async Task StopAsyncCompletes()
        {
            var options = Options.Create(new StorageOptions());
            var bootstrapper = new StorageBootstrapper(
                new TableStore(options),
                new BlobStore(options),
                options,
                NullLogger<StorageBootstrapper>.Instance);

            await bootstrapper.StopAsync(CancellationToken.None);
        }

        [TestMethod]
        public void UnconfiguredStoresReportNotConfigured()
        {
            var options = Options.Create(new StorageOptions());

            Assert.IsFalse(new TableStore(options).IsConfigured);
            Assert.IsFalse(new BlobStore(options).IsConfigured);
            Assert.IsFalse(options.Value.IsConfigured);
        }

        [TestMethod]
        public void ConfiguredByAccountNameReportsConfigured()
        {
            // The managed identity path: account name only, no secret anywhere.
            var options = Options.Create(new StorageOptions { AccountName = "examplestorage" });

            Assert.IsTrue(options.Value.IsConfigured);
            Assert.IsTrue(new TableStore(options).IsConfigured);
            Assert.IsTrue(new BlobStore(options).IsConfigured);
        }

        [TestMethod]
        public void UnconfiguredBlobStoreThrowsAClearErrorWhenUsed()
        {
            var options = Options.Create(new StorageOptions());
            var blobs = new BlobStore(options);

            Assert.ThrowsException<InvalidOperationException>(() => blobs.Container("images"));
        }

        [TestMethod]
        public void AppliesTheConfiguredTableNamePrefix()
        {
            var options = Options.Create(new StorageOptions
            {
                ConnectionString = StorageTestFixture.AzuriteConnectionString,
                TableNamePrefix = "pfx",
            });

            Assert.AreEqual("pfxdiary", new TableStore(options).Diaries.Name);
        }
    }
}
