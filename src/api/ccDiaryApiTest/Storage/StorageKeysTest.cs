// <copyright file="StorageKeysTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Storage
{
    using System.Text.RegularExpressions;
    using ccDiaryApi.Data.Storage;

    /// <summary>
    /// Tests for the key derivation functions.
    /// </summary>
    /// <remarks>
    /// These matter more than their size suggests. A key-value store has no migration
    /// step, so a change in how a key is derived does not move existing rows, it orphans
    /// them. These tests pin the derivations down.
    /// </remarks>
    [TestClass]
    public class StorageKeysTest
    {
        [TestMethod]
        public void EntryRowKey_OrdersChronologically_AsPlainStringComparison()
        {
            // Table Storage sorts row keys lexicographically, and that is the only
            // reason entries come back date-ordered without a secondary index.
            var id = Guid.NewGuid();
            var earlier = StorageKeys.EntryRowKey(new DateTime(1916, 7, 1, 7, 30, 0, DateTimeKind.Utc), id);
            var later = StorageKeys.EntryRowKey(new DateTime(1916, 7, 1, 7, 30, 1, DateTimeKind.Utc), id);

            Assert.IsTrue(string.CompareOrdinal(earlier, later) < 0);
        }

        [TestMethod]
        public void EntryRowKey_OrdersAcrossCenturyBoundary()
        {
            var id = Guid.NewGuid();
            var nineteen = StorageKeys.EntryRowKey(new DateTime(1999, 12, 31, 23, 59, 59, DateTimeKind.Utc), id);
            var twenty = StorageKeys.EntryRowKey(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), id);

            Assert.IsTrue(string.CompareOrdinal(nineteen, twenty) < 0);
        }

        [TestMethod]
        public void EntryRowKey_WithNullDate_SortsBeforeEveryRealDate()
        {
            var id = Guid.NewGuid();
            var nullDate = StorageKeys.EntryRowKey(null, id);
            var minDate = StorageKeys.EntryRowKey(DateTime.MinValue, id);

            Assert.IsTrue(string.CompareOrdinal(nullDate, minDate) <= 0);
            Assert.IsTrue(string.CompareOrdinal(nullDate, StorageKeys.EntryRowKey(new DateTime(1, 1, 2, 0, 0, 0, DateTimeKind.Utc), id)) < 0);
        }

        [TestMethod]
        public void EntryRowKey_TreatsUnspecifiedKindAsUtc_SoKeysDoNotDependOnServerTimeZone()
        {
            // A key that shifted with the host's time zone would make the same entry
            // addressable at two different row keys on two different machines.
            var value = new DateTime(1916, 7, 1, 7, 30, 0, DateTimeKind.Unspecified);
            var id = Guid.NewGuid();

            var key = StorageKeys.EntryRowKey(value, id);

            StringAssert.StartsWith(key, "19160701073000");
        }

        [TestMethod]
        public void EntryRowKey_ConvertsLocalKindToUtc()
        {
            var utc = new DateTime(1916, 7, 1, 7, 30, 0, DateTimeKind.Utc);
            var local = utc.ToLocalTime();

            Assert.AreEqual(
                StorageKeys.EntryRowKey(utc, Guid.Empty),
                StorageKeys.EntryRowKey(local, Guid.Empty));
        }

        [TestMethod]
        public void EntryRowKey_WithSameDate_BreaksTiesDeterministicallyById()
        {
            var date = new DateTime(1916, 7, 1, 7, 30, 0, DateTimeKind.Utc);
            var first = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var second = Guid.Parse("00000000-0000-0000-0000-000000000002");

            var a = StorageKeys.EntryRowKey(date, first);
            var b = StorageKeys.EntryRowKey(date, second);

            Assert.AreNotEqual(a, b);
            Assert.IsTrue(string.CompareOrdinal(a, b) < 0);
        }

        [TestMethod]
        public void EntryRowKeyPrefix_MatchesTheRowKeyPrefix_SoRangeFiltersLineUp()
        {
            var date = new DateTime(1916, 7, 1, 7, 30, 0, DateTimeKind.Utc);
            var prefix = StorageKeys.EntryRowKeyPrefix(date);

            StringAssert.StartsWith(StorageKeys.EntryRowKey(date, Guid.NewGuid()), prefix);
        }

        [TestMethod]
        public void GeocodeKey_IsDeterministicAndIgnoresCaseAndSurroundingWhitespace()
        {
            var a = StorageKeys.GeocodeKey("Ypres, Belgium");
            var b = StorageKeys.GeocodeKey("  ypres, BELGIUM  ");

            Assert.AreEqual(a, b);
        }

        [TestMethod]
        public void GeocodeKey_DiffersForDifferentQueries()
        {
            Assert.AreNotEqual(
                StorageKeys.GeocodeKey("Ypres"),
                StorageKeys.GeocodeKey("Verdun"));
        }

        [TestMethod]
        public void GeocodeKey_ProducesALegalRowKey_EvenForQueriesFullOfIllegalCharacters()
        {
            // Row keys forbid / \ # ? — all of which appear in real search text.
            var key = StorageKeys.GeocodeKey("a/b\\c#d?e");

            Assert.IsTrue(Regex.IsMatch(key, "^[0-9A-F]{32}$"), $"unexpected key: {key}");
        }

        [TestMethod]
        public void GeocodeKey_HandlesEmptyInput()
        {
            Assert.AreEqual(32, StorageKeys.GeocodeKey(string.Empty).Length);
        }

        [TestMethod]
        public void RouteBlobKey_QuantisesToSixDecimalPlaces()
        {
            // Differences below ~0.1 m must collapse to one cache entry, which is what
            // makes the lookup an exact match instead of the old tolerance comparison.
            var a = StorageKeys.RouteBlobKey("driving", 51.1234567, -1.2345678, 50.9, 1.1);
            var b = StorageKeys.RouteBlobKey("driving", 51.1234569, -1.2345676, 50.9, 1.1);

            Assert.AreEqual(a, b);
        }

        [TestMethod]
        public void RouteBlobKey_DistinguishesCoordinatesBeyondTheQuantisation()
        {
            Assert.AreNotEqual(
                StorageKeys.RouteBlobKey("driving", 51.123456, -1.0, 50.9, 1.1),
                StorageKeys.RouteBlobKey("driving", 51.123457, -1.0, 50.9, 1.1));
        }

        [TestMethod]
        public void RouteBlobKey_HandlesNegativeCoordinates()
        {
            var key = StorageKeys.RouteBlobKey("driving", -51.5, -1.25, -50.0, -0.5);

            StringAssert.StartsWith(key, "routes/driving/-51500000_-1250000_");
            StringAssert.EndsWith(key, ".json");
        }

        [TestMethod]
        public void RouteBlobKey_DistinguishesProfiles()
        {
            Assert.AreNotEqual(
                StorageKeys.RouteBlobKey("driving", 51.0, -1.0, 50.0, 1.0),
                StorageKeys.RouteBlobKey("walking", 51.0, -1.0, 50.0, 1.0));
        }

        [TestMethod]
        public void TileBlobKey_UsesTheSourceAndTileCoordinates()
        {
            Assert.AreEqual("tiles/osm/12/2045/1362", StorageKeys.TileBlobKey("osm", 12, 2045, 1362));
        }

        [TestMethod]
        public void ImageBlobKey_IsPrefixedByDiary_SoCascadeDeleteIsAPrefixScan()
        {
            var diaryId = Guid.NewGuid();
            var entryId = Guid.NewGuid();

            var key = StorageKeys.ImageBlobKey(diaryId, entryId);

            Assert.AreEqual($"{diaryId:N}/{entryId:N}", key);
            StringAssert.StartsWith(key, $"{diaryId:N}/");
        }

        [TestMethod]
        public void EntryJsonBlobKey_IsScopedToTheEntriesFolder()
        {
            var entryId = Guid.NewGuid();

            Assert.AreEqual($"entries/{entryId:N}.json", StorageKeys.EntryJsonBlobKey(entryId));
        }

        [TestMethod]
        public void SanitiseKey_ReplacesEveryCharacterATableKeyForbids()
        {
            Assert.AreEqual("a_b_c_d_e", StorageKeys.SanitiseKey("a/b\\c#d?e"));
        }

        [TestMethod]
        public void SanitiseKey_ReplacesControlCharacters()
        {
            Assert.AreEqual("a_b", StorageKeys.SanitiseKey("a" + (char)1 + "b"));
            Assert.AreEqual("a_b", StorageKeys.SanitiseKey("a" + (char)9 + "b"));
        }

        [TestMethod]
        public void SanitiseKey_LeavesLegalTextAlone()
        {
            Assert.AreEqual("Ypres, Belgium 1916", StorageKeys.SanitiseKey("Ypres, Belgium 1916"));
        }

        [TestMethod]
        public void SanitiseKey_HandlesNullAndEmpty()
        {
            Assert.AreEqual(string.Empty, StorageKeys.SanitiseKey(null));
            Assert.AreEqual(string.Empty, StorageKeys.SanitiseKey(string.Empty));
        }

        [TestMethod]
        public void SanitiseKey_TruncatesToTheKeyLengthLimit()
        {
            Assert.AreEqual(1024, StorageKeys.SanitiseKey(new string('x', 2000)).Length);
        }
    }
}
