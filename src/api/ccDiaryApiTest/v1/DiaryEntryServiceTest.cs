// <copyright file="DiaryEntryServiceTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Services;
    using Microsoft.EntityFrameworkCore;

    [TestClass]
    public class DiaryEntryServiceTest
    {
        private static DiaryDatabaseContext GetMemoryContext()
        {
            var options = new DbContextOptionsBuilder<DiaryDatabaseContext>()
                .UseInMemoryDatabase(databaseName: "DiaryEntryServiceTest_" + Guid.NewGuid())
                .Options;
            return new DiaryDatabaseContext(options);
        }

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
    }
}
