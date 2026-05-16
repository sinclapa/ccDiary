// <copyright file="AdminControllerTest.cs" company="CookingCode">
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
    using ccDiaryApi.Services;
    using ccDiaryApiTest.Integration;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Moq;

    [TestClass]
    public class AdminControllerTest
    {
        [TestInitialize]
        public async Task Init() => await SharedTestFactory.Factory.ClearDatabaseAsync();

        [TestMethod]
        public async Task GetRequests_WithoutAuth_ReturnsUnauthorized()
        {
            var client = SharedTestFactory.Factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.NoAuth, "true");
            var response = await client.GetAsync("/api/v1/Admin/Requests");
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task GetRequests_WithContributorRole_ReturnsForbidden()
        {
            var oid = "contributor-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(oid, AppRole.DiaryContributor);

            var client = SharedTestFactory.Factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserId, oid);
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryContributor");

            var response = await client.GetAsync("/api/v1/Admin/Requests");
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [TestMethod]
        public async Task GetRequests_WithAdminRole_ReturnsOk()
        {
            var oid = "admin-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(oid, AppRole.DiaryAdmin);

            var client = SharedTestFactory.Factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserId, oid);
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryAdmin");

            var response = await client.GetAsync("/api/v1/Admin/Requests");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task Approve_WithoutAuth_ReturnsUnauthorized()
        {
            var client = SharedTestFactory.Factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.NoAuth, "true");
            var response = await client.PutAsync($"/api/v1/Admin/Approve/{Guid.NewGuid()}", null);
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task Approve_WithContributorRole_ReturnsForbidden()
        {
            var oid = "contributor-approve-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(oid, AppRole.DiaryContributor);

            var client = SharedTestFactory.Factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserId, oid);
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryContributor");

            var response = await client.PutAsync($"/api/v1/Admin/Approve/{Guid.NewGuid()}", null);
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [TestMethod]
        public async Task Approve_NotFound_ReturnsNotFound()
        {
            var adminOid = "admin-approve-notfound-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(adminOid, AppRole.DiaryAdmin);

            var client = SharedTestFactory.Factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserId, adminOid);
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryAdmin");

            var response = await client.PutAsync($"/api/v1/Admin/Approve/{Guid.NewGuid()}", null);
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Approve_ExistingRequest_ReturnsOkWithRedeemUrl()
        {
            var adminOid = "admin-approve-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(adminOid, AppRole.DiaryAdmin);

            var clientAnon = SharedTestFactory.Factory.CreateClient();
            await clientAnon.PostAsJsonAsync(
                "/api/v1/AccessRequest/Submit",
                new AccessRequestController.SubmitAccessRequestBody("Eve Adams", "eve@example.com"));

            var adminClient = SharedTestFactory.Factory.CreateClient();
            adminClient.DefaultRequestHeaders.Add(TestAuthHandler.UserId, adminOid);
            adminClient.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryAdmin");

            var requests = await (await adminClient.GetAsync("/api/v1/Admin/Requests"))
                .Content.ReadFromJsonAsync<List<AccessRequestDto>>(SharedTestFactory.ApiJsonOptions);
            Assert.IsNotNull(requests);
            Assert.AreEqual(1, requests.Count);

            var approveResponse = await adminClient.PutAsync($"/api/v1/Admin/Approve/{requests[0].AccessRequestId}", null);
            Assert.AreEqual(HttpStatusCode.OK, approveResponse.StatusCode);

            var body = await approveResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("https://test-redeem.example.com", body.GetProperty("redeemUrl").GetString());
        }

        [TestMethod]
        public async Task Approve_GraphReturnsEmpty_RedeemUrlIsNull()
        {
            SharedTestFactory.Factory.GraphRedeemUrl = string.Empty;
            try
            {
                var adminOid = "admin-approve-nourl-oid";
                await SharedTestFactory.Factory.CreateAppUserAsync(adminOid, AppRole.DiaryAdmin);

                var clientAnon = SharedTestFactory.Factory.CreateClient();
                await clientAnon.PostAsJsonAsync(
                    "/api/v1/AccessRequest/Submit",
                    new AccessRequestController.SubmitAccessRequestBody("Frank Stone", "frank@example.com"));

                var adminClient = SharedTestFactory.Factory.CreateClient();
                adminClient.DefaultRequestHeaders.Add(TestAuthHandler.UserId, adminOid);
                adminClient.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryAdmin");

                var requests = await (await adminClient.GetAsync("/api/v1/Admin/Requests"))
                    .Content.ReadFromJsonAsync<List<AccessRequestDto>>(SharedTestFactory.ApiJsonOptions);
                Assert.IsNotNull(requests);

                var approveResponse = await adminClient.PutAsync($"/api/v1/Admin/Approve/{requests[0].AccessRequestId}", null);
                Assert.AreEqual(HttpStatusCode.OK, approveResponse.StatusCode);

                var body = await approveResponse.Content.ReadFromJsonAsync<JsonElement>();
                Assert.AreEqual(JsonValueKind.Null, body.GetProperty("redeemUrl").ValueKind);
            }
            finally
            {
                SharedTestFactory.Factory.GraphRedeemUrl = "https://test-redeem.example.com";
            }
        }

        [TestMethod]
        public async Task Decline_WithoutAuth_ReturnsUnauthorized()
        {
            var client = SharedTestFactory.Factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.NoAuth, "true");
            var response = await client.PutAsync($"/api/v1/Admin/Decline/{Guid.NewGuid()}", null);
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task Decline_WithContributorRole_ReturnsForbidden()
        {
            var oid = "contributor-decline-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(oid, AppRole.DiaryContributor);

            var client = SharedTestFactory.Factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserId, oid);
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryContributor");

            var response = await client.PutAsync($"/api/v1/Admin/Decline/{Guid.NewGuid()}", null);
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [TestMethod]
        public async Task Decline_NotFound_ReturnsNotFound()
        {
            var adminOid = "admin-decline-notfound-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(adminOid, AppRole.DiaryAdmin);

            var client = SharedTestFactory.Factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserId, adminOid);
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryAdmin");

            var response = await client.PutAsync($"/api/v1/Admin/Decline/{Guid.NewGuid()}", null);
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Decline_ExistingRequest_ReturnsOk()
        {
            var adminOid = "admin-oid-2";
            await SharedTestFactory.Factory.CreateAppUserAsync(adminOid, AppRole.DiaryAdmin);

            // Submit a request
            var clientAnon = SharedTestFactory.Factory.CreateClient();
            await clientAnon.PostAsJsonAsync(
                "/api/v1/AccessRequest/Submit",
                new AccessRequestController.SubmitAccessRequestBody("Dan Evans", "dan@example.com"));

            // Get the request ID
            var adminClient = SharedTestFactory.Factory.CreateClient();
            adminClient.DefaultRequestHeaders.Add(TestAuthHandler.UserId, adminOid);
            adminClient.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryAdmin");

            var requestsResponse = await adminClient.GetAsync("/api/v1/Admin/Requests");
            var requests = await requestsResponse.Content.ReadFromJsonAsync<List<AccessRequestDto>>(SharedTestFactory.ApiJsonOptions);
            Assert.IsNotNull(requests);
            Assert.AreEqual(1, requests.Count);

            var declineResponse = await adminClient.PutAsync($"/api/v1/Admin/Decline/{requests[0].AccessRequestId}", null);
            Assert.AreEqual(HttpStatusCode.OK, declineResponse.StatusCode);

            // Declined request should still appear in full list with Declined status
            var afterResponse = await adminClient.GetAsync("/api/v1/Admin/Requests");
            var afterRequests = await afterResponse.Content.ReadFromJsonAsync<List<AccessRequestDto>>(SharedTestFactory.ApiJsonOptions);
            Assert.IsNotNull(afterRequests);
            Assert.AreEqual(1, afterRequests.Count);
            Assert.AreEqual(RequestStatus.Declined, afterRequests[0].Status);
        }

        [TestMethod]
        public async Task Approve_WithNoOidClaim_ReturnsUnauthorized()
        {
            var mock = new Mock<IAccessRequestService>();
            var controller = new AdminController(mock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()),
                },
            };

            var result = await controller.Approve(Guid.NewGuid());

            Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
        }

        [TestMethod]
        public async Task Decline_WithNoOidClaim_ReturnsUnauthorized()
        {
            var mock = new Mock<IAccessRequestService>();
            var controller = new AdminController(mock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity()),
                },
            };

            var result = await controller.Decline(Guid.NewGuid());

            Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
        }

        [TestMethod]
        public async Task Delete_WithoutAuth_ReturnsUnauthorized()
        {
            var client = SharedTestFactory.Factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.NoAuth, "true");
            var response = await client.DeleteAsync($"/api/v1/Admin/Delete/{Guid.NewGuid()}");
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task Delete_WithContributorRole_ReturnsForbidden()
        {
            var oid = "contributor-delete-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(oid, AppRole.DiaryContributor);

            var client = SharedTestFactory.Factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserId, oid);
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryContributor");

            var response = await client.DeleteAsync($"/api/v1/Admin/Delete/{Guid.NewGuid()}");
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [TestMethod]
        public async Task Delete_NotFound_ReturnsNotFound()
        {
            var adminOid = "admin-delete-notfound-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(adminOid, AppRole.DiaryAdmin);

            var client = SharedTestFactory.Factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserId, adminOid);
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryAdmin");

            var response = await client.DeleteAsync($"/api/v1/Admin/Delete/{Guid.NewGuid()}");
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Delete_PendingRequest_ReturnsBadRequest()
        {
            var adminOid = "admin-delete-pending-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(adminOid, AppRole.DiaryAdmin);

            var clientAnon = SharedTestFactory.Factory.CreateClient();
            await clientAnon.PostAsJsonAsync(
                "/api/v1/AccessRequest/Submit",
                new AccessRequestController.SubmitAccessRequestBody("Ivan Test", "ivan@example.com"));

            var adminClient = SharedTestFactory.Factory.CreateClient();
            adminClient.DefaultRequestHeaders.Add(TestAuthHandler.UserId, adminOid);
            adminClient.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryAdmin");

            var requests = await (await adminClient.GetAsync("/api/v1/Admin/Requests"))
                .Content.ReadFromJsonAsync<List<AccessRequestDto>>(SharedTestFactory.ApiJsonOptions);
            Assert.IsNotNull(requests);

            var response = await adminClient.DeleteAsync($"/api/v1/Admin/Delete/{requests[0].AccessRequestId}");
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Delete_ApprovedRequest_ReturnsNoContent()
        {
            var adminOid = "admin-delete-approved-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(adminOid, AppRole.DiaryAdmin);

            var clientAnon = SharedTestFactory.Factory.CreateClient();
            await clientAnon.PostAsJsonAsync(
                "/api/v1/AccessRequest/Submit",
                new AccessRequestController.SubmitAccessRequestBody("Julia Test", "julia@example.com"));

            var adminClient = SharedTestFactory.Factory.CreateClient();
            adminClient.DefaultRequestHeaders.Add(TestAuthHandler.UserId, adminOid);
            adminClient.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryAdmin");

            var requests = await (await adminClient.GetAsync("/api/v1/Admin/Requests"))
                .Content.ReadFromJsonAsync<List<AccessRequestDto>>(SharedTestFactory.ApiJsonOptions);
            Assert.IsNotNull(requests);
            var requestId = requests[0].AccessRequestId;

            await adminClient.PutAsync($"/api/v1/Admin/Approve/{requestId}", null);

            var response = await adminClient.DeleteAsync($"/api/v1/Admin/Delete/{requestId}");
            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        }

        [TestMethod]
        public async Task Delete_DeclinedRequest_ReturnsNoContent()
        {
            var adminOid = "admin-delete-declined-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(adminOid, AppRole.DiaryAdmin);

            var clientAnon = SharedTestFactory.Factory.CreateClient();
            await clientAnon.PostAsJsonAsync(
                "/api/v1/AccessRequest/Submit",
                new AccessRequestController.SubmitAccessRequestBody("Karl Test", "karl@example.com"));

            var adminClient = SharedTestFactory.Factory.CreateClient();
            adminClient.DefaultRequestHeaders.Add(TestAuthHandler.UserId, adminOid);
            adminClient.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryAdmin");

            var requests = await (await adminClient.GetAsync("/api/v1/Admin/Requests"))
                .Content.ReadFromJsonAsync<List<AccessRequestDto>>(SharedTestFactory.ApiJsonOptions);
            Assert.IsNotNull(requests);
            var requestId = requests[0].AccessRequestId;

            await adminClient.PutAsync($"/api/v1/Admin/Decline/{requestId}", null);

            var response = await adminClient.DeleteAsync($"/api/v1/Admin/Delete/{requestId}");
            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        }

        [TestMethod]
        public async Task ResendInvitation_WithoutAuth_ReturnsUnauthorized()
        {
            var client = SharedTestFactory.Factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.NoAuth, "true");
            var response = await client.PostAsync($"/api/v1/Admin/ResendInvitation/{Guid.NewGuid()}", null);
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task ResendInvitation_WithContributorRole_ReturnsForbidden()
        {
            var oid = "contributor-resend-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(oid, AppRole.DiaryContributor);

            var client = SharedTestFactory.Factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserId, oid);
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryContributor");

            var response = await client.PostAsync($"/api/v1/Admin/ResendInvitation/{Guid.NewGuid()}", null);
            Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [TestMethod]
        public async Task ResendInvitation_NotFound_ReturnsNotFound()
        {
            var adminOid = "admin-resend-notfound-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(adminOid, AppRole.DiaryAdmin);

            var client = SharedTestFactory.Factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserId, adminOid);
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryAdmin");

            var response = await client.PostAsync($"/api/v1/Admin/ResendInvitation/{Guid.NewGuid()}", null);
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task ResendInvitation_AfterApprove_ReturnsOkWithRedeemUrl()
        {
            var adminOid = "admin-resend-ok-oid";
            await SharedTestFactory.Factory.CreateAppUserAsync(adminOid, AppRole.DiaryAdmin);

            var clientAnon = SharedTestFactory.Factory.CreateClient();
            await clientAnon.PostAsJsonAsync(
                "/api/v1/AccessRequest/Submit",
                new AccessRequestController.SubmitAccessRequestBody("Grace Hill", "grace@example.com"));

            var adminClient = SharedTestFactory.Factory.CreateClient();
            adminClient.DefaultRequestHeaders.Add(TestAuthHandler.UserId, adminOid);
            adminClient.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryAdmin");

            var requests = await (await adminClient.GetAsync("/api/v1/Admin/Requests"))
                .Content.ReadFromJsonAsync<List<AccessRequestDto>>(SharedTestFactory.ApiJsonOptions);
            Assert.IsNotNull(requests);
            var requestId = requests[0].AccessRequestId;

            await adminClient.PutAsync($"/api/v1/Admin/Approve/{requestId}", null);

            var resendResponse = await adminClient.PostAsync($"/api/v1/Admin/ResendInvitation/{requestId}", null);
            Assert.AreEqual(HttpStatusCode.OK, resendResponse.StatusCode);

            var body = await resendResponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("https://test-redeem.example.com", body.GetProperty("redeemUrl").GetString());
        }

        [TestMethod]
        public async Task ResendInvitation_NoUrlAvailable_ReturnsBadRequest()
        {
            SharedTestFactory.Factory.GraphRedeemUrl = string.Empty;
            try
            {
                var adminOid = "admin-resend-nourl-oid";
                await SharedTestFactory.Factory.CreateAppUserAsync(adminOid, AppRole.DiaryAdmin);

                var clientAnon = SharedTestFactory.Factory.CreateClient();
                await clientAnon.PostAsJsonAsync(
                    "/api/v1/AccessRequest/Submit",
                    new AccessRequestController.SubmitAccessRequestBody("Hank Jones", "hank@example.com"));

                var adminClient = SharedTestFactory.Factory.CreateClient();
                adminClient.DefaultRequestHeaders.Add(TestAuthHandler.UserId, adminOid);
                adminClient.DefaultRequestHeaders.Add(TestAuthHandler.UserRole, "DiaryAdmin");

                var requests = await (await adminClient.GetAsync("/api/v1/Admin/Requests"))
                    .Content.ReadFromJsonAsync<List<AccessRequestDto>>(SharedTestFactory.ApiJsonOptions);
                Assert.IsNotNull(requests);
                var requestId = requests[0].AccessRequestId;

                await adminClient.PutAsync($"/api/v1/Admin/Approve/{requestId}", null);

                var resendResponse = await adminClient.PostAsync($"/api/v1/Admin/ResendInvitation/{requestId}", null);
                Assert.AreEqual(HttpStatusCode.BadRequest, resendResponse.StatusCode);
            }
            finally
            {
                SharedTestFactory.Factory.GraphRedeemUrl = "https://test-redeem.example.com";
            }
        }
    }
}
