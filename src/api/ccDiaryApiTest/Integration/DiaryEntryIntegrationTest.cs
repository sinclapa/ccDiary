using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ccDiaryApi.Data.Model;

namespace ccDiaryApiTest.Integration
{
    [TestClass]
    public class DiaryEntryIntegrationTest
    {
        private static HttpClient _httpClient = new CustomWebApplicationFactory<Program>().CreateDefaultClient();

        public async Task<DiaryDTO> CreateDiary()
        {
            var diary = new DiaryDTO
            {
                Author = $"Author{DateTime.UtcNow.Ticks}",
                Title = $"Title{DateTime.UtcNow.Ticks}"
            };

            var response = await _httpClient.PostAsJsonAsync("/api/v1/Diary/Create", diary);
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DiaryDTO>();
            Assert.IsNotNull(result);
            return result;
        }

        public async Task<DiaryEntryDTO> CreateDiaryEntry(DiaryEntryDTO diaryEntry)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/DiaryEntry/Create", diaryEntry);
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DiaryEntryDTO>();
            Assert.IsNotNull(result);
            return result;
        }

        public async Task<DiaryEntryDTO> CreateDiaryEntry(Guid diaryId, DateTime date)
        {
            // Act
            var diaryEntry = new DiaryEntryDTO
            {
                Date = date,
                DiaryId = diaryId,
                Location = $"Location{DateTime.UtcNow.Ticks}",
                Entry = $"Notes{DateTime.UtcNow.Ticks}"
            };
            return await CreateDiaryEntry(diaryEntry);
        }

        public async Task<DiaryEntryDTO> CreateDiaryEntry(Guid diaryId)
        {
            return await CreateDiaryEntry(diaryId, DateTime.UtcNow);
        }


        [TestMethod]
        public async Task Get()
        {
            // Arrange
            var diary = await CreateDiary();
            var diaryEntry = await CreateDiaryEntry(diary.DiaryId);

            // Act
            var response = await _httpClient.GetAsync($"/api/v1/DiaryEntry/Get/{diaryEntry.DiaryEntryId}");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DiaryEntryDTO>();
            Assert.IsNotNull(result);
            Assert.AreEqual(diaryEntry.DiaryEntryId, result.DiaryEntryId);
            Assert.AreEqual(diaryEntry.Location, result.Location);
            Assert.AreEqual(diaryEntry.Entry, result.Entry);
            Assert.AreEqual(diaryEntry.DiaryId, result.DiaryId);
        }

        [TestMethod]
        public async Task GetDiaryEntries()
        {
            // Arrange
            var diary = await CreateDiary();
            var diaryEntry2 = await CreateDiaryEntry(diary.DiaryId, new DateTime(2020, 8, 26, 14, 0, 0));
            var diaryEntry1 = await CreateDiaryEntry(diary.DiaryId, new DateTime(2019, 3, 17, 14, 0, 0));
            var diaryEntry3 = await CreateDiaryEntry(diary.DiaryId, new DateTime(2021, 9, 13, 14, 0, 0));

            // Act
            var response = await _httpClient.GetAsync($"api/v1/DiaryEntry/GetDiaryEntries/{diary.DiaryId}");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<IEnumerable<DiaryEntryDTO>>();
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count());
            Assert.AreEqual(diaryEntry1.DiaryEntryId, result.ElementAt(0).DiaryEntryId);
            Assert.AreEqual(diaryEntry2.DiaryEntryId, result.ElementAt(1).DiaryEntryId);
            Assert.AreEqual(diaryEntry3.DiaryEntryId, result.ElementAt(2).DiaryEntryId);
        }

        [TestMethod]
        public async Task Create()
        {
            // Arrange
            var diary = await CreateDiary();

            // Act
            var diaryEntry = new DiaryEntryDTO
            {
                Date = new DateTime(2020, 6, 17, 14, 0, 0),
                DiaryId = diary.DiaryId,
                Location = $"Location{DateTime.UtcNow.Ticks}",
                Entry = $"Notes{DateTime.UtcNow.Ticks}"
            };
            var response = await _httpClient.PostAsJsonAsync("/api/v1/DiaryEntry/Create", diaryEntry);

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DiaryEntryDTO>();

            Assert.IsNotNull(result);
            Assert.AreNotEqual(Guid.Empty, result.DiaryEntryId);
            Assert.AreEqual(diaryEntry.Location, result.Location);
            Assert.AreEqual(diaryEntry.Entry, result.Entry);
            Assert.AreEqual(diary.DiaryId, result.DiaryId);
        }

        [TestMethod]
        public async Task Create_FailNonExistentDiary()
        {
            // Act
            var diaryEntry = new DiaryEntryDTO
            {
                Date = new DateTime(2020, 6, 17, 14, 0, 0),
                DiaryId = Guid.NewGuid(),
                Location = $"Location{DateTime.UtcNow.Ticks}",
                Entry = $"Notes{DateTime.UtcNow.Ticks}"
            };
            var response = await _httpClient.PostAsJsonAsync("/api/v1/DiaryEntry/Create", diaryEntry);

            // Assert
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [TestMethod]
        public async Task Create_MissingDate()
        {
            // Arrange
            var diary = await CreateDiary();

            // Act
            var diaryEntry = new DiaryEntryDTO
            {
                DiaryId = diary.DiaryId,
                Location = $"Location{DateTime.UtcNow.Ticks}",
                Entry = $"Notes{DateTime.UtcNow.Ticks}"
            };

            var response = await _httpClient.PostAsJsonAsync("/api/v1/DiaryEntry/Create", diaryEntry);

            // Assert
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [TestMethod]
        public async Task Create_MissingLocation()
        {
            // Arrange
            var diary = await CreateDiary();

            // Act
            var diaryEntry = new DiaryEntryDTO
            {
                Date = new DateTime(2020, 6, 17, 14, 0, 0),
                DiaryId = diary.DiaryId,
                Entry = $"Notes{DateTime.UtcNow.Ticks}"
            };

            var response = await _httpClient.PostAsJsonAsync("/api/v1/DiaryEntry/Create", diaryEntry);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Create_MissingNotes()
        {
            // Arrange
            var diary = await CreateDiary();

            // Act
            var diaryEntry = new DiaryEntryDTO
            {
                Date = new DateTime(2020, 6, 17, 14, 0, 0),
                DiaryId = diary.DiaryId,
                Location = $"Location{DateTime.UtcNow.Ticks}"
            };

            var response = await _httpClient.PostAsJsonAsync("/api/v1/DiaryEntry/Create", diaryEntry);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Search_NonExistentDiary()
        {
            // Act
            var response = await _httpClient.GetAsync($"api/v1/DiaryEntry/Search/{Guid.NewGuid()}");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<IEnumerable<int>>();
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public async Task Search()
        {
            // Arrange
            var diary = await CreateDiary();
            await CreateDiaryEntry(diary.DiaryId, new DateTime(2020, 8, 26, 14, 0, 0));
            await CreateDiaryEntry(diary.DiaryId, new DateTime(2019, 3, 17, 14, 0, 0));
            await CreateDiaryEntry(diary.DiaryId, new DateTime(2021, 9, 13, 14, 0, 0));

            // Act
            var response = await _httpClient.GetAsync($"api/v1/DiaryEntry/Search/{diary.DiaryId}");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<IEnumerable<int>>();
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count());
            Assert.AreEqual(2019, result.ElementAt(0));
            Assert.AreEqual(2020, result.ElementAt(1));
            Assert.AreEqual(2021, result.ElementAt(2));
        }

        [TestMethod]
        public async Task SearchYear()
        {
            // Arrange
            var diary = await CreateDiary();
            await CreateDiaryEntry(diary.DiaryId, new DateTime(2020, 7, 26, 14, 0, 0));
            await CreateDiaryEntry(diary.DiaryId, new DateTime(2020, 6, 17, 14, 0, 0));
            await CreateDiaryEntry(diary.DiaryId, new DateTime(2020, 6, 14, 14, 0, 0));
            await CreateDiaryEntry(diary.DiaryId, new DateTime(2020, 8, 17, 14, 0, 0));
            await CreateDiaryEntry(diary.DiaryId, new DateTime(2020, 12, 13, 14, 0, 0));

            // Act
            var response = await _httpClient.GetAsync($"api/v1/DiaryEntry/Search/{diary.DiaryId}/2020");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<IEnumerable<int>>();
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Count());
            Assert.AreEqual(6, result.ElementAt(0));
            Assert.AreEqual(7, result.ElementAt(1));
            Assert.AreEqual(8, result.ElementAt(2));
            Assert.AreEqual(12, result.ElementAt(3));
        }

        [TestMethod]
        public async Task SearchMonth()
        {
            // Arrange
            var diary = await CreateDiary();
            await CreateDiaryEntry(diary.DiaryId, new DateTime(2020, 6, 26, 14, 0, 0));
            await CreateDiaryEntry(diary.DiaryId, new DateTime(2020, 6, 17, 14, 0, 0));
            await CreateDiaryEntry(diary.DiaryId, new DateTime(2020, 6, 14, 14, 0, 0));
            await CreateDiaryEntry(diary.DiaryId, new DateTime(2020, 6, 17, 14, 0, 0));
            await CreateDiaryEntry(diary.DiaryId, new DateTime(2020, 6, 13, 14, 0, 0));

            // Act
            var response = await _httpClient.GetAsync($"api/v1/DiaryEntry/Search/{diary.DiaryId}/2020/6");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<IEnumerable<int>>();
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Count());
            Assert.AreEqual(13, result.ElementAt(0));
            Assert.AreEqual(14, result.ElementAt(1));
            Assert.AreEqual(17, result.ElementAt(2));
            Assert.AreEqual(26, result.ElementAt(3));
        }

        [TestMethod]
        public async Task SearchMonth_YearEnd()
        {
            // Arrange
            var diary = await CreateDiary();
            await CreateDiaryEntry(diary.DiaryId, new DateTime(2022, 12, 26, 14, 0, 0));

            // Act
            var response = await _httpClient.GetAsync($"api/v1/DiaryEntry/Search/{diary.DiaryId}/2022/12");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<IEnumerable<int>>();
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(26, result.ElementAt(0));
        }

        [TestMethod]
        public async Task SearchDay_MonthEnd()
        {
            // Arrange
            var diary = await CreateDiary();
            var result30 = await CreateDiaryEntry(diary.DiaryId, new DateTime(2022, 9, 30, 14, 0, 0));

            // Act
            var response = await _httpClient.GetAsync($"api/v1/DiaryEntry/Search/{diary.DiaryId}/2022/9/30");
            var result = await response.Content.ReadFromJsonAsync<IEnumerable<DiaryEntryDTO>>();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(result30.DiaryEntryId, result.ElementAt(0).DiaryEntryId);
        }

        [TestMethod]
        public async Task SearchDay()
        {
            // Arrange
            var diary = await CreateDiary();
            var result14 = await CreateDiaryEntry(diary.DiaryId, new DateTime(2020, 6, 17, 14, 0, 0));
            var result13 = await CreateDiaryEntry(diary.DiaryId, new DateTime(2020, 6, 17, 13, 0, 0));

            // Act
            var response = await _httpClient.GetAsync($"api/v1/DiaryEntry/Search/{diary.DiaryId}/2020/6/17/0");
            var result = await response.Content.ReadFromJsonAsync<IEnumerable<DiaryEntryDTO>>();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(result13.DiaryEntryId, result.ElementAt(0).DiaryEntryId);
            Assert.AreEqual(result14.DiaryEntryId, result.ElementAt(1).DiaryEntryId);
        }

        [TestMethod]
        public async Task Update_null()
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
            var diary = await CreateDiary();
            var diaryEntry = await CreateDiaryEntry(diary.DiaryId);

            // Act
            var updateDiaryEntry = new DiaryEntryDTO
            {
                DiaryEntryId = diaryEntry.DiaryEntryId,
                Date = new DateTime(2021, 5, 16, 13, 0, 0),
                DiaryId = diary.DiaryId,
                Location = $"UpdatedLocation{DateTime.UtcNow.Ticks}",
                Entry = $"UpdatedNotes{DateTime.UtcNow.Ticks}"
            };
            var response = await _httpClient.PutAsJsonAsync("/api/v1/DiaryEntry/Update", updateDiaryEntry);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DiaryEntryDTO>();
            Assert.IsNotNull(result);
            Assert.AreEqual(updateDiaryEntry.DiaryEntryId, result.DiaryEntryId);
            Assert.AreEqual(updateDiaryEntry.Date, result.Date);
            Assert.AreEqual(updateDiaryEntry.Location, result.Location);
            Assert.AreEqual(updateDiaryEntry.Entry, result.Entry);
            Assert.AreEqual(updateDiaryEntry.DiaryId, result.DiaryId);
        }

        [TestMethod]
        public async Task Delete()
        {
            // Arrange
            var diary = await CreateDiary();
            var diaryEntry = await CreateDiaryEntry(diary.DiaryId);

            // Act
            var response = await _httpClient.DeleteAsync($"/api/v1/DiaryEntry/Delete/{diaryEntry.DiaryEntryId}");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task Delete_NotFound()
        {
            // Act
            var response = await _httpClient.DeleteAsync($"/api/v1/DiaryEntry/Delete/{Guid.NewGuid()}");

            // Assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task GetMinDate()
        {
            // Arrange
            var diary = await CreateDiary();
            var minDate = new DateTime(2019, 3, 17, 14, 0, 0, DateTimeKind.Utc);
            var maxDate = new DateTime(2021, 9, 13, 14, 0, 0, DateTimeKind.Utc);
            await CreateDiaryEntry(diary.DiaryId, new DateTime(2020, 8, 26, 14, 0, 0, DateTimeKind.Utc));
            await CreateDiaryEntry(diary.DiaryId, minDate);
            await CreateDiaryEntry(diary.DiaryId, maxDate);

            // Act
            var response = await _httpClient.GetAsync($"api/v1/DiaryEntry/GetMinDate/{diary.DiaryId}");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DateTime>();
            Assert.AreEqual(minDate, result);
        }

        [TestMethod]
        public async Task GetMaxDate()
        {
            // Arrange
            var diary = await CreateDiary();
            var minDate = new DateTime(2019, 3, 17, 14, 0, 0, DateTimeKind.Utc);
            var maxDate = new DateTime(2021, 9, 13, 14, 0, 0, DateTimeKind.Utc);
            await CreateDiaryEntry(diary.DiaryId, new DateTime(2020, 8, 26, 14, 0, 0, DateTimeKind.Utc));
            await CreateDiaryEntry(diary.DiaryId, minDate);
            await CreateDiaryEntry(diary.DiaryId, maxDate);

            // Act
            var response = await _httpClient.GetAsync($"api/v1/DiaryEntry/GetMaxDate/{diary.DiaryId}");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DateTime>();
            Assert.AreEqual(maxDate, result);
        }

        [TestMethod]
        public async Task GetMinDateOnEmptyDiary()
        {
            // Arrange
            var diary = await CreateDiary();

            // Act
            var response = await _httpClient.GetAsync($"api/v1/DiaryEntry/GetMinDate/{diary.DiaryId}");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DateTime>();
            Assert.AreEqual(DateTime.UtcNow.Date, result.Date);
        }

        [TestMethod]
        public async Task GetMaxDateOnEmptyDiary()
        {
            // Arrange
            var diary = await CreateDiary();

            // Act
            var response = await _httpClient.GetAsync($"api/v1/DiaryEntry/GetMaxDate/{diary.DiaryId}");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DateTime>();
            Assert.AreEqual(DateTime.UtcNow.Date, result.Date);
        }
    }
}
