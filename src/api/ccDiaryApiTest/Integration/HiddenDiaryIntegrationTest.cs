// <copyright file="HiddenDiaryIntegrationTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Integration
{
    using System.Net;
    using System.Net.Http.Json;
    using ccDiaryApi.Data.Model;

    /// <summary>
    /// A hidden diary, and everything in it, is visible only to an admin and the contributor who
    /// owns it. These run the real pipeline - the role comes from the AppUser row, as in
    /// production - and call every read endpoint as each kind of caller.
    /// </summary>
    [TestClass]
    public class HiddenDiaryIntegrationTest
    {
        private const string OwnerOid = "hidden-diary-owner";
        private const string OtherContributorOid = "hidden-diary-other-contributor";
        private const string ReaderOid = "hidden-diary-reader-without-role";

        private static readonly DateTime EntryDate = new DateTime(1918, 5, 21, 12, 0, 0, DateTimeKind.Utc);

        private HttpClient _admin = null!;
        private HttpClient _owner = null!;
        private DiaryDto _hidden = null!;
        private DiaryDto _visible = null!;
        private DiaryEntryDto _hiddenEntry = null!;

        /// <summary>Who is calling the API.</summary>
        public enum Caller
        {
            /// <summary>Not signed in.</summary>
            Anonymous,

            /// <summary>Signed in, but with no AppUser row and so no role.</summary>
            ReaderWithoutRole,

            /// <summary>A contributor who does not own the hidden diary.</summary>
            OtherContributor,

            /// <summary>The contributor who created, and so owns, the hidden diary.</summary>
            Owner,

            /// <summary>An admin.</summary>
            Admin,
        }

        [TestInitialize]
        public async Task TestInit()
        {
            _admin = SharedTestFactory.Factory.CreateDefaultClient();
            await SharedTestFactory.Factory.ClearDatabaseAsync();
            await SharedTestFactory.Factory.CreateAppUserAsync(SharedTestFactory.Factory.DefaultUserId, AppRole.DiaryAdmin);

            await SharedTestFactory.Factory.CreateAppUserAsync(OwnerOid, AppRole.DiaryContributor);
            await SharedTestFactory.Factory.CreateAppUserAsync(OtherContributorOid, AppRole.DiaryContributor);
            _owner = SharedTestFactory.Factory.CreateDefaultClient();
            _owner.DefaultRequestHeaders.Add(TestAuthHandler.UserId, OwnerOid);

            // Created by the owner, so the diary records them in OwnerId as it would for real.
            _hidden = await CreateDiary(_owner, "Hidden diary", isHidden: true);
            _visible = await CreateDiary(_admin, "Visible diary", isHidden: false);
            _hiddenEntry = await CreateEntry(_owner, _hidden.DiaryId!.Value);
            await CreateEntry(_admin, _visible.DiaryId!.Value);
            Assert.AreEqual(OwnerOid, _hidden.OwnerId);
        }

        [TestMethod]
        [DataRow(Caller.Anonymous, 1)]
        [DataRow(Caller.ReaderWithoutRole, 1)]
        [DataRow(Caller.OtherContributor, 1)]
        [DataRow(Caller.Owner, 2)]
        [DataRow(Caller.Admin, 2)]
        public async Task List_IncludesAHiddenDiaryOnlyForItsOwnerAndAdmins(Caller caller, int expected)
        {
            var response = await Send(caller, HttpMethod.Get, "/api/v1/Diary/Get");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var page = await response.Content.ReadFromJsonAsync<PagedResultDto<DiaryDto>>(SharedTestFactory.ApiJsonOptions);
            Assert.IsNotNull(page);

            // The count matters as much as the items: it must not give the hidden diary away either.
            Assert.AreEqual(expected, page.TotalCount);
            Assert.AreEqual(expected, page.Items.Count());
            Assert.AreEqual(expected == 2, page.Items.Any(d => d.DiaryId == _hidden.DiaryId));
        }

        [TestMethod]
        [DataRow(Caller.Anonymous)]
        [DataRow(Caller.ReaderWithoutRole)]
        [DataRow(Caller.OtherContributor)]
        public async Task EveryReadOfAHiddenDiary_IsNotFound_ForEveryoneElse(Caller caller)
        {
            foreach (var url in HiddenDiaryReadUrls(includeExport: caller != Caller.Anonymous))
            {
                var response = await Send(caller, HttpMethod.Get, url);
                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, $"{caller} GET {url}");
            }
        }

        [TestMethod]
        [DataRow(Caller.Owner)]
        [DataRow(Caller.Admin)]
        public async Task EveryReadOfAHiddenDiary_Succeeds_ForItsOwnerAndAdmins(Caller caller)
        {
            foreach (var url in HiddenDiaryReadUrls(includeExport: true))
            {
                var response = await Send(caller, HttpMethod.Get, url);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{caller} GET {url}");
            }
        }

        [TestMethod]
        public async Task AVisibleDiary_IsStillReadableAnonymously()
        {
            var id = _visible.DiaryId!.Value;
            foreach (var url in new[]
            {
                $"/api/v1/Diary/Get/{id}",
                $"/api/v1/DiaryEntry/GetDiaryEntries/{id}",
                $"/api/v1/DiaryEntry/Search/{id}/{EntryDate.Year}/{EntryDate.Month}/{EntryDate.Day}",
            })
            {
                var response = await Send(Caller.Anonymous, HttpMethod.Get, url);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"GET {url}");
            }
        }

        [TestMethod]
        public async Task HidingAndUnhiding_TakesEffectImmediately()
        {
            var diary = _visible;
            var url = $"/api/v1/Diary/Get/{diary.DiaryId}";

            diary.IsHidden = true;
            Assert.AreEqual(HttpStatusCode.OK, (await _admin.PutAsJsonAsync("/api/v1/Diary/Update", diary)).StatusCode);
            Assert.AreEqual(HttpStatusCode.NotFound, (await Send(Caller.Anonymous, HttpMethod.Get, url)).StatusCode);

            diary.IsHidden = false;
            Assert.AreEqual(HttpStatusCode.OK, (await _admin.PutAsJsonAsync("/api/v1/Diary/Update", diary)).StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, (await Send(Caller.Anonymous, HttpMethod.Get, url)).StatusCode);
        }

        [TestMethod]
        public async Task IsHidden_RoundTripsForTheOwner()
        {
            var response = await Send(Caller.Owner, HttpMethod.Get, $"/api/v1/Diary/Get/{_hidden.DiaryId}");

            var diary = await response.Content.ReadFromJsonAsync<DiaryDto>(SharedTestFactory.ApiJsonOptions);
            Assert.IsNotNull(diary);
            Assert.IsTrue(diary.IsHidden);
        }

        [TestMethod]
        public async Task AnUnknownDiary_KeepsItsExistingBehaviour()
        {
            // The filter only hides hidden diaries; a missing one is still answered by the action.
            var response = await Send(Caller.Anonymous, HttpMethod.Get, $"/api/v1/DiaryEntry/GetDiaryEntries/{Guid.NewGuid()}");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        private static async Task<HttpResponseMessage> Send(Caller caller, HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);
            switch (caller)
            {
                case Caller.Anonymous:
                    request.Headers.Add(TestAuthHandler.NoAuth, "true");
                    break;
                case Caller.ReaderWithoutRole:
                    request.Headers.Add(TestAuthHandler.UserId, ReaderOid);
                    break;
                case Caller.OtherContributor:
                    request.Headers.Add(TestAuthHandler.UserId, OtherContributorOid);
                    break;
                case Caller.Owner:
                    request.Headers.Add(TestAuthHandler.UserId, OwnerOid);
                    break;
                case Caller.Admin:
                    break;
            }

            return await SharedTestFactory.Factory.CreateDefaultClient().SendAsync(request);
        }

        private static async Task<DiaryDto> CreateDiary(HttpClient client, string title, bool isHidden)
        {
            var response = await client.PostAsJsonAsync(
                "/api/v1/Diary/Create",
                new DiaryDto { Title = title, Author = "Test author", IsHidden = isHidden });
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            var diary = await response.Content.ReadFromJsonAsync<DiaryDto>(SharedTestFactory.ApiJsonOptions);
            Assert.IsNotNull(diary);
            Assert.AreEqual(isHidden, diary.IsHidden);
            return diary;
        }

        private static async Task<DiaryEntryDto> CreateEntry(HttpClient client, Guid diaryId)
        {
            var response = await client.PostAsJsonAsync(
                "/api/v1/DiaryEntry/Create",
                new DiaryEntryDto { DiaryId = diaryId, Date = EntryDate, Location = "Location", Entry = "Notes from the front" });
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            var entry = await response.Content.ReadFromJsonAsync<DiaryEntryDto>(SharedTestFactory.ApiJsonOptions);
            Assert.IsNotNull(entry);
            return entry;
        }

        private List<string> HiddenDiaryReadUrls(bool includeExport)
        {
            var id = _hidden.DiaryId!.Value;
            var urls = new List<string>
            {
                $"/api/v1/Diary/Get/{id}",
                $"/api/v1/DiaryEntry/Search/{id}",
                $"/api/v1/DiaryEntry/Search/{id}/{EntryDate.Year}",
                $"/api/v1/DiaryEntry/Search/{id}/{EntryDate.Year}/{EntryDate.Month}",
                $"/api/v1/DiaryEntry/Search/{id}/{EntryDate.Year}/{EntryDate.Month}/{EntryDate.Day}",
                $"/api/v1/DiaryEntry/Get/{_hiddenEntry.DiaryEntryId}",
                $"/api/v1/DiaryEntry/GetDiaryEntries/{id}",
                $"/api/v1/DiaryEntry/TextSearch/{id}?search=Notes",
                $"/api/v1/DiaryEntry/GetMinDate/{id}",
                $"/api/v1/DiaryEntry/GetMaxDate/{id}",
            };

            // Export needs a signed-in caller before visibility is even considered.
            if (includeExport)
            {
                urls.Add($"/api/v1/DiaryArchive/Export/{id}");
            }

            return urls;
        }
    }
}
