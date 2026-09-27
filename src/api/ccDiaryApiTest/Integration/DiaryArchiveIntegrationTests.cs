// <copyright file="DiaryArchiveIntegrationTests.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Integration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http.Json;
    using System.Threading.Tasks;
    using ccDiaryApi.Data.Model;

    [TestClass]
    public class DiaryArchiveIntegrationTests
    {
        private HttpClient _httpClient = null!;

        [TestInitialize]
        public async Task TestInit()
        {
            _httpClient = SharedTestFactory.Factory.CreateDefaultClient();
            await SharedTestFactory.Factory.ClearDatabaseAsync();

            // Import is for admins outside the local environments, and the factory runs as Development.
            await SharedTestFactory.Factory.CreateAppUserAsync(SharedTestFactory.Factory.DefaultUserId, AppRole.DiaryAdmin);
        }

        [TestMethod]
        public async Task TheApplicationItselfMayImport()
        {
            // the deploy pipeline seeds each environment this way: an app-only token for the
            // application's own registration, with no AppUser row behind it
            var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/DiaryArchive/Import")
            {
                Content = JsonContent.Create(CreateArchiveDiary()),
            };
            request.Headers.Add(TestAuthHandler.UserId, "pipeline-service-principal");
            request.Headers.Add(TestAuthHandler.AppClientId, CustomWebApplicationFactory<Program>.ApplicationClientId);

            var response = await _httpClient.SendAsync(request);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task AnotherApplicationMayNotImport()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/DiaryArchive/Import")
            {
                Content = JsonContent.Create(CreateArchiveDiary()),
            };
            request.Headers.Add(TestAuthHandler.UserId, "some-other-app");
            request.Headers.Add(TestAuthHandler.AppClientId, "99999999-0000-0000-0000-000000000000");

            var response = await _httpClient.SendAsync(request);

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [TestMethod]
        public async Task ImportAsAContributorIsForbidden()
        {
            await SharedTestFactory.Factory.CreateAppUserAsync(SharedTestFactory.Factory.DefaultUserId, AppRole.DiaryContributor);

            var response = await _httpClient.PostAsJsonAsync($"api/v1/DiaryArchive/Import", CreateArchiveDiary());

            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [TestMethod]
        public async Task ImportNew()
        {
            // Arrange
            var archiveDiary = CreateArchiveDiary();

            // Act
            var response = await _httpClient.PostAsJsonAsync($"api/v1/DiaryArchive/Import", archiveDiary);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DiaryDTO>();
            Assert.IsNotNull(result);
            Assert.AreEqual(archiveDiary.Diary.DiaryId, result.DiaryId);
            Assert.AreEqual(archiveDiary.Diary.Title, result.Title);
            Assert.AreEqual(archiveDiary.Diary.Author, result.Author);
            Assert.AreEqual(archiveDiary.Diary.Description, result.Description);

            // Validate entries
            var responeEntries = await _httpClient.GetAsync($"api/v1/DiaryEntry/GetDiaryEntries/{archiveDiary.Diary.DiaryId}");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var resultEntries = await responeEntries.Content.ReadFromJsonAsync<IEnumerable<DiaryEntryDTO>>(SharedTestFactory.ApiJsonOptions);
            Assert.IsNotNull(resultEntries);
            Assert.AreEqual(3, resultEntries.Count());
            Assert.AreEqual(archiveDiary.DiaryEntries[0].Entry, resultEntries.ElementAt(0).Entry);
            Assert.AreEqual(archiveDiary.DiaryEntries[1].Entry, resultEntries.ElementAt(1).Entry);
            Assert.AreEqual(archiveDiary.DiaryEntries[2].Entry, resultEntries.ElementAt(2).Entry);
        }

        [TestMethod]
        public async Task ImportUpdate()
        {
            // Arrange
            var archiveDiary = CreateArchiveDiary();
            await _httpClient.PostAsJsonAsync($"api/v1/DiaryArchive/Import", archiveDiary);
            archiveDiary.Diary.Title = "Age of computers";
            archiveDiary.DiaryEntries[1].Entry = "International Business Machines";

            // Act
            var response = await _httpClient.PostAsJsonAsync($"api/v1/DiaryArchive/Import", archiveDiary);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<DiaryDTO>();
            Assert.IsNotNull(result);
            Assert.AreEqual(archiveDiary.Diary.DiaryId, result.DiaryId);
            Assert.AreEqual(archiveDiary.Diary.Title, result.Title);
            Assert.AreEqual(archiveDiary.Diary.Author, result.Author);
            Assert.AreEqual(archiveDiary.Diary.Description, result.Description);

            // Validate entries
            var responeEntries = await _httpClient.GetAsync($"api/v1/DiaryEntry/GetDiaryEntries/{archiveDiary.Diary.DiaryId}");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var resultEntries = await responeEntries.Content.ReadFromJsonAsync<IEnumerable<DiaryEntryDTO>>(SharedTestFactory.ApiJsonOptions);
            Assert.IsNotNull(resultEntries);
            Assert.AreEqual(3, resultEntries.Count());
            Assert.AreEqual(archiveDiary.DiaryEntries[0].Entry, resultEntries.ElementAt(0).Entry);
            Assert.AreEqual(archiveDiary.DiaryEntries[1].Entry, resultEntries.ElementAt(1).Entry);
            Assert.AreEqual(archiveDiary.DiaryEntries[2].Entry, resultEntries.ElementAt(2).Entry);
        }

        [TestMethod]
        public async Task ImportUpdateShowMapAndMapLocation()
        {
            // Arrange — import with showMap=false and no mapLocation
            var archiveDiary = CreateArchiveDiary();
            await _httpClient.PostAsJsonAsync($"api/v1/DiaryArchive/Import", archiveDiary);

            // Update showMap and mapLocation on one entry and reimport
            archiveDiary.DiaryEntries[0].ShowMap = true;
            archiveDiary.DiaryEntries[0].MapLocation = "London, UK";

            // Act
            var response = await _httpClient.PostAsJsonAsync($"api/v1/DiaryArchive/Import", archiveDiary);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var responeEntries = await _httpClient.GetAsync($"api/v1/DiaryEntry/GetDiaryEntries/{archiveDiary.Diary.DiaryId}");
            var resultEntries = await responeEntries.Content.ReadFromJsonAsync<IEnumerable<DiaryEntryDTO>>(SharedTestFactory.ApiJsonOptions);
            Assert.IsNotNull(resultEntries);
            var updatedEntry = resultEntries.First(e => e.DiaryEntryId == archiveDiary.DiaryEntries[0].DiaryEntryId);
            Assert.IsTrue(updatedEntry.ShowMap);
            Assert.AreEqual("London, UK", updatedEntry.MapLocation);
        }

        [TestMethod]
        public async Task ImportUpdateShowJourneyAndLocations()
        {
            // Arrange — import with showJourney=false and no locations
            var archiveDiary = CreateArchiveDiary();
            await _httpClient.PostAsJsonAsync($"api/v1/DiaryArchive/Import", archiveDiary);

            // Update showJourney, fromLocation, toLocation, journeyMode on one entry and reimport
            archiveDiary.DiaryEntries[0].ShowJourney = true;
            archiveDiary.DiaryEntries[0].FromLocation = "Glasgow, UK";
            archiveDiary.DiaryEntries[0].ToLocation = "Edinburgh, UK";
            archiveDiary.DiaryEntries[0].JourneyMode = JourneyMode.Train;

            // Act
            var response = await _httpClient.PostAsJsonAsync($"api/v1/DiaryArchive/Import", archiveDiary);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var responeEntries = await _httpClient.GetAsync($"api/v1/DiaryEntry/GetDiaryEntries/{archiveDiary.Diary.DiaryId}");
            var resultEntries = await responeEntries.Content.ReadFromJsonAsync<IEnumerable<DiaryEntryDTO>>(SharedTestFactory.ApiJsonOptions);
            Assert.IsNotNull(resultEntries);
            var updatedEntry = resultEntries.First(e => e.DiaryEntryId == archiveDiary.DiaryEntries[0].DiaryEntryId);
            Assert.IsTrue(updatedEntry.ShowJourney);
            Assert.AreEqual("Glasgow, UK", updatedEntry.FromLocation);
            Assert.AreEqual("Edinburgh, UK", updatedEntry.ToLocation);
            Assert.AreEqual(JourneyMode.Train, updatedEntry.JourneyMode);
        }

        private static DiaryArchiveDTO CreateArchiveDiary()
        {
            var diary = new DiaryDTO { Author = "Paul John", Title = "History of computers", Description = "Computers from ancient time to digital era", DiaryId = Guid.NewGuid() };
            var diaryEntries = new List<DiaryEntryDTO>
            {
                new DiaryEntryDTO { Date = new DateTime(2024, 11, 19, 10, 15, 0, DateTimeKind.Utc), DiaryId = diary.DiaryId!.Value, Entry = "Spectrum", Location = "Glasgow", DiaryEntryId = Guid.NewGuid() },
                new DiaryEntryDTO { Date = new DateTime(2024, 11, 19, 14, 25, 0, DateTimeKind.Utc), DiaryId = diary.DiaryId!.Value, Entry = "IBM", Location = "New York", DiaryEntryId = Guid.NewGuid() },
                new DiaryEntryDTO { Date = new DateTime(2024, 11, 20, 8, 18, 0, DateTimeKind.Utc), DiaryId = diary.DiaryId!.Value, Entry = "Acorn", Location = "Manchester", DiaryEntryId = Guid.NewGuid() },
            };
            var archiveDiary = new DiaryArchiveDTO { Diary = diary, DiaryEntries = diaryEntries };
            return archiveDiary;
        }
    }
}
