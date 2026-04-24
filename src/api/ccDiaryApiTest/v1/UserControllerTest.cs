// <copyright file="UserControllerTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Security.Claims;
    using System.Text.Json;
    using System.Threading.Tasks;
    using ccDiaryApi.Controllers.v1;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Extensions;
    using ccDiaryApi.Services;
    using ccDiaryApiTest.Integration;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Moq;

    [TestClass]
    public class UserControllerTest
    {
        [TestInitialize]
        public async Task Init() => await SharedTestFactory.Factory.ClearDatabaseAsync();

        [TestMethod]
        public async Task Me_WithoutAuth_ReturnsUnauthorized()
        {
            var client = SharedTestFactory.Factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.NoAuth, "true");
            var response = await client.GetAsync("/api/v1/User/Me");
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task Me_UserNotInDb_ReturnsNotFound()
        {
            var client = SharedTestFactory.Factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserId, "unknown-oid");

            var response = await client.GetAsync("/api/v1/User/Me");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Me_AdminUser_ReturnsCorrectRole()
        {
            var oid = "me-admin-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(oid, AppRole.DiaryAdmin);

            var client = SharedTestFactory.Factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserId, oid);

            var response = await client.GetAsync("/api/v1/User/Me");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var role = json.GetProperty("role").GetString();
            Assert.AreEqual("diary-admin", role);
        }

        [TestMethod]
        public async Task Me_ApprovedRequest_AutoProvisions_AndReturnsContributorRole()
        {
            var oid = "new-user-oid";
            var email = $"{oid}@test.com";

            // Submit and approve an access request for this email
            var anonClient = SharedTestFactory.Factory.CreateClient();
            await anonClient.PostAsJsonAsync(
                "/api/v1/AccessRequest/Submit",
                new { displayName = "New User", email });

            var adminOid = "admin-provision-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(adminOid, AppRole.DiaryAdmin);
            var adminClient = SharedTestFactory.Factory.CreateClient();
            adminClient.DefaultRequestHeaders.Add(TestAuthHandler.UserId, adminOid);
            adminClient.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryAdmin");

            var requests = await (await adminClient.GetAsync("/api/v1/Admin/Requests"))
                .Content.ReadFromJsonAsync<List<AccessRequestDto>>(SharedTestFactory.ApiJsonOptions);
            Assert.IsNotNull(requests);
            await adminClient.PutAsync($"/api/v1/Admin/Approve/{requests[0].AccessRequestId}", null);

            // Now the new user logs in — their email matches an approved request, so AppUser is auto-created
            var userClient = SharedTestFactory.Factory.CreateClient();
            userClient.DefaultRequestHeaders.Add(TestAuthHandler.UserId, oid);
            userClient.DefaultRequestHeaders.Add(TestAuthHandler.UserEmail, email);

            var response = await userClient.GetAsync("/api/v1/User/Me");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("diary-contributor", json.GetProperty("role").GetString());
        }

        [TestMethod]
        public async Task Me_ContributorUser_ReturnsCorrectRole()
        {
            var oid = "me-contributor-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(oid, AppRole.DiaryContributor);

            var client = SharedTestFactory.Factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserId, oid);

            var response = await client.GetAsync("/api/v1/User/Me");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var role = json.GetProperty("role").GetString();
            Assert.AreEqual("diary-contributor", role);
        }

        [TestMethod]
        public async Task Me_WithNoOidClaim_ReturnsUnauthorized()
        {
            var mock = new Mock<IUserService>();
            var controller = new UserController(mock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()),
                },
            };

            var result = await controller.Me();

            Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
        }

        [TestMethod]
        public void GetOid_WithAltClaimType_ReturnsOid()
        {
            const string altClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[] { new Claim(altClaimType, "alt-oid-123") },
                    "test"));

            var oid = user.GetOid();

            Assert.AreEqual("alt-oid-123", oid);
        }
    }
}
