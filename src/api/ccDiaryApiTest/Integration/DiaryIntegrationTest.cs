// <copyright file="DiaryIntegrationTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Integration
{
    using System.Net;
    using System.Net.Http.Json;
    using ccDiaryApi.Data.Model;
    using Microsoft.AspNetCore.Http;

    [TestClass]
    public class DiaryIntegrationTest
    {
        public static async Task<DiaryDTO> CreateDiary(HttpClient httpClient)
        {
            DiaryDTO diary = new ()
            {
                Author = $"Author{DateTime.UtcNow.Ticks}",
                Title = $"Title{DateTime.UtcNow.Ticks}",
            };
            return await CreateDiary(httpClient, diary);
        }

        public static async Task<DiaryDTO> CreateDiary(HttpClient httpClient, DiaryDTO diary)
        {
            var diaryResponse = await httpClient.PostAsJsonAsync("/api/v1/Diary/Create", diary);
            var diaryResult = await diaryResponse.Content.ReadFromJsonAsync<DiaryDTO>();
            Assert.IsNotNull(diaryResult);
            return diaryResult;
        }

        [TestMethod]
        public async Task GetNoItems()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();

            // Act
            var response = await httpClient.GetAsync("/api/v1/Diary/Get");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<IEnumerable<DiaryDTO>>();
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public async Task Get()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();
            await CreateDiary(httpClient);
            await CreateDiary(httpClient);
            await CreateDiary(httpClient);

            // Act
            var response = await httpClient.GetAsync("/api/v1/Diary/Get");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<IEnumerable<DiaryDTO>>();
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count());
        }

        [TestMethod]
        public async Task GetById()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();
            var diary = await CreateDiary(httpClient);

            // Act
            var response = await httpClient.GetAsync($"/api/v1/Diary/Get/{diary.DiaryId}");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DiaryDTO>();
            Assert.IsNotNull(result);
            Assert.AreEqual(diary.Author, result.Author);
            Assert.AreEqual(diary.Title, result.Title);
            Assert.AreEqual(diary.DiaryId, result.DiaryId);
        }

        [TestMethod]
        public async Task GetByIdForUser()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();
            var diary = await CreateDiary(httpClient);

            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/Diary/Get/{diary.DiaryId}");
            request.Headers.Add("UserId", "testuser");
            var response = await httpClient.SendAsync(request);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DiaryDTO>();
            Assert.IsNotNull(result);
            Assert.AreEqual(diary.Author, result.Author);
            Assert.AreEqual(diary.Title, result.Title);
            Assert.AreEqual(diary.DiaryId, result.DiaryId);
        }

        [TestMethod]
        public async Task LoadSwagger()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();

            // Act
            var response = await httpClient.GetAsync($"/swagger/index.html");

            // Assert
            var result = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(result.IndexOf("swagger", StringComparison.InvariantCultureIgnoreCase) > 0);
        }

        [TestMethod]
        public async Task Create()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();

            DiaryDTO diary = new ()
            {
                Author = "ABCDEFGHIJABCDEFGHIJABCDEFGHIJABCDEFGHIJABCDEFGHIJ",
                Title = "12345678901234567890123456789012345678901234567890",
            };

            // Act
            var response = await httpClient.PostAsJsonAsync("/api/v1/Diary/Create", diary);

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DiaryDTO>();
            Assert.IsNotNull(result);
            Assert.AreEqual(diary.Author, result.Author);
            Assert.AreEqual(diary.Title, result.Title);
            Assert.AreNotEqual(Guid.Empty, result.DiaryId);
        }

        [TestMethod]
        public async Task CreateNull()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();

            // Act
            var response = await httpClient.PostAsJsonAsync<DiaryDTO?>("/api/v1/Diary/Create", null);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task CreateTooShortTitle()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();

            DiaryDTO diary = new ()
            {
                Author = "Paul",
                Title = "1234",
            };

            // Act
            var response = await httpClient.PostAsJsonAsync("/api/v1/Diary/Create", diary);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task CreateTooLongTitle()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();

            DiaryDTO diary = new ()
            {
                Author = "Paul",
                Title = "123456789012345678901234567890123456789012345678901",
            };

            // Act
            var response = await httpClient.PostAsJsonAsync("/api/v1/Diary/Create", diary);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task CreateTooLomgAuthor()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();

            DiaryDTO diary = new ()
            {
                Author = "123456789012345678901234567890123456789012345678901",
                Title = "Title",
            };

            // Act
            var response = await httpClient.PostAsJsonAsync("/api/v1/Diary/Create", diary);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task UpdateNull()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();

            // Act
            var response = await httpClient.PutAsJsonAsync<DiaryDTO?>("/api/v1/Diary/Update", null);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Update()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();

            var diary = await CreateDiary(httpClient);
            diary.Author = $"UpdatedAuthor{DateTime.UtcNow.Ticks}";
            diary.Title = $"UpdatedTitle{DateTime.UtcNow.Ticks}";

            // Act
            var response = await httpClient.PutAsJsonAsync("/api/v1/Diary/Update", diary);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DiaryDTO>();
            Assert.IsNotNull(result);
            Assert.AreEqual(diary.Title, result.Title);
            Assert.AreEqual(diary.Author, result.Author);
            Assert.AreEqual(diary.DiaryId, result.DiaryId);
        }

        [TestMethod]
        public async Task Delete()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();

            var diary = await CreateDiary(httpClient);

            // Act
            var response = await httpClient.DeleteAsync($"/api/v1/Diary/Delete/{diary.DiaryId}");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task DeleteNotFound()
        {
            // Arrange
            var webAppFactory = new CustomWebApplicationFactory<Program>();
            var httpClient = webAppFactory.CreateDefaultClient();

            // Act
            var response = await httpClient.DeleteAsync($"/api/v1/Diary/Delete/{Guid.NewGuid()}");

            // Assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
