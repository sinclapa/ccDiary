// <copyright file="TestDiaryVisibility.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApiTest.v1
{
    using ccDiaryApi.Authorization;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// The real <see cref="DiaryVisibility"/>, over an authorization service holding the same
    /// admin and contributor policies as Program.cs, so controller tests exercise the actual rule.
    /// </summary>
    internal static class TestDiaryVisibility
    {
        public static IDiaryVisibility Create()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAuthorizationBuilder()
                .AddPolicy(DiaryVisibility.AdminPolicy, p => p.RequireRole("DiaryAdmin"))
                .AddPolicy(DiaryVisibility.ContributorPolicy, p => p.RequireRole("DiaryAdmin", "DiaryContributor"));
            return new DiaryVisibility(services.BuildServiceProvider().GetRequiredService<IAuthorizationService>());
        }
    }
}
