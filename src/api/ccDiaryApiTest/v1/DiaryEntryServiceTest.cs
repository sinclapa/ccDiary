// <copyright file="DiaryEntryServiceTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Data.Storage;
    using ccDiaryApi.Services;
    using ccDiaryApiTest.Storage;
    using global::Azure;
    using global::Azure.Data.Tables;

    [TestClass]
    public class DiaryEntryServiceTest
    {
        private StorageTestFixture _fixture = null!;
        private DiaryEntryService _service = null!;

        [TestInitialize]
        public async Task Init()
        {
            _fixture = await StorageTestFixture.CreateAsync();
            _service = new DiaryEntryService(_fixture.Tables, _fixture.Blobs, _fixture.AsOptions());
        }

        [TestCleanup]
        public void Cleanup()
        {
            _fixture?.Dispose();
        }

        [TestMethod]
        public async Task SearchDiaryEntries_ThrowsArgumentException_ForInvalidSearchType()
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            {
                await _service.SearchDiaryEntriesAsync(Guid.NewGuid(), DateTime.MinValue, DateTime.MaxValue, (SearchType)99);
            });
        }

        [TestMethod]
        public async Task CreateDiaryEntry_ThrowsArgumentException_WhenDateIsNull()
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            {
                await _service.CreateDiaryEntryAsync(new DiaryEntryDTO { DiaryId = Guid.NewGuid(), Entry = "E", Location = "L", Date = null });
            });
        }

        [TestMethod]
        public async Task CreateDiaryEntry_ThrowsArgumentException_WhenDateIsMinValue()
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
            {
                await _service.CreateDiaryEntryAsync(new DiaryEntryDTO { DiaryId = Guid.NewGuid(), Entry = "E", Location = "L", Date = DateTime.MinValue });
            });
        }

        [TestMethod]
        public async Task MinDiaryEntryDate_ReturnsApproximatelyUtcNow_WhenNoDiaryEntries()
        {
            var before = DateTime.UtcNow;

            var result = await _service.MinDiaryEntryDateAsync(Guid.NewGuid());

            Assert.IsTrue(result >= before.AddSeconds(-1) && result <= DateTime.UtcNow.AddSeconds(1));
        }

        [TestMethod]
        public async Task MaxDiaryEntryDate_ReturnsApproximatelyUtcNow_WhenNoDiaryEntries()
        {
            var before = DateTime.UtcNow;

            var result = await _service.MaxDiaryEntryDateAsync(Guid.NewGuid());

            Assert.IsTrue(result >= before.AddSeconds(-1) && result <= DateTime.UtcNow.AddSeconds(1));
        }

        [TestMethod]
        public async Task GetDiaryDateRange_ReturnsSentinelBounds_WhenNoDiaryEntries()
        {
            // Preserved from the relational implementation: an empty diary reports an
            // inverted range rather than throwing, and callers depend on it.
            var range = await _service.GetDiaryDateRangeAsync(Guid.NewGuid());

            Assert.AreEqual(DateTime.MaxValue, range.MaxDateTime);
            Assert.AreEqual(DateTime.MinValue, range.MinDateTime);
        }

        [TestMethod]
        public async Task GetDiaryEntries_ReturnsEntriesInDateOrder_RegardlessOfInsertOrder()
        {
            // Ordering comes from the row key, not from a sort, so inserting out of
            // order is the meaningful test.
            var diaryId = Guid.NewGuid();
            await CreateAsync(diaryId, new DateTime(1918, 11, 11, 11, 0, 0, DateTimeKind.Utc), "Armistice");
            await CreateAsync(diaryId, new DateTime(1916, 7, 1, 7, 30, 0, DateTimeKind.Utc), "Somme");
            await CreateAsync(diaryId, new DateTime(1917, 7, 31, 3, 50, 0, DateTimeKind.Utc), "Passchendaele");

            var entries = await _service.GetDiaryEntriesAsync(diaryId);

            CollectionAssert.AreEqual(
                new[] { "Somme", "Passchendaele", "Armistice" },
                entries.Select(e => e.Entry).ToArray());
        }

        [TestMethod]
        public async Task GetDiaryEntries_FiltersByDateRange()
        {
            var diaryId = Guid.NewGuid();
            await CreateAsync(diaryId, new DateTime(1916, 1, 1, 0, 0, 0, DateTimeKind.Utc), "before");
            await CreateAsync(diaryId, new DateTime(1917, 6, 1, 0, 0, 0, DateTimeKind.Utc), "inside");
            await CreateAsync(diaryId, new DateTime(1918, 1, 1, 0, 0, 0, DateTimeKind.Utc), "after");

            var entries = await _service.GetDiaryEntriesAsync(
                diaryId,
                new DateTime(1917, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(1917, 12, 31, 23, 59, 59, DateTimeKind.Utc));

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("inside", entries[0].Entry);
        }

        [TestMethod]
        public async Task GetDiaryEntries_RangeIsInclusiveOfBothBounds()
        {
            var diaryId = Guid.NewGuid();
            var from = new DateTime(1917, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var until = new DateTime(1917, 12, 31, 0, 0, 0, DateTimeKind.Utc);
            await CreateAsync(diaryId, from, "first");
            await CreateAsync(diaryId, until, "last");

            var entries = await _service.GetDiaryEntriesAsync(diaryId, from, until);

            Assert.AreEqual(2, entries.Count);
        }

        [TestMethod]
        public async Task MovingAnEntrysDateDoesNotLeaveTheOldRowBehind()
        {
            // The date is part of the row key and row keys are immutable, so an edit
            // that changes the date writes a new row; the old one must not survive.
            var diaryId = Guid.NewGuid();
            var entry = await CreateAsync(diaryId, new DateTime(1916, 7, 1, 0, 0, 0, DateTimeKind.Utc), "moved");

            entry.Date = new DateTime(1917, 7, 1, 0, 0, 0, DateTimeKind.Utc);
            await _service.UpdateDiaryEntryAsync(entry);

            var entries = await _service.GetDiaryEntriesAsync(diaryId);
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(new DateTime(1917, 7, 1, 0, 0, 0, DateTimeKind.Utc), entries[0].Date);
        }

        [TestMethod]
        public async Task RoundTripsAnImageThroughBlobStorage()
        {
            // Images exceed what a table row can hold, so they live in blobs — but the
            // API contract still returns base64, so the round trip must be exact.
            var diaryId = Guid.NewGuid();
            var bytes = new byte[] { 1, 2, 3, 4, 5 };
            var entry = new DiaryEntryDTO
            {
                DiaryId = diaryId,
                Date = DateTime.UtcNow,
                Location = "L",
                Entry = "E",
                ImageData = Convert.ToBase64String(bytes),
                ImageContentType = "image/png",
            };

            var created = await _service.CreateDiaryEntryAsync(entry);
            var fetched = await _service.GetDiaryEntryAsync(created.DiaryEntryId!.Value);

            Assert.IsNotNull(fetched);
            Assert.AreEqual(Convert.ToBase64String(bytes), fetched.ImageData);
            Assert.AreEqual("image/png", fetched.ImageContentType);
        }

        [TestMethod]
        public async Task DeletingAnEntryRemovesItsImage()
        {
            var diaryId = Guid.NewGuid();
            var entry = await _service.CreateDiaryEntryAsync(new DiaryEntryDTO
            {
                DiaryId = diaryId,
                Date = DateTime.UtcNow,
                Location = "L",
                Entry = "E",
                ImageData = Convert.ToBase64String(new byte[] { 9, 9 }),
                ImageContentType = "image/png",
            });

            await _service.DeleteDiaryEntryAsync(entry);

            Assert.IsNull(await _service.GetDiaryEntryAsync(entry.DiaryEntryId!.Value));
            Assert.IsNull(await _fixture.Blobs.TryGetAsync(
                _fixture.Options.ImagesContainer,
                ccDiaryApi.Data.Storage.StorageKeys.ImageBlobKey(diaryId, entry.DiaryEntryId!.Value)));
        }

        [TestMethod]
        public async Task RoundTripsSeveralImagesInOrder()
        {
            var diaryId = Guid.NewGuid();
            var created = await CreateWithImagesAsync(diaryId, Image(1, "image/png"), Image(2, "image/jpeg"), Image(3, "image/webp"));

            var fetched = await _service.GetDiaryEntryAsync(created.DiaryEntryId!.Value);

            Assert.IsNotNull(fetched);
            CollectionAssert.AreEqual(
                new[] { Base64(1), Base64(2), Base64(3) },
                fetched.Images!.Select(i => i.Data).ToArray());
            CollectionAssert.AreEqual(
                new[] { "image/png", "image/jpeg", "image/webp" },
                fetched.Images!.Select(i => i.ContentType).ToArray());
        }

        [TestMethod]
        public async Task RepeatsTheFirstImageInTheSingleImageFields()
        {
            // Archives and older callers read ImageData; they must still see an entry's picture.
            var created = await CreateWithImagesAsync(Guid.NewGuid(), Image(1, "image/png"), Image(2, "image/jpeg"));

            var fetched = await _service.GetDiaryEntryAsync(created.DiaryEntryId!.Value);

            Assert.AreEqual(Base64(1), fetched!.ImageData);
            Assert.AreEqual("image/png", fetched.ImageContentType);
        }

        [TestMethod]
        public async Task ASingleImageRequestIsAOneImageEntry()
        {
            var created = await _service.CreateDiaryEntryAsync(new DiaryEntryDTO
            {
                DiaryId = Guid.NewGuid(),
                Date = DateTime.UtcNow,
                Location = "L",
                Entry = "E",
                ImageData = Base64(7),
                ImageContentType = "image/png",
            });

            var fetched = await _service.GetDiaryEntryAsync(created.DiaryEntryId!.Value);

            Assert.AreEqual(1, fetched!.Images!.Count);
            Assert.AreEqual(Base64(7), fetched.Images[0].Data);
        }

        [TestMethod]
        public async Task RemovingImagesDeletesTheirBlobs()
        {
            var diaryId = Guid.NewGuid();
            var entry = await CreateWithImagesAsync(diaryId, Image(1, "image/png"), Image(2, "image/png"), Image(3, "image/png"));

            entry.Images = new List<DiaryEntryImageDTO> { Image(1, "image/png") };
            await _service.UpdateDiaryEntryAsync(entry);

            var fetched = await _service.GetDiaryEntryAsync(entry.DiaryEntryId!.Value);
            Assert.AreEqual(1, fetched!.Images!.Count);
            Assert.IsNotNull(await ImageBlobAsync(diaryId, entry, 0));
            Assert.IsNull(await ImageBlobAsync(diaryId, entry, 1));
            Assert.IsNull(await ImageBlobAsync(diaryId, entry, 2));
        }

        [TestMethod]
        public async Task AnEmptyImageListRemovesEveryImage()
        {
            // An empty list is a request for no images; only an absent one defers to ImageData.
            var diaryId = Guid.NewGuid();
            var entry = await CreateWithImagesAsync(diaryId, Image(1, "image/png"), Image(2, "image/png"));

            entry.Images = new List<DiaryEntryImageDTO>();
            entry.ImageData = Base64(1);
            await _service.UpdateDiaryEntryAsync(entry);

            var fetched = await _service.GetDiaryEntryAsync(entry.DiaryEntryId!.Value);
            Assert.AreEqual(0, fetched!.Images!.Count);
            Assert.IsNull(fetched.ImageData);
            Assert.IsNull(await ImageBlobAsync(diaryId, entry, 0));
            Assert.IsNull(await ImageBlobAsync(diaryId, entry, 1));
        }

        [TestMethod]
        public async Task CreatingWithTheListReturnsTheFirstImageInTheSingleImageFields()
        {
            // The create response is the DTO itself, so it must carry the legacy fields too.
            var created = await CreateWithImagesAsync(Guid.NewGuid(), Image(1, "image/png"), Image(2, "image/jpeg"));

            Assert.AreEqual(Base64(1), created.ImageData);
            Assert.AreEqual("image/png", created.ImageContentType);
            Assert.AreEqual(2, created.Images!.Count);
        }

        [TestMethod]
        public async Task CreatingWithTheSingleImageFieldsReturnsTheList()
        {
            var created = await _service.CreateDiaryEntryAsync(new DiaryEntryDTO
            {
                DiaryId = Guid.NewGuid(),
                Date = DateTime.UtcNow,
                Location = "L",
                Entry = "E",
                ImageData = Base64(7),
                ImageContentType = "image/png",
            });

            Assert.AreEqual(1, created.Images!.Count);
            Assert.AreEqual(Base64(7), created.Images[0].Data);
        }

        [TestMethod]
        public async Task UpdatingToNoImagesReturnsAndReadsNoContentType()
        {
            var entry = await CreateWithImagesAsync(Guid.NewGuid(), Image(1, "image/png"));

            entry.Images = new List<DiaryEntryImageDTO>();
            var updated = await _service.UpdateDiaryEntryAsync(entry);
            var fetched = await _service.GetDiaryEntryAsync(entry.DiaryEntryId!.Value);

            Assert.IsNull(updated.ImageData);
            Assert.IsNull(updated.ImageContentType);
            Assert.IsNull(fetched!.ImageContentType);
        }

        [TestMethod]
        public async Task ReorderingImagesIsKept()
        {
            var entry = await CreateWithImagesAsync(Guid.NewGuid(), Image(1, "image/png"), Image(2, "image/jpeg"));

            entry.Images = new List<DiaryEntryImageDTO> { Image(2, "image/jpeg"), Image(1, "image/png") };
            await _service.UpdateDiaryEntryAsync(entry);

            var fetched = await _service.GetDiaryEntryAsync(entry.DiaryEntryId!.Value);
            CollectionAssert.AreEqual(new[] { Base64(2), Base64(1) }, fetched!.Images!.Select(i => i.Data).ToArray());
            Assert.AreEqual("image/jpeg", fetched.ImageContentType);
        }

        [TestMethod]
        public async Task MovingAnEntrysDateKeepsItsImages()
        {
            var entry = await CreateWithImagesAsync(Guid.NewGuid(), Image(1, "image/png"), Image(2, "image/png"));

            entry.Date = entry.Date!.Value.AddDays(1);
            await _service.UpdateDiaryEntryAsync(entry);

            var fetched = await _service.GetDiaryEntryAsync(entry.DiaryEntryId!.Value);
            Assert.AreEqual(2, fetched!.Images!.Count);
        }

        [TestMethod]
        public async Task DeletingAnEntryRemovesAllItsImages()
        {
            var diaryId = Guid.NewGuid();
            var entry = await CreateWithImagesAsync(diaryId, Image(1, "image/png"), Image(2, "image/png"), Image(3, "image/png"));

            await _service.DeleteDiaryEntryAsync(entry);

            for (var i = 0; i < 3; i++)
            {
                Assert.IsNull(await ImageBlobAsync(diaryId, entry, i), $"image {i} survived");
            }
        }

        [TestMethod]
        public async Task DeletingAnEntryLeavesAnotherEntrysImages()
        {
            // The delete is a prefix scan, so it must not reach a neighbour in the same diary.
            var diaryId = Guid.NewGuid();
            var doomed = await CreateWithImagesAsync(diaryId, Image(1, "image/png"), Image(2, "image/png"));
            var kept = await CreateWithImagesAsync(diaryId, Image(3, "image/png"), Image(4, "image/png"));

            await _service.DeleteDiaryEntryAsync(doomed);

            var fetched = await _service.GetDiaryEntryAsync(kept.DiaryEntryId!.Value);
            Assert.AreEqual(2, fetched!.Images!.Count);
        }

        [TestMethod]
        public async Task ARowWrittenBeforeImageCountsReadsAsOneImage()
        {
            // Rows from before an entry could hold several images carry HasImage and no count.
            var diaryId = Guid.NewGuid();
            var entry = await CreateWithImagesAsync(diaryId, Image(1, "image/png"));
            var partition = diaryId.ToString("N");
            TableEntity? row = null;
            await foreach (var r in _fixture.Tables.DiaryEntries.QueryAsync<TableEntity>(e => e.PartitionKey == partition))
            {
                row = r;
            }

            row!.Remove("ImageCount");
            await _fixture.Tables.DiaryEntries.UpdateEntityAsync(row, ETag.All, TableUpdateMode.Replace);

            var fetched = await _service.GetDiaryEntryAsync(entry.DiaryEntryId!.Value);

            Assert.AreEqual(1, fetched!.Images!.Count);
            Assert.AreEqual(Base64(1), fetched.ImageData);
        }

        [TestMethod]
        public async Task SpillsOversizedEntryTextToABlobAndReadsItBack()
        {
            // A Table string property caps at 64 KB. Entries are normally far smaller,
            // so this path is rare — but it has to work when it is taken.
            var diaryId = Guid.NewGuid();
            var large = new string('x', _fixture.Options.JsonSpillThresholdBytes + 1000);

            var created = await _service.CreateDiaryEntryAsync(new DiaryEntryDTO
            {
                DiaryId = diaryId,
                Date = DateTime.UtcNow,
                Location = "L",
                Entry = large,
            });

            var fetched = await _service.GetDiaryEntryAsync(created.DiaryEntryId!.Value);

            Assert.IsNotNull(fetched);
            Assert.AreEqual(large, fetched.Entry);
        }

        [TestMethod]
        public async Task TextSearchMatchesAcrossEntryAndLocationFields()
        {
            var diaryId = Guid.NewGuid();
            await CreateAsync(diaryId, new DateTime(1917, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Arrived at the Menin Gate.", "Ypres");
            await CreateAsync(diaryId, new DateTime(1917, 2, 1, 0, 0, 0, DateTimeKind.Utc), "Quiet day.", "Passchendaele");

            var byEntry = await _service.TextSearchDiaryEntriesAsync(diaryId, "Menin");
            var byLocation = await _service.TextSearchDiaryEntriesAsync(diaryId, "Passchendaele");
            var noMatch = await _service.TextSearchDiaryEntriesAsync(diaryId, "zzznomatch");

            Assert.AreEqual(1, byEntry.TotalCount);
            Assert.AreEqual(1, byLocation.TotalCount);
            Assert.AreEqual(0, noMatch.TotalCount);
        }

        [TestMethod]
        public async Task SearchDiaryEntriesReturnsDistinctYears()
        {
            var diaryId = Guid.NewGuid();
            await CreateAsync(diaryId, new DateTime(1916, 7, 1, 0, 0, 0, DateTimeKind.Utc), "a");
            await CreateAsync(diaryId, new DateTime(1916, 8, 1, 0, 0, 0, DateTimeKind.Utc), "b");
            await CreateAsync(diaryId, new DateTime(1918, 1, 1, 0, 0, 0, DateTimeKind.Utc), "c");

            var years = await _service.SearchDiaryEntriesAsync(
                diaryId, DateTime.MinValue, DateTime.MaxValue, SearchType.Year);

            CollectionAssert.AreEqual(new[] { 1916, 1918 }, years.ToArray());
        }

        [TestMethod]
        public async Task MinAndMaxReflectTheStoredEntries()
        {
            var diaryId = Guid.NewGuid();
            var first = new DateTime(1916, 7, 1, 0, 0, 0, DateTimeKind.Utc);
            var last = new DateTime(1918, 11, 11, 0, 0, 0, DateTimeKind.Utc);
            await CreateAsync(diaryId, last, "last");
            await CreateAsync(diaryId, first, "first");

            Assert.AreEqual(first, await _service.MinDiaryEntryDateAsync(diaryId));
            Assert.AreEqual(last, await _service.MaxDiaryEntryDateAsync(diaryId));
        }

        private static string Base64(byte marker) => Convert.ToBase64String(new byte[] { marker, marker, marker });

        private static DiaryEntryImageDTO Image(byte marker, string contentType) =>
            new DiaryEntryImageDTO { Data = Base64(marker), ContentType = contentType };

        private async Task<DiaryEntryDTO> CreateAsync(Guid diaryId, DateTime date, string entry, string location = "L")
        {
            return await _service.CreateDiaryEntryAsync(new DiaryEntryDTO
            {
                DiaryId = diaryId,
                Date = date,
                Location = location,
                Entry = entry,
            });
        }

        private async Task<DiaryEntryDTO> CreateWithImagesAsync(Guid diaryId, params DiaryEntryImageDTO[] images)
        {
            return await _service.CreateDiaryEntryAsync(new DiaryEntryDTO
            {
                DiaryId = diaryId,
                Date = DateTime.UtcNow,
                Location = "L",
                Entry = "E",
                Images = images.ToList(),
            });
        }

        private Task<StoredBlob?> ImageBlobAsync(Guid diaryId, DiaryEntryDTO entry, int index) =>
            _fixture.Blobs.TryGetAsync(
                _fixture.Options.ImagesContainer,
                StorageKeys.ImageBlobKey(diaryId, entry.DiaryEntryId!.Value, index));
    }
}
