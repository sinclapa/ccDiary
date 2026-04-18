// <copyright file="GraphService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using Microsoft.Identity.Client;

    public class GraphService : IGraphService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GraphService> _logger;

        public GraphService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<GraphService> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<string> SendInvitationAsync(string email, string displayName)
        {
            var tenantId = _configuration["Graph:TenantId"];
            var clientId = _configuration["Graph:ClientId"];
            var clientSecret = _configuration["Graph:ClientSecret"];
            var redirectUrl = _configuration["Graph:InviteRedirectUrl"] ?? "https://localhost:5173";

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogWarning("Graph API not configured — skipping invitation.");
                return string.Empty;
            }

            var token = await AcquireTokenAsync(tenantId, clientId, clientSecret);

            var appName = _configuration["Graph:AppDisplayName"] ?? "Cooking Code Diary";
            var invitation = new
            {
                invitedUserEmailAddress = email,
                invitedUserDisplayName = displayName,
                inviteRedirectUrl = redirectUrl,
                sendInvitationMessage = true,
                invitedUserMessageInfo = new
                {
                    messageLanguage = "en-US",
                    customizedMessageBody =
                        $"Hi {displayName},\n\n" +
                        $"You have been invited to join {appName}.\n\n" +
                        "Click the link below to accept your invitation and get started.\n\n" +
                        "If you did not request access, you can ignore this email.",
                },
            };

            var json = JsonSerializer.Serialize(invitation);
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsync(
                "https://graph.microsoft.com/v1.0/invitations",
                new StringContent(json, Encoding.UTF8, "application/json"));

            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Graph invitation failed: {Status} {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"Failed to send Entra invitation: {response.StatusCode}");
            }

            _logger.LogInformation("Entra B2B invitation sent. Response: {Body}", body);

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("inviteRedeemUrl", out var urlProp)
                ? urlProp.GetString() ?? string.Empty
                : string.Empty;
        }

        private static string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return "***";
            }

            var atIndex = email.IndexOf('@');
            if (atIndex <= 0 || atIndex == email.Length - 1)
            {
                return "***";
            }

            var localPartFirstChar = email[0];
            var domain = email[(atIndex + 1)..];
            return $"{localPartFirstChar}***@{domain}";
        }

        private static async Task<string> AcquireTokenAsync(string tenantId, string clientId, string clientSecret)
        {
            var app = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .Build();

            var result = await app
                .AcquireTokenForClient(["https://graph.microsoft.com/.default"])
                .ExecuteAsync();

            return result.AccessToken;
        }
    }
}
