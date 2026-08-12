// <copyright file="BlobStoreTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Storage
{
    using ccDiaryApi.Data.Storage;

    /// <summary>
    /// Tests for blob operations against Azurite.
    /// </summary>
    [TestClass]
    public class BlobStoreTest
    {
        private StorageTestFixture _fixture = null!;
        private string _container = null!;

        [TestInitialize]
        public async Task Init()
        {
            _fixture = await StorageTestFixture.CreateAsync();
            _container = _fixture.Options.ImagesContainer;
        }

        [TestCleanup]
        public void Cleanup()
        {
            _fixture?.Dispose();
        }

        [TestMethod]
        public async Task RoundTripsContentAndContentType()
        {
            var payload = new byte[] { 1, 2, 3, 4 };

            await _fixture.Blobs.PutAsync(_container, "a/b", BinaryData.FromBytes(payload), "image/png");
            var stored = await _fixture.Blobs.TryGetAsync(_container, "a/b");

            Assert.IsNotNull(stored);
            CollectionAssert.AreEqual(payload, stored.Content.ToArray());
            Assert.AreEqual("image/png", stored.ContentType);
        }

        [TestMethod]
        public async Task ReportsLastModified_WhichTheMapCachesUseAsTheirExpiryClock()
        {
            var before = DateTimeOffset.UtcNow.AddMinutes(-1);

            await _fixture.Blobs.PutAsync(_container, "clock", BinaryData.FromString("x"));
            var stored = await _fixture.Blobs.TryGetAsync(_container, "clock");

            Assert.IsNotNull(stored);
            Assert.IsTrue(stored.LastModified > before, $"unexpected LastModified: {stored.LastModified}");
        }

        [TestMethod]
        public async Task TryGetReturnsNullForAMissingBlob_RatherThanThrowing()
        {
            Assert.IsNull(await _fixture.Blobs.TryGetAsync(_container, "does/not/exist"));
            Assert.IsNull(await _fixture.Blobs.TryGetStringAsync(_container, "does/not/exist"));
        }

        [TestMethod]
        public async Task OverwritesExistingContent()
        {
            await _fixture.Blobs.PutAsync(_container, "dup", BinaryData.FromString("first"));
            await _fixture.Blobs.PutAsync(_container, "dup", BinaryData.FromString("second"));

            Assert.AreEqual("second", await _fixture.Blobs.TryGetStringAsync(_container, "dup"));
        }

        [TestMethod]
        public async Task DeleteIfExistsReportsWhetherItRemovedAnything()
        {
            await _fixture.Blobs.PutAsync(_container, "gone", BinaryData.FromString("x"));

            Assert.IsTrue(await _fixture.Blobs.DeleteIfExistsAsync(_container, "gone"));
            Assert.IsFalse(await _fixture.Blobs.DeleteIfExistsAsync(_container, "gone"));
        }

        [TestMethod]
        public async Task DeleteByPrefixRemovesOnlyTheMatchingBlobs()
        {
            // This is how a diary's images are removed now that there is no cascade.
            var keep = Guid.NewGuid();
            var remove = Guid.NewGuid();

            await _fixture.Blobs.PutAsync(_container, $"{remove:N}/one", BinaryData.FromString("1"));
            await _fixture.Blobs.PutAsync(_container, $"{remove:N}/two", BinaryData.FromString("2"));
            await _fixture.Blobs.PutAsync(_container, $"{keep:N}/three", BinaryData.FromString("3"));

            var deleted = await _fixture.Blobs.DeleteByPrefixAsync(_container, $"{remove:N}/");

            Assert.AreEqual(2, deleted);
            Assert.IsNull(await _fixture.Blobs.TryGetStringAsync(_container, $"{remove:N}/one"));
            Assert.AreEqual("3", await _fixture.Blobs.TryGetStringAsync(_container, $"{keep:N}/three"));
        }

        [TestMethod]
        public async Task DeleteByPrefixOnAnEmptyPrefixDeletesNothing()
        {
            Assert.AreEqual(0, await _fixture.Blobs.DeleteByPrefixAsync(_container, $"{Guid.NewGuid():N}/"));
        }

        [TestMethod]
        public async Task StoresPayloadsFarLargerThanATableRowAllows()
        {
            // The reason images live in blobs at all: a Table entity caps at 1 MB and a
            // single string property at 64 KB, while real images reach ~3.3 MB base64.
            var large = new byte[4 * 1024 * 1024];
            Random.Shared.NextBytes(large);

            await _fixture.Blobs.PutAsync(_container, "large", BinaryData.FromBytes(large), "image/jpeg");
            var stored = await _fixture.Blobs.TryGetAsync(_container, "large");

            Assert.IsNotNull(stored);
            Assert.AreEqual(large.Length, stored.Content.ToArray().Length);
        }
    }
}
