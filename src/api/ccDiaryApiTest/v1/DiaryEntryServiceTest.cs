// <copyright file="DiaryEntryServiceTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
    using Microsoft.EntityFrameworkCore;

    [TestClass]
    public class DiaryEntryServiceTest
    {
        [TestMethod]
        public void SearchDiaryEntries_ThrowsArgumentException_ForInvalidSearchType()
        {
            // Arrange
            var db = GetMemoryContext();
            var service = new DiaryEntryService(db);

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
            {
                service.SearchDiaryEntries(Guid.NewGuid(), DateTime.MinValue, DateTime.MaxValue, (SearchType)99);
            });
        }

        [TestMethod]
        public void CreateDiaryEntry_ThrowsArgumentException_WhenDateIsNull()
        {
            var db = GetMemoryContext();
            var service = new DiaryEntryService(db);

            Assert.ThrowsException<ArgumentException>(() =>
            {
                service.CreateDiaryEntry(new DiaryEntryDTO { DiaryId = Guid.NewGuid(), Entry = "E", Location = "L", Date = null });
            });
        }

        [TestMethod]
        public void CreateDiaryEntry_ThrowsArgumentException_WhenDateIsMinValue()
        {
            var db = GetMemoryContext();
            var service = new DiaryEntryService(db);

            Assert.ThrowsException<ArgumentException>(() =>
            {
                service.CreateDiaryEntry(new DiaryEntryDTO { DiaryId = Guid.NewGuid(), Entry = "E", Location = "L", Date = DateTime.MinValue });
            });
        }

        [TestMethod]
        public void MinDiaryEntryDate_ReturnsApproximatelyUtcNow_WhenNoDiaryEntries()
        {
            var db = GetMemoryContext();
            var service = new DiaryEntryService(db);
            var before = DateTime.UtcNow;

            var result = service.MinDiaryEntryDate(Guid.NewGuid());

            Assert.IsTrue(result >= before.AddSeconds(-1) && result <= DateTime.UtcNow.AddSeconds(1));
        }

        [TestMethod]
        public void MaxDiaryEntryDate_ReturnsApproximatelyUtcNow_WhenNoDiaryEntries()
        {
            var db = GetMemoryContext();
            var service = new DiaryEntryService(db);
            var before = DateTime.UtcNow;

            var result = service.MaxDiaryEntryDate(Guid.NewGuid());

            Assert.IsTrue(result >= before.AddSeconds(-1) && result <= DateTime.UtcNow.AddSeconds(1));
        }

        private static DiaryDatabaseContext GetMemoryContext()
        {
            var options = new DbContextOptionsBuilder<DiaryDatabaseContext>()
                .UseInMemoryDatabase(databaseName: "DiaryEntryServiceTest_" + Guid.NewGuid())
                .Options;
            return new DiaryDatabaseContext(options);
        }
    }
}
