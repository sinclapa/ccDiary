// <copyright file="TableJsonStorageTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Storage
{
    using ccDiaryApi.Data.Storage;
    using global::Azure.Data.Tables;

    /// <summary>
    /// Tests for the table query and batch helpers against Azurite.
    /// </summary>
    /// <remarks>
    /// These exercise the emulator rather than a fake precisely because the behaviours
    /// that matter here — row key ordering, filter evaluation, the 100-entity
    /// transaction limit and 404 semantics — are the ones a fake would get wrong.
    /// </remarks>
    [TestClass]
    public class TableJsonStorageTest
    {
        private static readonly string[] RowKeyOnly = new[] { "RowKey" };

        private StorageTestFixture _fixture = null!;
        private TableClient _table = null!;

        [TestInitialize]
        public async Task Init()
        {
            _fixture = await StorageTestFixture.CreateAsync();
            _table = _fixture.Tables.DiaryEntries;
        }

        [TestCleanup]
        public void Cleanup()
        {
            _fixture?.Dispose();
        }

        [TestMethod]
        public async Task GetIfExistsReturnsNullRatherThanThrowingForAMissingRow()
        {
            Assert.IsNull(await TableJson.GetIfExistsAsync(_table, "nope", "nope"));
        }

        [TestMethod]
        public async Task GetIfExistsReturnsTheRow()
        {
            await _table.AddEntityAsync(new TableEntity("p", "r") { { TableJson.JsonColumn, "{}" } });

            var entity = await TableJson.GetIfExistsAsync(_table, "p", "r");

            Assert.IsNotNull(entity);
            Assert.AreEqual("{}", entity.GetString(TableJson.JsonColumn));
        }

        [TestMethod]
        public async Task QueryReturnsRowsInRowKeyOrder_WhichIsWhatMakesEntriesDateSorted()
        {
            var diaryId = Guid.NewGuid();
            var partition = diaryId.ToString("N");
            var later = StorageKeys.EntryRowKey(new DateTime(1918, 11, 11, 11, 0, 0, DateTimeKind.Utc), Guid.NewGuid());
            var earlier = StorageKeys.EntryRowKey(new DateTime(1916, 7, 1, 7, 30, 0, DateTimeKind.Utc), Guid.NewGuid());

            await _table.AddEntityAsync(new TableEntity(partition, later));
            await _table.AddEntityAsync(new TableEntity(partition, earlier));

            var rows = await TableJson.QueryAsync(_table, $"PartitionKey eq '{partition}'");

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual(earlier, rows[0].RowKey);
            Assert.AreEqual(later, rows[1].RowKey);
        }

        [TestMethod]
        public async Task QuerySupportsRowKeyRangeFilters_ReplacingTheOldDateWhereClause()
        {
            var partition = Guid.NewGuid().ToString("N");
            var y1916 = StorageKeys.EntryRowKey(new DateTime(1916, 7, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid());
            var y1917 = StorageKeys.EntryRowKey(new DateTime(1917, 7, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid());
            var y1918 = StorageKeys.EntryRowKey(new DateTime(1918, 7, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid());

            foreach (var rowKey in new[] { y1916, y1917, y1918 })
            {
                await _table.AddEntityAsync(new TableEntity(partition, rowKey));
            }

            var from = StorageKeys.EntryRowKeyPrefix(new DateTime(1917, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var until = StorageKeys.EntryRowKeyPrefix(new DateTime(1918, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            var rows = await TableJson.QueryAsync(
                _table,
                $"PartitionKey eq '{partition}' and RowKey ge '{from}' and RowKey lt '{until}'");

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual(y1917, rows[0].RowKey);
        }

        [TestMethod]
        public async Task QueryHonoursSelect_SoLargeColumnsStayOffTheWire()
        {
            // Selecting only what is needed is what keeps a whole-partition scan
            // affordable when the rows carry serialised entries.
            var partition = Guid.NewGuid().ToString("N");
            await _table.AddEntityAsync(new TableEntity(partition, "r")
            {
                { TableJson.JsonColumn, "a very large serialised entry" },
                { "Date", DateTime.UtcNow },
            });

            var rows = await TableJson.QueryAsync(_table, $"PartitionKey eq '{partition}'", RowKeyOnly);

            Assert.AreEqual(1, rows.Count);
            Assert.IsFalse(rows[0].ContainsKey(TableJson.JsonColumn));
        }

        [TestMethod]
        public async Task QueryWithNoFilterReturnsEverything()
        {
            var partition = Guid.NewGuid().ToString("N");
            await _table.AddEntityAsync(new TableEntity(partition, "r"));

            Assert.IsTrue((await TableJson.QueryAsync(_table)).Count >= 1);
        }

        [TestMethod]
        public async Task DeleteBatchRemovesRows()
        {
            var partition = Guid.NewGuid().ToString("N");
            var keys = Enumerable.Range(0, 5).Select(i => $"r{i}").ToList();
            foreach (var key in keys)
            {
                await _table.AddEntityAsync(new TableEntity(partition, key));
            }

            await TableJson.DeleteBatchAsync(_table, partition, keys);

            Assert.AreEqual(0, (await TableJson.QueryAsync(_table, $"PartitionKey eq '{partition}'")).Count);
        }

        [TestMethod]
        public async Task DeleteBatchChunksBeyondTheHundredEntityTransactionLimit()
        {
            // A transaction caps at 100 entities, so deleting a diary with more entries
            // than that has to span several transactions.
            var partition = Guid.NewGuid().ToString("N");
            var keys = Enumerable.Range(0, 205).Select(i => $"r{i:D4}").ToList();
            foreach (var key in keys)
            {
                await _table.AddEntityAsync(new TableEntity(partition, key));
            }

            await TableJson.DeleteBatchAsync(_table, partition, keys);

            Assert.AreEqual(0, (await TableJson.QueryAsync(_table, $"PartitionKey eq '{partition}'")).Count);
        }

        [TestMethod]
        public async Task DeleteBatchWithNoKeysIsANoOp()
        {
            await TableJson.DeleteBatchAsync(_table, "p", Array.Empty<string>());
        }
    }
}
