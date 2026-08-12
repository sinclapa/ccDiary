// <copyright file="TableJsonTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Storage
{
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Data.Storage;

    /// <summary>
    /// Tests for the on-disk JSON shape used by the <c>Json</c> column.
    /// </summary>
    [TestClass]
    public class TableJsonTest
    {
        [TestMethod]
        public void RoundTrips_ADiaryEntry()
        {
            var entry = new DiaryEntryDTO
            {
                DiaryEntryId = Guid.NewGuid(),
                DiaryId = Guid.NewGuid(),
                Date = new DateTime(1916, 7, 1, 7, 30, 0, DateTimeKind.Utc),
                Location = "Ypres",
                Entry = "Arrived at the Menin Gate.",
                ShowMap = true,
                JourneyMode = JourneyMode.CrowFlies,
            };

            var restored = TableJson.Deserialize<DiaryEntryDTO>(TableJson.Serialize(entry));

            Assert.IsNotNull(restored);
            Assert.AreEqual(entry.DiaryEntryId, restored.DiaryEntryId);
            Assert.AreEqual(entry.Location, restored.Location);
            Assert.AreEqual(entry.Entry, restored.Entry);
            Assert.AreEqual(entry.ShowMap, restored.ShowMap);
            Assert.AreEqual(entry.Date, restored.Date);
        }

        [TestMethod]
        public void RestoresDatesAsUtc_NotUnspecified()
        {
            // A date coming back as Unspecified would shift the moment a caller
            // converted it, which is the bug UtcValueConverter existed to prevent.
            var entry = new DiaryEntryDTO
            {
                DiaryId = Guid.NewGuid(),
                Location = "L",
                Entry = "E",
                Date = new DateTime(1916, 7, 1, 7, 30, 0, DateTimeKind.Utc),
            };

            var restored = TableJson.Deserialize<DiaryEntryDTO>(TableJson.Serialize(entry));

            Assert.IsNotNull(restored?.Date);
            Assert.AreEqual(DateTimeKind.Utc, restored.Date!.Value.Kind);
        }

        [TestMethod]
        public void TreatsUnspecifiedDatesAsUtcRatherThanLocal()
        {
            var entry = new DiaryEntryDTO
            {
                DiaryId = Guid.NewGuid(),
                Location = "L",
                Entry = "E",
                Date = new DateTime(1916, 7, 1, 7, 30, 0, DateTimeKind.Unspecified),
            };

            var restored = TableJson.Deserialize<DiaryEntryDTO>(TableJson.Serialize(entry));

            Assert.AreEqual(new DateTime(1916, 7, 1, 7, 30, 0, DateTimeKind.Utc), restored?.Date);
        }

        [TestMethod]
        public void WritesEnumsAsKebabCase_MatchingTheHttpContract()
        {
            var entry = new DiaryEntryDTO
            {
                DiaryId = Guid.NewGuid(),
                Location = "L",
                Entry = "E",
                Date = DateTime.UtcNow,
                JourneyMode = JourneyMode.CrowFlies,
            };

            var json = TableJson.Serialize(entry);

            StringAssert.Contains(json, "crow-flies");
        }

        [TestMethod]
        public void ReadsEnumsBackFromKebabCase()
        {
            var entry = new DiaryEntryDTO
            {
                DiaryId = Guid.NewGuid(),
                Location = "L",
                Entry = "E",
                Date = DateTime.UtcNow,
                JourneyMode = JourneyMode.CrowFlies,
            };

            var restored = TableJson.Deserialize<DiaryEntryDTO>(TableJson.Serialize(entry));

            Assert.AreEqual(JourneyMode.CrowFlies, restored?.JourneyMode);
        }

        [TestMethod]
        public void Deserialize_ReturnsDefaultForEmptyInput()
        {
            Assert.IsNull(TableJson.Deserialize<DiaryEntryDTO>(null));
            Assert.IsNull(TableJson.Deserialize<DiaryEntryDTO>(string.Empty));
        }

        [TestMethod]
        public void ByteSize_CountsEncodedBytesNotCharacters()
        {
            // The 64 KB property limit is measured in bytes, so a multi-byte character
            // must count for more than one. Using string.Length here would let an entry
            // slip past the spill threshold and fail on write.
            Assert.AreEqual(3, TableJson.ByteSize("abc"));
            Assert.IsTrue(TableJson.ByteSize("€") > 1);
        }

        [TestMethod]
        public void MissingPropertiesFallBackToClrDefaults_SoOlderRowsStillLoad()
        {
            // This is the whole schema-evolution story in a store with no migrations:
            // a row written before a property existed must still deserialise.
            var json = """{"diaryId":"11111111-1111-1111-1111-111111111111","location":"L","entry":"E"}""";

            var restored = TableJson.Deserialize<DiaryEntryDTO>(json);

            Assert.IsNotNull(restored);
            Assert.AreEqual("L", restored.Location);
            Assert.IsFalse(restored.ShowJourney);
            Assert.IsNull(restored.ImageData);
        }
    }
}
