// <copyright file="AccessRequestControllerTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Threading.Tasks;
    using ccDiaryApi.Controllers.v1;
    using ccDiaryApiTest.Integration;

    [TestClass]
    public class AccessRequestControllerTest
    {
        [TestInitialize]
        public async Task Init() => await SharedTestFactory.Factory.ClearDatabaseAsync();

        [TestMethod]
        public async Task Submit_ReturnsCreated()
        {
            var client = SharedTestFactory.Factory.CreateClient();
            var response = await client.PostAsJsonAsync(
                "/api/v1/AccessRequest/Submit",
                new AccessRequestController.SubmitAccessRequestBody("Alice Smith", "alice@example.com"));

            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        }

        [TestMethod]
        public async Task Submit_DuplicatePending_ReturnsConflict()
        {
            var client = SharedTestFactory.Factory.CreateClient();
            var body = new AccessRequestController.SubmitAccessRequestBody("Bob Jones", "bob@example.com");

            await client.PostAsJsonAsync("/api/v1/AccessRequest/Submit", body);
            var response = await client.PostAsJsonAsync("/api/v1/AccessRequest/Submit", body);

            Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        }

        [TestMethod]
        public async Task Submit_DoesNotRequireAuthentication()
        {
            // Client with no auth headers
            var client = SharedTestFactory.Factory.CreateClient();
            var response = await client.PostAsJsonAsync(
                "/api/v1/AccessRequest/Submit",
                new AccessRequestController.SubmitAccessRequestBody("Carol White", "carol@example.com"));

            Assert.AreNotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
