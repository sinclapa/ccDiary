// <copyright file="DiaryControllerTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using ccDiaryApi.Controllers;
    using ccDiaryApi.Controllers.v1;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Moq;

    /// <summary>
    /// Controller-level tests: routing of results, authorisation branches and query
    /// parameter clamping. The query semantics of <see cref="DiaryService"/> itself
    /// are covered by the integration tests, which run it end to end over HTTP.
    /// </summary>
    [TestClass]
    public class DiaryControllerTest
    {
        private Mock<IDiaryService> _diaryService = null!;

        [TestInitialize]
        public void Init()
        {
            _diaryService = new Mock<IDiaryService>();
        }

        [TestMethod]
        public async Task Insert()
        {
            // Arrange
            _diaryService.Setup(x => x.CreateAsync(It.IsAny<DiaryDTO>()))
                .ReturnsAsync((DiaryDTO d) =>
                {
                    d.DiaryId = Guid.NewGuid();
                    return d;
                });
            var controller = CreateController();

            // Act
            var response = await controller.Create(new DiaryDTO { Author = "Paul", Title = "Paul's Diary" });

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(CreatedResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual("Paul", result.Author);
            Assert.AreEqual("Paul's Diary", result.Title);
            Assert.AreNotEqual(Guid.Empty, result.DiaryId);
        }

        [TestMethod]
        public async Task Create_StampsOwnerIdFromCallersOid()
        {
            // Arrange
            DiaryDTO? captured = null;
            _diaryService.Setup(x => x.CreateAsync(It.IsAny<DiaryDTO>()))
                .Callback<DiaryDTO>(d => captured = d)
                .ReturnsAsync((DiaryDTO d) => d);
            var controller = CreateController(oid: "owner-oid");

            // Act
            await controller.Create(new DiaryDTO { Author = "Paul", Title = "Paul's Diary" });

            // Assert
            Assert.IsNotNull(captured);
            Assert.AreEqual("owner-oid", captured.OwnerId);
        }

        [TestMethod]
        public async Task GetMany()
        {
            // Arrange
            var page = NewPage(
                new DiaryDTO { DiaryId = Guid.NewGuid(), Author = "Paul1", Title = "Paul's 1st Diary" },
                new DiaryDTO { DiaryId = Guid.NewGuid(), Author = "Paul2", Title = "Paul's 2nd Diary" },
                new DiaryDTO { DiaryId = Guid.NewGuid(), Author = "Paul3", Title = "Paul's 3rd Diary" });
            _diaryService.Setup(x => x.GetDiariesAsync(1, 12, null)).ReturnsAsync(page);
            var controller = CreateController();

            // Act
            var response = await controller.Get();

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.TotalCount);
            Assert.AreEqual(3, result.Items.Count());
        }

        [TestMethod]
        public async Task GetNone()
        {
            // Arrange
            _diaryService.Setup(x => x.GetDiariesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()))
                .ReturnsAsync(NewPage());
            var controller = CreateController();

            // Act
            var response = await controller.Get();

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.TotalCount);
            Assert.AreEqual(0, result.Items.Count());
        }

        [TestMethod]
        public async Task GetSingleById()
        {
            // Arrange
            var diaryId = Guid.NewGuid();
            var diary = new DiaryDTO
            {
                DiaryId = diaryId,
                Author = "Paul2",
                Title = "Paul's 2nd Diary",
                Description = "Description of Paul's 2nd Diary",
            };
            _diaryService.Setup(x => x.GetDiaryAsync(diaryId)).ReturnsAsync(diary);
            var controller = CreateController();

            // Act
            var response = await controller.Get(diaryId);

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(diaryId, result.DiaryId);
            Assert.AreEqual("Paul2", result.Author);
            Assert.AreEqual("Paul's 2nd Diary", result.Title);
            Assert.AreEqual("Description of Paul's 2nd Diary", result.Description);
        }

        [TestMethod]
        public async Task Get_PassesSearchTermThrough()
        {
            // Arrange
            _diaryService.Setup(x => x.GetDiariesAsync(1, 12, "World War")).ReturnsAsync(NewPage());
            var controller = CreateController();

            // Act
            await controller.Get(search: "World War");

            // Assert
            _diaryService.Verify(x => x.GetDiariesAsync(1, 12, "World War"), Times.Once);
        }

        [TestMethod]
        public async Task Get_ClampsPageSizeToMaximum()
        {
            // Arrange
            _diaryService.Setup(x => x.GetDiariesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()))
                .ReturnsAsync(NewPage());
            var controller = CreateController();

            // Act
            await controller.Get(page: 1, pageSize: 5000);

            // Assert
            _diaryService.Verify(x => x.GetDiariesAsync(1, PagingLimits.MaxPageSize, null), Times.Once);
        }

        [TestMethod]
        public async Task Get_ClampsNonPositivePageAndPageSize()
        {
            // Arrange
            _diaryService.Setup(x => x.GetDiariesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()))
                .ReturnsAsync(NewPage());
            var controller = CreateController();

            // Act
            await controller.Get(page: 0, pageSize: 0);

            // Assert
            _diaryService.Verify(x => x.GetDiariesAsync(1, 1, null), Times.Once);
        }

        [TestMethod]
        public async Task GetPaged_PassesPagingThrough()
        {
            // Arrange
            var page = NewPage(new DiaryDTO { DiaryId = Guid.NewGuid(), Author = "Author01", Title = "Diary01" });
            page.TotalCount = 15;
            page.Page = 2;
            page.PageSize = 5;
            _diaryService.Setup(x => x.GetDiariesAsync(2, 5, null)).ReturnsAsync(page);
            var controller = CreateController();

            // Act
            var response = await controller.Get(page: 2, pageSize: 5);

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(15, result.TotalCount);
            Assert.AreEqual(2, result.Page);
            Assert.AreEqual(5, result.PageSize);
        }

        [TestMethod]
        public async Task Delete()
        {
            // Arrange
            var diaryId = Guid.NewGuid();
            var diary = new DiaryDTO { DiaryId = diaryId, Author = "Paul", Title = "Paul's Diary", OwnerId = "owner-oid" };
            _diaryService.Setup(x => x.GetDiaryAsync(diaryId)).ReturnsAsync(diary);
            var controller = CreateController(oid: "owner-oid");

            // Act
            var response = await controller.Delete(diaryId);

            // Assert
            Assert.IsInstanceOfType(response, typeof(OkResult));
            _diaryService.Verify(x => x.DeleteAsync(diary), Times.Once);
        }

        [TestMethod]
        public async Task DeleteNone()
        {
            // Arrange
            _diaryService.Setup(x => x.GetDiaryAsync(It.IsAny<Guid>())).ReturnsAsync((DiaryDTO?)null);
            var controller = CreateController();

            // Act
            var response = await controller.Delete(Guid.NewGuid());

            // Assert
            Assert.IsInstanceOfType(response, typeof(NotFoundResult));
            _diaryService.Verify(x => x.DeleteAsync(It.IsAny<DiaryDTO>()), Times.Never);
        }

        [TestMethod]
        public async Task Update()
        {
            // Arrange
            var diaryId = Guid.NewGuid();
            var updated = new DiaryDTO
            {
                DiaryId = diaryId,
                Author = "John",
                Title = "John's Diary",
                Description = "Description of John's Diary",
                OwnerId = "owner-oid",
            };
            _diaryService.Setup(x => x.GetDiaryAsync(diaryId)).ReturnsAsync(updated);
            _diaryService.Setup(x => x.UpdateAsync(It.IsAny<DiaryDTO>())).ReturnsAsync((DiaryDTO d) => d);
            var controller = CreateController(oid: "owner-oid");

            // Act
            var response = await controller.Update(updated);

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(diaryId, result.DiaryId);
            Assert.AreEqual("John", result.Author);
            Assert.AreEqual("John's Diary", result.Title);
            Assert.AreEqual("Description of John's Diary", result.Description);
        }

        [TestMethod]
        public async Task Update_AsNonOwner_ReturnsForbid()
        {
            // Arrange
            var diaryId = Guid.NewGuid();
            var diary = new DiaryDTO { DiaryId = diaryId, Author = "Owner", Title = "Owner's Diary", OwnerId = "owner-oid" };
            _diaryService.Setup(x => x.GetDiaryAsync(diaryId)).ReturnsAsync(diary);
            var controller = CreateController(oid: "other-oid");

            // Act
            var response = await controller.Update(new DiaryDTO { DiaryId = diaryId, Author = "Owner", Title = "Hijacked" });

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(ForbidResult));
            _diaryService.Verify(x => x.UpdateAsync(It.IsAny<DiaryDTO>()), Times.Never);
        }

        [TestMethod]
        public async Task Update_WithMissingDiary_ReturnsForbid()
        {
            // Arrange
            _diaryService.Setup(x => x.GetDiaryAsync(It.IsAny<Guid>())).ReturnsAsync((DiaryDTO?)null);
            var controller = CreateController(oid: "user-oid");

            // Act
            var response = await controller.Update(new DiaryDTO { DiaryId = Guid.NewGuid(), Author = "Ghost", Title = "Ghost Diary" });

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(ForbidResult));
        }

        [TestMethod]
        public async Task Update_AsAdmin_ReturnsOk()
        {
            // Arrange — an admin never triggers the ownership lookup
            var diary = new DiaryDTO { DiaryId = Guid.NewGuid(), Author = "Owner", Title = "Admin Updated", OwnerId = "owner-oid" };
            _diaryService.Setup(x => x.UpdateAsync(It.IsAny<DiaryDTO>())).ReturnsAsync((DiaryDTO d) => d);
            var controller = CreateController(oid: "admin-oid", isAdmin: true);

            // Act
            var response = await controller.Update(diary);

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            _diaryService.Verify(x => x.GetDiaryAsync(It.IsAny<Guid>()), Times.Never);
        }

        [TestMethod]
        public async Task Delete_AsNonOwner_ReturnsForbid()
        {
            // Arrange
            var diaryId = Guid.NewGuid();
            var diary = new DiaryDTO { DiaryId = diaryId, Author = "Owner", Title = "Owner's Diary", OwnerId = "owner-oid" };
            _diaryService.Setup(x => x.GetDiaryAsync(diaryId)).ReturnsAsync(diary);
            var controller = CreateController(oid: "other-oid");

            // Act
            var response = await controller.Delete(diaryId);

            // Assert
            Assert.IsInstanceOfType(response, typeof(ForbidResult));
            _diaryService.Verify(x => x.DeleteAsync(It.IsAny<DiaryDTO>()), Times.Never);
        }

        [TestMethod]
        public async Task Delete_AsAdmin_ReturnsOk()
        {
            // Arrange
            var diaryId = Guid.NewGuid();
            var diary = new DiaryDTO { DiaryId = diaryId, Author = "Owner", Title = "Owner's Diary", OwnerId = "owner-oid" };
            _diaryService.Setup(x => x.GetDiaryAsync(diaryId)).ReturnsAsync(diary);
            var controller = CreateController(oid: "admin-oid", isAdmin: true);

            // Act
            var response = await controller.Delete(diaryId);

            // Assert
            Assert.IsInstanceOfType(response, typeof(OkResult));
            _diaryService.Verify(x => x.DeleteAsync(diary), Times.Once);
        }

        private static PagedResultDTO<DiaryDTO> NewPage(params DiaryDTO[] items) => new ()
        {
            Items = items.ToList(),
            TotalCount = items.Length,
            Page = 1,
            PageSize = 12,
        };

        private DiaryController CreateController(string? oid = null, bool isAdmin = false)
        {
            var claims = new List<Claim>();
            if (oid != null)
            {
                claims.Add(new Claim("oid", oid));
            }

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "DiaryAdmin"));
            }

            return new DiaryController(_diaryService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(claims, claims.Count > 0 ? "Test" : string.Empty)),
                    },
                },
            };
        }
    }
}
