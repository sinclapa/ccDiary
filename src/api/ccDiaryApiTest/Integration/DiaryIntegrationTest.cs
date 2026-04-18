// <copyright file="DiaryIntegrationTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Integration
{
    using System.Net;
    using System.Net.Http.Json;
    using ccDiaryApi.Data.Model;

    [TestClass]
    public class DiaryIntegrationTest
    {
        private HttpClient _httpClient = null!;

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

        [TestInitialize]
        public async Task TestInit()
        {
            _httpClient = SharedTestFactory.Factory.CreateDefaultClient();
            await SharedTestFactory.Factory.ClearDatabaseAsync();

            // Seed the default test user as admin so Create/Update/Delete are authorised
            await SharedTestFactory.Factory.CreateAppUserAsync(SharedTestFactory.Factory.DefaultUserId, AppRole.DiaryAdmin);
        }

        [TestMethod]
        public async Task GetNoItems()
        {
            // Act
            var response = await _httpClient.GetAsync("/api/v1/Diary/Get");

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
            await CreateDiary(_httpClient);
            await CreateDiary(_httpClient);
            await CreateDiary(_httpClient);

            // Act
            var response = await _httpClient.GetAsync("/api/v1/Diary/Get");

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
            var diary = await CreateDiary(_httpClient);

            // Act
            var response = await _httpClient.GetAsync($"/api/v1/Diary/Get/{diary.DiaryId}");

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
            var diary = await CreateDiary(_httpClient);

            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/Diary/Get/{diary.DiaryId}");
            request.Headers.Add("UserId", "testuser");
            var response = await _httpClient.SendAsync(request);

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
            // Act
            var response = await _httpClient.GetAsync($"/swagger/index.html");

            // Assert
            var result = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(result.IndexOf("swagger", StringComparison.InvariantCultureIgnoreCase) > 0);
        }

        [TestMethod]
        public async Task Create()
        {
            // Arrange
            DiaryDTO diary = new ()
            {
                Author = "ABCDEFGHIJABCDEFGHIJABCDEFGHIJABCDEFGHIJABCDEFGHIJ",
                Title = "12345678901234567890123456789012345678901234567890",
            };

            // Act
            var response = await _httpClient.PostAsJsonAsync("/api/v1/Diary/Create", diary);

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
            // Act
            var response = await _httpClient.PostAsJsonAsync<DiaryDTO?>("/api/v1/Diary/Create", null);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task CreateTooShortTitle()
        {
            // Arrange
            DiaryDTO diary = new ()
            {
                Author = "Paul",
                Title = "1234",
            };

            // Act
            var response = await _httpClient.PostAsJsonAsync("/api/v1/Diary/Create", diary);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task CreateTooLongTitle()
        {
            // Arrange
            DiaryDTO diary = new ()
            {
                Author = "Paul",
                Title = "123456789012345678901234567890123456789012345678901",
            };

            // Act
            var response = await _httpClient.PostAsJsonAsync("/api/v1/Diary/Create", diary);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task CreateTooLomgAuthor()
        {
            // Arrange
            DiaryDTO diary = new ()
            {
                Author = "123456789012345678901234567890123456789012345678901",
                Title = "Title",
            };

            // Act
            var response = await _httpClient.PostAsJsonAsync("/api/v1/Diary/Create", diary);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task UpdateNull()
        {
            // Act
            var response = await _httpClient.PutAsJsonAsync<DiaryDTO?>("/api/v1/Diary/Update", null);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Update()
        {
            // Arrange
            var diary = await CreateDiary(_httpClient);
            diary.Author = $"UpdatedAuthor{DateTime.UtcNow.Ticks}";
            diary.Title = $"UpdatedTitle{DateTime.UtcNow.Ticks}";

            // Act
            var response = await _httpClient.PutAsJsonAsync("/api/v1/Diary/Update", diary);

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
            var diary = await CreateDiary(_httpClient);

            // Act
            var response = await _httpClient.DeleteAsync($"/api/v1/Diary/Delete/{diary.DiaryId}");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task DeleteNotFound()
        {
            // Act
            var response = await _httpClient.DeleteAsync($"/api/v1/Diary/Delete/{Guid.NewGuid()}");

            // Assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Contributor_CannotEditAnotherUsersDiary()
        {
            // Arrange — owner creates a diary
            var ownerOid = "owner-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(ownerOid, AppRole.DiaryContributor);

            var ownerClient = SharedTestFactory.Factory.CreateClient();
            ownerClient.DefaultRequestHeaders.Add(TestAuthHandler.UserId, ownerOid);
            ownerClient.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryContributor");

            var diary = await CreateDiary(ownerClient);

            // Arrange — a different contributor tries to delete the diary
            var otherOid = "other-contributor-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(otherOid, AppRole.DiaryContributor);

            var otherClient = SharedTestFactory.Factory.CreateClient();
            otherClient.DefaultRequestHeaders.Add(TestAuthHandler.UserId, otherOid);
            otherClient.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryContributor");

            // Act
            var response = await otherClient.DeleteAsync($"/api/v1/Diary/Delete/{diary.DiaryId}");

            // Assert
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [TestMethod]
        public async Task Admin_CanEditAnyDiary()
        {
            // Arrange — contributor creates a diary
            var contributorOid = "contributor-for-admin-test";
            await SharedTestFactory.Factory.CreateAppUserAsync(contributorOid, AppRole.DiaryContributor);

            var contributorClient = SharedTestFactory.Factory.CreateClient();
            contributorClient.DefaultRequestHeaders.Add(TestAuthHandler.UserId, contributorOid);
            contributorClient.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryContributor");

            var diary = await CreateDiary(contributorClient);

            // Act — admin deletes it
            var response = await _httpClient.DeleteAsync($"/api/v1/Diary/Delete/{diary.DiaryId}");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
