// <copyright file="DiaryArchiveControllerTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System;
    using ccDiaryApi.Controllers.v1;
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    [TestClass]
    public class DiaryArchiveControllerTest
    {
        public static DiaryDatabaseContext GetMemoryContext()
        {
            var options = new DbContextOptionsBuilder<DiaryDatabaseContext>()
                .UseInMemoryDatabase(databaseName: "InMemoryDatabase")
                .EnableSensitiveDataLogging()
                .Options;
            return new DiaryDatabaseContext(options);
        }

        [TestInitialize]
        public void InitDb()
        {
            var db = GetMemoryContext();
            if (db.Database.IsInMemory())
            {
                db.Database.EnsureDeleted();
            }
        }

        [TestMethod]
        public void Export()
        {
            // Arrange
            var db = GetMemoryContext();
            var diaryService = new DiaryService(db);
            var diaryEntryService = new DiaryEntryService(db);
            var diaryArchiveService = new DiaryArchiveService(db);
            var controller = new DiaryArchiveController(diaryService, diaryEntryService, diaryArchiveService);

            var diary = diaryService.Create(new DiaryDTO()
            {
                Author = "TestAuthor",
                Title = "TestDiary",
                Description = "TestDescription",
            });

            diaryEntryService.CreateDiaryEntry(new DiaryEntryDTO()
            {
                Entry = "TestEntryA",
                Date = DateTime.Now,
                Location = "TestLocationA",
                DiaryId = diary.DiaryId,
            });

            diaryEntryService.CreateDiaryEntry(new DiaryEntryDTO()
            {
                Entry = "TestEntryB",
                Date = DateTime.Now,
                Location = "TestLocationB",
                DiaryId = diary.DiaryId,
            });

            // Act
            var response = controller.Export(diary.DiaryId);

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(diary.DiaryId, result.Diary.DiaryId);
            Assert.AreEqual(2, result.DiaryEntries.Count);
        }

        [TestMethod]
        public void ExportDiaryNotFound()
        {
            // Arrange
            var db = GetMemoryContext();
            var diaryService = new DiaryService(db);
            var diaryEntryService = new DiaryEntryService(db);
            var diaryArchiveService = new DiaryArchiveService(db);
            var controller = new DiaryArchiveController(diaryService, diaryEntryService, diaryArchiveService);

            // Act
            var response = controller.Export(Guid.NewGuid());

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(NotFoundResult));
        }
    }
}
