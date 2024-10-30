// <copyright file="DiaryControllerTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ccDiaryApi.Controllers.v1;
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
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
            var controller = new DiaryController(diaryService);

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
            var controller = new DiaryController(diaryService);

            // Act
            controller.Create(new DiaryDTO { Author = "Paul1", Title = "Paul's 1st Diary" });
            controller.Create(new DiaryDTO { Author = "Paul2", Title = "Paul's 2nd Diary" });
            controller.Create(new DiaryDTO { Author = "Paul3", Title = "Paul's 3rd Diary" });
            var response = controller.Get();

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count());
        }

        [TestMethod]
        public void GetSingle()
        {
            // Arrange
            var db = GetMemoryContext();
            var diaryService = new DiaryService(db);
            var controller = new DiaryController(diaryService);

            // Act
            controller.Create(new DiaryDTO { Author = "Paul", Title = "Paul's Diary" });
            var response = controller.Get();

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual("Paul's Diary", result.First().Title);
            Assert.AreNotEqual(Guid.Empty, result.First().DiaryId);
        }

        [TestMethod]
        public void GetSingleById()
        {
            // Arrange
            var db = GetMemoryContext();
            var diaryService = new DiaryService(db);
            var controller = new DiaryController(diaryService);

            // Act
            controller.Create(new DiaryDTO { Author = "Paul1", Title = "Paul's 1st Diary", Description = "Description of Paul's 1st Diary" });
            var createResponse = controller.Create(new DiaryDTO { Author = "Paul2", Title = "Paul's 2nd Diary", Description = "Description of Paul's 2nd Diary" });
            controller.Create(new DiaryDTO { Author = "Paul3", Title = "Paul's 3rd Diary", Description = "Description of Paul's 3rd Diary" });
            var createResult = createResponse.GetObjectResult();
            Assert.IsNotNull(createResult);
            var response = controller.Get(createResult.DiaryId);

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
            var controller = new DiaryController(diaryService);

            controller.Create(new DiaryDTO { Author = "Paul1", Title = "Paul's 1st Diary", Description = "Description of Paul's 1st Diary" });
            var createResponse = controller.Create(new DiaryDTO { Author = "Paul2", Title = "Paul's 2nd Diary", Description = "Description of Paul's 2nd Diary" });
            controller.Create(new DiaryDTO { Author = "Paul3", Title = "Paul's 3rd Diary", Description = "Description of Paul's 3rd Diary" });
            var createResult = createResponse.GetObjectResult();
            Assert.IsNotNull(createResult);
            var preGetResponse = controller.Get();
            var preGetResult = preGetResponse.GetObjectResult();
            Assert.IsNotNull(preGetResult);
            Assert.AreEqual(3, preGetResult.Count());

            // Act
            var response = controller.Delete(createResult.DiaryId);

            // Assert
            Assert.IsInstanceOfType(response, typeof(OkResult));
            var postGetResponse = controller.Get();
            var postGetResult = postGetResponse.GetObjectResult();
            Assert.IsNotNull(postGetResult);
            Assert.AreEqual(2, postGetResult.Count());
        }

        [TestMethod]
        public void DeleteNone()
        {
            // Arrange
            var db = GetMemoryContext();
            var diaryService = new DiaryService(db);
            var controller = new DiaryController(diaryService);

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
            var controller = new DiaryController(diaryService);

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
            var controller = new DiaryController(diaryService);

            // Act
            var response = controller.Get();

            // Assert
            Assert.IsInstanceOfType(response.Result, typeof(OkObjectResult));
            var result = response.GetObjectResult();
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }
    }
}
