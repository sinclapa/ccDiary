// <copyright file="DiaryVisibilityTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using ccDiaryApi.Authorization;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Abstractions;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.AspNetCore.Routing;
    using Moq;
    using static ccDiaryApi.Authorization.RequireVisibleDiaryAttribute;

    [TestClass]
    public class DiaryVisibilityTest
    {
        private static readonly ClaimsPrincipal Anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        [TestMethod]
        [DataRow(null, null, false, DisplayName = "Anonymous")]
        [DataRow("reader", null, false, DisplayName = "Signed in, no role")]
        [DataRow("someone-else", "DiaryContributor", false, DisplayName = "Another contributor")]
        [DataRow("owner", null, false, DisplayName = "Owner whose contributor role was withdrawn")]
        [DataRow("owner", "DiaryContributor", true, DisplayName = "Owner, contributor")]
        [DataRow("someone-else", "DiaryAdmin", true, DisplayName = "Admin")]
        public async Task AHiddenDiary_IsVisibleOnlyToAnAdminOrItsContributorOwner(string? oid, string? role, bool expected)
        {
            var visibility = TestDiaryVisibility.Create();
            var hidden = Diary(isHidden: true, ownerId: "owner");
            var caller = oid == null ? Anonymous : User(oid, role);

            Assert.AreEqual(expected, await visibility.CanViewAsync(caller, hidden));
            Assert.AreEqual(expected, (await visibility.CanSeeHiddenAsync(caller))(hidden));
        }

        [TestMethod]
        public async Task AnOwnerlessHiddenDiary_IsVisibleOnlyToAdmins()
        {
            // Diaries from before ownership was recorded have no owner, so no contributor matches.
            var visibility = TestDiaryVisibility.Create();
            var ownerless = Diary(isHidden: true, ownerId: null);

            Assert.IsFalse(await visibility.CanViewAsync(User("someone", "DiaryContributor"), ownerless));
            Assert.IsTrue(await visibility.CanViewAsync(User("someone", "DiaryAdmin"), ownerless));
        }

        [TestMethod]
        public async Task CanView_AMissingOrVisibleDiary_IsLeftToTheCaller()
        {
            var visibility = TestDiaryVisibility.Create();

            Assert.IsTrue(await visibility.CanViewAsync(Anonymous, null));
            Assert.IsTrue(await visibility.CanViewAsync(Anonymous, Diary(isHidden: false)));
        }

        [TestMethod]
        public async Task Filter_OnAnActionWithoutADiaryId_Throws()
        {
            var filter = new RequireVisibleDiaryFilter(Mock.Of<IDiaryService>(), TestDiaryVisibility.Create());

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => filter.OnActionExecutionAsync(Context(Anonymous, new Dictionary<string, object?>()), Next));
        }

        [TestMethod]
        public async Task Filter_AHiddenDiary_IsNotFoundForThePublic_AndTheActionNeverRuns()
        {
            var diary = Diary(isHidden: true);
            var diaries = new Mock<IDiaryService>();
            diaries.Setup(d => d.GetDiaryAsync(diary.DiaryId!.Value)).ReturnsAsync(diary);
            var filter = new RequireVisibleDiaryFilter(diaries.Object, TestDiaryVisibility.Create());
            var context = Context(Anonymous, new Dictionary<string, object?> { ["diaryId"] = diary.DiaryId!.Value });
            var ran = false;

            await filter.OnActionExecutionAsync(context, () =>
            {
                ran = true;
                return Next();
            });

            Assert.IsInstanceOfType(context.Result, typeof(NotFoundResult));
            Assert.IsFalse(ran);
        }

        private static ClaimsPrincipal User(string oid, string? role)
        {
            var claims = new List<Claim> { new Claim("oid", oid) };
            if (role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }

        private static DiaryDto Diary(bool isHidden, string? ownerId = "owner") =>
            new DiaryDto { DiaryId = Guid.NewGuid(), Title = "A diary", Author = "An author", OwnerId = ownerId, IsHidden = isHidden };

        private static ActionExecutingContext Context(ClaimsPrincipal user, IDictionary<string, object?> arguments)
        {
            var actionContext = new ActionContext(
                new DefaultHttpContext { User = user },
                new RouteData(),
                new ActionDescriptor { DisplayName = "TestAction" });
            return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), arguments, controller: new object());
        }

        private static Task<ActionExecutedContext> Next() => Task.FromResult<ActionExecutedContext>(null!);
    }
}
