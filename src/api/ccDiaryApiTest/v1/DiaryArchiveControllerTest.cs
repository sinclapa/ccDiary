// <copyright file="DiaryArchiveControllerTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System;
    using System.Security.Claims;
    using ccDiaryApi.Controllers.v1;
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Moq;

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
            var controller = new DiaryArchiveController(diaryArchiveService);

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
                DiaryId = diary.DiaryId!.Value,
            });

            diaryEntryService.CreateDiaryEntry(new DiaryEntryDTO()
            {
                Entry = "TestEntryB",
                Date = DateTime.Now,
                Location = "TestLocationB",
                DiaryId = diary.DiaryId!.Value,
            });

            // Act
            var response = controller.Export(diary.DiaryId!.Value);

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
            var diaryArchiveService = new DiaryArchiveService(db);
            var controller = new DiaryArchiveController(diaryArchiveService);

            // Act
            var response = controller.Export(Guid.NewGuid());

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(NotFoundResult));
        }

        [TestMethod]
        public void Import_NonLocalEnvironment_Unauthenticated_ReturnsUnauthorized()
        {
            // Arrange
            var db = GetMemoryContext();
            var controller = new DiaryArchiveController(new DiaryArchiveService(db));
            var env = new Mock<IWebHostEnvironment>();
            env.Setup(e => e.EnvironmentName).Returns("Production");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()),
                },
            };

            // Act
            var archive = new DiaryArchiveDTO
            {
                Diary = new DiaryDTO { Author = "A", Title = "T", Description = "D" },
                DiaryEntries = new List<DiaryEntryDTO>(),
            };
            var result = controller.Import(env.Object, archive);

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(UnauthorizedResult));
        }

        [TestMethod]
        public void Import_LocalEnvironment_Unauthenticated_ReturnsOk()
        {
            // Arrange
            var db = GetMemoryContext();
            var controller = new DiaryArchiveController(new DiaryArchiveService(db));
            var env = new Mock<IWebHostEnvironment>();
            env.Setup(e => e.EnvironmentName).Returns("local");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()),
                },
            };
            var archive = new DiaryArchiveDTO
            {
                Diary = new DiaryDTO { Author = "Author", Title = "Title", Description = "Desc" },
                DiaryEntries = new List<DiaryEntryDTO>(),
            };

            // Act
            var result = controller.Import(env.Object, archive);

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        }

        [TestMethod]
        public void Import_LocalContainerEnvironment_Unauthenticated_ReturnsOk()
        {
            // Arrange — covers the env.IsEnvironment("LocalContainer") == true branch
            var db = GetMemoryContext();
            var controller = new DiaryArchiveController(new DiaryArchiveService(db));
            var env = new Mock<IWebHostEnvironment>();
            env.Setup(e => e.EnvironmentName).Returns("LocalContainer");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()),
                },
            };
            var archive = new DiaryArchiveDTO
            {
                Diary = new DiaryDTO { Author = "Author", Title = "Title", Description = "Desc" },
                DiaryEntries = new List<DiaryEntryDTO>(),
            };

            // Act
            var result = controller.Import(env.Object, archive);

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        }
    }
}
