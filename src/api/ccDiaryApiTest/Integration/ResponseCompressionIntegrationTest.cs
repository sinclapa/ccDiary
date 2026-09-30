// <copyright file="ResponseCompressionIntegrationTest.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.Integration
{
    using System.IO.Compression;
    using System.Net;
    using System.Text.Json;

    /// <summary>
    /// Integration tests proving API responses are compressed when the client accepts it.
    /// </summary>
    [TestClass]
    public class ResponseCompressionIntegrationTest
    {
        private const string JsonEndpoint = "/api/v1/AppInfo/Get";

        private HttpClient _httpClient = null!;

        // The test server's client does not decompress, so the raw encoding is observable.
        [TestInitialize]
        public void TestInit() => _httpClient = SharedTestFactory.Factory.CreateDefaultClient();

        [TestMethod]
        public async Task Get_AcceptingBrotliAndGzip_PrefersBrotli()
        {
            // Act
            var response = await SendAsync("gzip, deflate, br");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("br", string.Join(",", response.Content.Headers.ContentEncoding));
            await AssertDecompressesToJsonAsync(response, s => new BrotliStream(s, CompressionMode.Decompress));
        }

        [TestMethod]
        public async Task Get_AcceptingGzip_ReturnsGzip()
        {
            // Act
            var response = await SendAsync("gzip");

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("gzip", string.Join(",", response.Content.Headers.ContentEncoding));
            await AssertDecompressesToJsonAsync(response, s => new GZipStream(s, CompressionMode.Decompress));
        }

        [TestMethod]
        public async Task Get_WithoutAcceptEncoding_ReturnsUncompressed()
        {
            // Act
            var response = await _httpClient.GetAsync(JsonEndpoint);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(0, response.Content.Headers.ContentEncoding.Count);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.AreEqual(JsonValueKind.Object, doc.RootElement.ValueKind);
        }

        private static async Task AssertDecompressesToJsonAsync(HttpResponseMessage response, Func<Stream, Stream> decompress)
        {
            await using var body = await response.Content.ReadAsStreamAsync();
            await using var decompressed = decompress(body);
            using var doc = await JsonDocument.ParseAsync(decompressed);
            Assert.AreEqual(JsonValueKind.Object, doc.RootElement.ValueKind);
        }

        private Task<HttpResponseMessage> SendAsync(string acceptEncoding)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, JsonEndpoint);
            request.Headers.TryAddWithoutValidation("Accept-Encoding", acceptEncoding);
            return _httpClient.SendAsync(request);
        }
    }
}
