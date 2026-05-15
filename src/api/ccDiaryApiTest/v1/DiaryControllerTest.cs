// <copyright file="DiaryControllerTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Text;
    using System.Threading.Tasks;
    using ccDiaryApi.Controllers.v1;
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.HttpResults;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    [TestClass]
    public class DiaryControllerTest
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
        public void Insert()
        {
            // Arrange
            var db = GetMemoryContext();
            var diaryService = new DiaryService(db);
            var controller = CreateController(diaryService);

            // Act
            var response = controller.Create(new DiaryDTO { Author = "Paul", Title = "Paul's Diary" });

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(CreatedResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual("Paul", result.Author);
            Assert.AreEqual("Paul's Diary", result.Title);
            Assert.AreNotEqual(Guid.Empty, result.DiaryId);
        }

        [TestMethod]
        public void GetMany()
        {
            // Arrange
            var db = GetMemoryContext();
            var diaryService = new DiaryService(db);
            var controller = CreateController(diaryService);

            // Act
            controller.Create(new DiaryDTO { Author = "Paul1", Title = "Paul's 1st Diary" });
            controller.Create(new DiaryDTO { Author = "Paul2", Title = "Paul's 2nd Diary" });
            controller.Create(new DiaryDTO { Author = "Paul3", Title = "Paul's 3rd Diary" });
            var response = controller.Get();

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.TotalCount);
            Assert.AreEqual(3, result.Items.Count());
        }

        [TestMethod]
        public void GetSingle()
        {
            // Arrange
            var db = GetMemoryContext();
            var diaryService = new DiaryService(db);
            var controller = CreateController(diaryService);

            // Act
            controller.Create(new DiaryDTO { Author = "Paul", Title = "Paul's Diary" });
            var response = controller.Get();

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.TotalCount);
            Assert.AreEqual(1, result.Items.Count());
            Assert.AreEqual("Paul's Diary", result.Items.First().Title);
            Assert.AreNotEqual(Guid.Empty, result.Items.First().DiaryId);
        }

        [TestMethod]
        public void GetSingleById()
        {
            // Arrange
            var db = GetMemoryContext();
            var diaryService = new DiaryService(db);
            var controller = CreateController(diaryService);

            // Act
            controller.Create(new DiaryDTO { Author = "Paul1", Title = "Paul's 1st Diary", Description = "Description of Paul's 1st Diary" });
            var createResponse = controller.Create(new DiaryDTO { Author = "Paul2", Title = "Paul's 2nd Diary", Description = "Description of Paul's 2nd Diary" });
            controller.Create(new DiaryDTO { Author = "Paul3", Title = "Paul's 3rd Diary", Description = "Description of Paul's 3rd Diary" });
            var createResult = createResponse.GetObjectResult();
            Assert.IsNotNull(createResult);
            var response = controller.Get(createResult.DiaryId!.Value);

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(createResult.DiaryId, result.DiaryId);
            Assert.AreEqual("Paul2", result.Author);
            Assert.AreEqual("Paul's 2nd Diary", result.Title);
            Assert.AreEqual("Description of Paul's 2nd Diary", result.Description);
        }

        [TestMethod]
        public void Delete()
        {
            // Arrange
            var db = GetMemoryContext();
            var diaryService = new DiaryService(db);
            var controller = CreateController(diaryService);

            controller.Create(new DiaryDTO { Author = "Paul1", Title = "Paul's 1st Diary", Description = "Description of Paul's 1st Diary" });
            var createResponse = controller.Create(new DiaryDTO { Author = "Paul2", Title = "Paul's 2nd Diary", Description = "Description of Paul's 2nd Diary" });
            controller.Create(new DiaryDTO { Author = "Paul3", Title = "Paul's 3rd Diary", Description = "Description of Paul's 3rd Diary" });
            var createResult = createResponse.GetObjectResult();
            Assert.IsNotNull(createResult);
            var preGetResponse = controller.Get();
            var preGetResult = preGetResponse.GetObjectResult();
            Assert.IsNotNull(preGetResult);
            Assert.AreEqual(3, preGetResult.TotalCount);

            // Act
            var response = controller.Delete(createResult.DiaryId!.Value);

            // Assert
            Assert.IsInstanceOfType(response, typeof(OkResult));
            var postGetResponse = controller.Get();
            var postGetResult = postGetResponse.GetObjectResult();
            Assert.IsNotNull(postGetResult);
            Assert.AreEqual(2, postGetResult.TotalCount);
        }

        [TestMethod]
        public void DeleteNone()
        {
            // Arrange
            var db = GetMemoryContext();
            var diaryService = new DiaryService(db);
            var controller = CreateController(diaryService);

            // Act
            var response = controller.Delete(Guid.NewGuid());

            // Assert
            Assert.IsInstanceOfType(response, typeof(NotFoundResult));
        }

        [TestMethod]
        public void Update()
        {
            // Arrange
            var db = GetMemoryContext();
            var diaryService = new DiaryService(db);
            var controller = CreateController(diaryService);

            var createResponse = controller.Create(new DiaryDTO { Author = "Paul2", Title = "Paul's 2nd Diary", Description = "Description of Paul's 2nd Diary" });
            var createResult = createResponse.GetObjectResult();
            Assert.IsNotNull(createResult);

            // Act
            createResult.Author = "John";
            createResult.Title = "John's Diary";
            createResult.Description = "Description of John's Diary";
            var response = controller.Update(createResult);

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(createResult.DiaryId, result.DiaryId);
            Assert.AreEqual("John", result.Author);
            Assert.AreEqual("John's Diary", result.Title);
            Assert.AreEqual("Description of John's Diary", result.Description);
        }

        [TestMethod]
        public void GetNone()
        {
            // Arrange
            var db = GetMemoryContext();
            var diaryService = new DiaryService(db);
            var controller = CreateController(diaryService);

            // Act
            var response = controller.Get();

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.TotalCount);
            Assert.AreEqual(0, result.Items.Count());
        }

        [TestMethod]
        public void GetPaged_ReturnsCorrectPage()
        {
            // Arrange
            var db = GetMemoryContext();
            var diaryService = new DiaryService(db);
            var controller = CreateController(diaryService);

            for (int i = 1; i <= 15; i++)
            {
                controller.Create(new DiaryDTO { Author = $"Author{i:D2}", Title = $"Diary{i:D2}" });
            }

            // Act — page 2 with page size 5
            var response = controller.Get(page: 2, pageSize: 5);

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(15, result.TotalCount);
            Assert.AreEqual(5, result.Items.Count());
            Assert.AreEqual(2, result.Page);
            Assert.AreEqual(5, result.PageSize);
        }

        [TestMethod]
        public void Update_AsNonOwner_ReturnsForbid()
        {
            var db = GetMemoryContext();
            var diaryService = new DiaryService(db);

            var ownerController = CreateController(diaryService, oid: "owner-oid");
            var diary = ownerController.Create(new DiaryDTO { Author = "Owner", Title = "Owner's Diary" }).GetObjectResult();
            Assert.IsNotNull(diary);

            var otherController = CreateController(diaryService, oid: "other-oid");
            diary.Title = "Hijacked";
            var response = otherController.Update(diary);

            Assert.IsInstanceOfType(response.Result, typeof(ForbidResult));
        }

        [TestMethod]
        public void Update_WithMissingDiary_ReturnsForbid()
        {
            var db = GetMemoryContext();
            var diaryService = new DiaryService(db);
            var controller = CreateController(diaryService, oid: "user-oid");

            var response = controller.Update(new DiaryDTO { DiaryId = Guid.NewGuid(), Author = "Ghost", Title = "Ghost Diary" });

            Assert.IsInstanceOfType(response.Result, typeof(ForbidResult));
        }

        [TestMethod]
        public void Update_AsAdmin_ReturnsOk()
        {
            var db = GetMemoryContext();
            var diaryService = new DiaryService(db);

            var ownerController = CreateController(diaryService, oid: "owner-oid");
            var diary = ownerController.Create(new DiaryDTO { Author = "Owner", Title = "Owner's Diary" }).GetObjectResult();
            Assert.IsNotNull(diary);

            var adminController = CreateController(diaryService, oid: "admin-oid", isAdmin: true);
            diary.Title = "Admin Updated";
            var response = adminController.Update(diary);

            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
        }

        [TestMethod]
        public void Delete_AsNonOwner_ReturnsForbid()
        {
            var db = GetMemoryContext();
            var diaryService = new DiaryService(db);

            var ownerController = CreateController(diaryService, oid: "owner-oid");
            var diary = ownerController.Create(new DiaryDTO { Author = "Owner", Title = "Owner's Diary" }).GetObjectResult();
            Assert.IsNotNull(diary);

            var otherController = CreateController(diaryService, oid: "other-oid");
            var response = otherController.Delete(diary.DiaryId!.Value);

            Assert.IsInstanceOfType(response, typeof(ForbidResult));
        }

        [TestMethod]
        public void Delete_AsAdmin_ReturnsOk()
        {
            var db = GetMemoryContext();
            var diaryService = new DiaryService(db);

            var ownerController = CreateController(diaryService, oid: "owner-oid");
            var diary = ownerController.Create(new DiaryDTO { Author = "Owner", Title = "Owner's Diary" }).GetObjectResult();
            Assert.IsNotNull(diary);

            var adminController = CreateController(diaryService, oid: "admin-oid", isAdmin: true);
            var response = adminController.Delete(diary.DiaryId!.Value);

            Assert.IsInstanceOfType(response, typeof(OkResult));
        }

        private static DiaryController CreateController(IDiaryService service, string? oid = null, bool isAdmin = false)
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

            var controller = new DiaryController(service);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, claims.Count > 0 ? "Test" : string.Empty)),
                },
            };
            return controller;
        }
    }
}
