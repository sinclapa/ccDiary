// <copyright file="DiaryArchiveControllerTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System;
    using System.Security.Claims;
    using ccDiaryApi.Controllers.v1;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Moq;

    [TestClass]
    public class DiaryArchiveControllerTest
    {
        private Mock<IDiaryArchiveService> _archiveService = null!;

        [TestInitialize]
        public void Init()
        {
            _archiveService = new Mock<IDiaryArchiveService>();
        }

        [TestMethod]
        public async Task Export()
        {
            // Arrange
            var diaryId = Guid.NewGuid();
            var archive = new DiaryArchiveDTO
            {
                Diary = new DiaryDTO
                {
                    DiaryId = diaryId,
                    Author = "TestAuthor",
                    Title = "TestDiary",
                    Description = "TestDescription",
                },
                DiaryEntries = new List<DiaryEntryDTO>
                {
                    new () { Entry = "TestEntryA", Date = DateTime.UtcNow, Location = "TestLocationA", DiaryId = diaryId },
                    new () { Entry = "TestEntryB", Date = DateTime.UtcNow, Location = "TestLocationB", DiaryId = diaryId },
                },
            };
            _archiveService.Setup(x => x.ExportAsync(diaryId)).ReturnsAsync(archive);
            var controller = new DiaryArchiveController(_archiveService.Object);

            // Act
            var response = await controller.Export(diaryId);

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(diaryId, result.Diary.DiaryId);
            Assert.AreEqual(2, result.DiaryEntries.Count);
        }

        [TestMethod]
        public async Task ExportDiaryNotFound()
        {
            // Arrange
            _archiveService.Setup(x => x.ExportAsync(It.IsAny<Guid>())).ReturnsAsync((DiaryArchiveDTO?)null);
            var controller = new DiaryArchiveController(_archiveService.Object);

            // Act
            var response = await controller.Export(Guid.NewGuid());

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Import_NonLocalEnvironment_Unauthenticated_ReturnsUnauthorized()
        {
            // Arrange
            var controller = CreateControllerWithAnonymousUser();
            var env = MockEnvironment("Production");

            // Act
            var result = await controller.Import(env, NewArchive());

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(UnauthorizedResult));
            _archiveService.Verify(x => x.ImportAsync(It.IsAny<DiaryArchiveDTO>()), Times.Never);
        }

        [TestMethod]
        public async Task Import_LocalEnvironment_Unauthenticated_ReturnsOk()
        {
            // Arrange
            var archive = NewArchive();
            _archiveService.Setup(x => x.ImportAsync(archive)).ReturnsAsync(archive.Diary);
            var controller = CreateControllerWithAnonymousUser();
            var env = MockEnvironment("local");

            // Act
            var result = await controller.Import(env, archive);

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        }

        [TestMethod]
        public async Task Import_LocalContainerEnvironment_Unauthenticated_ReturnsOk()
        {
            // Arrange — covers the env.IsEnvironment("LocalContainer") == true branch
            var archive = NewArchive();
            _archiveService.Setup(x => x.ImportAsync(archive)).ReturnsAsync(archive.Diary);
            var controller = CreateControllerWithAnonymousUser();
            var env = MockEnvironment("LocalContainer");

            // Act
            var result = await controller.Import(env, archive);

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        }

        private static IWebHostEnvironment MockEnvironment(string environmentName)
        {
            var env = new Mock<IWebHostEnvironment>();
            env.Setup(e => e.EnvironmentName).Returns(environmentName);
            return env.Object;
        }

        private static DiaryArchiveDTO NewArchive() => new ()
        {
            Diary = new DiaryDTO { Author = "Author", Title = "Title", Description = "Desc" },
            DiaryEntries = new List<DiaryEntryDTO>(),
        };

        private DiaryArchiveController CreateControllerWithAnonymousUser()
        {
            return new DiaryArchiveController(_archiveService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity()),
                    },
                },
            };
        }
    }
}
