// <copyright file="TestAuthHandler.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest
{
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Primitives;

    public class TestAuthHandler : AuthenticationHandler<TestAuthHandlerOptions>
    {
        public const string UserId = "UserId";

        public const string AuthenticationScheme = "Test";

        public const string UserRole = "X-Test-Role";

        public const string UserEmail = "X-Test-Email";

        public const string NoAuth = "X-Test-No-Auth";

        private readonly string _defaultUserId;

        public TestAuthHandler(
            IOptionsMonitor<TestAuthHandlerOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
            _defaultUserId = options.CurrentValue.DefaultUserId;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Context.Request.Headers.ContainsKey(NoAuth))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim> { new (ClaimTypes.Name, "Test user") };

            // Extract User ID from the request headers if it exists,
            // otherwise use the default User ID from the options.
            var oid = _defaultUserId;
            if (Context.Request.Headers.TryGetValue(UserId, out var userId))
            {
                var headerUserId = userId[0];
                if (headerUserId != null)
                {
                    oid = headerUserId;
                }
            }

            claims.Add(new Claim(ClaimTypes.NameIdentifier, oid));
            claims.Add(new Claim("oid", oid));

            // Extract email from the X-Test-Email header if present
            if (Context.Request.Headers.TryGetValue(UserEmail, out var emailHeader))
            {
                var email = emailHeader[0];
                if (!string.IsNullOrEmpty(email))
                {
                    claims.Add(new Claim("preferred_username", email));
                }
            }

            // Extract role from the X-Test-Role header if present
            if (Context.Request.Headers.TryGetValue(UserRole, out var roleHeader))
            {
                var role = roleHeader[0];
                if (!string.IsNullOrEmpty(role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            var identity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

            var result = AuthenticateResult.Success(ticket);

            return Task.FromResult(result);
        }
    }
}
