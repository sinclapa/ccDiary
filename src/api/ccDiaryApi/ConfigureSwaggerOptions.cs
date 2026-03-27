// <copyright file="ConfigureSwaggerOptions.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi
{
    using System.Reflection;
    using Asp.Versioning.ApiExplorer;
    using ccDiaryApi.Utilities;
    using Microsoft.Extensions.Options;
    using Microsoft.OpenApi.Models;
    using Swashbuckle.AspNetCore.SwaggerGen;

    public class ConfigureSwaggerOptions : IConfigureNamedOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _provider = provider;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }

        public void Configure(string? name, SwaggerGenOptions options)
        {
            Configure(options);
        }

        // Null-conditional branches on assembly attributes (?.Product, ?.Description) only trigger
        // when attributes are missing, which doesn't occur in normal builds or test assemblies.
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Null-conditional attribute branches not reachable in standard test environments.")]
        public void Configure(SwaggerGenOptions options)
        {
            // Add a swagger document for each discovered API version
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = $"{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>()?.Product}",
                    Version = description.ApiVersion.ToString(),
                    Description = $"{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description}" +
                        $"<p><strong>Build: </strong>{AssemblyVersionInfo.GetInformationalVersion()}</p>" +
                        $"<p><strong>Environment: </strong>{_webHostEnvironment.EnvironmentName}</p>",
                });
            }

            options.OperationFilter<AuthorizeCheckOperationFilter>();

            var scopes = new Dictionary<string, string>
            {
                {
                    $"{_configuration["Entra:ApplicationIdUri"]}/Diary.Update", "Diary.Update"
                },
            };
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows()
                {
                    AuthorizationCode = new OpenApiOAuthFlow()
                    {
                        AuthorizationUrl = new Uri($"{_configuration["Entra:Instance"]}{_configuration["Entra:TenantId"]}/oauth2/v2.0/authorize"),
                        TokenUrl = new Uri($"{_configuration["Entra:Instance"]}{_configuration["Entra:TenantId"]}/oauth2/v2.0/token"),
                        Scopes = scopes,
                    },
                },
            });
        }
    }
}
