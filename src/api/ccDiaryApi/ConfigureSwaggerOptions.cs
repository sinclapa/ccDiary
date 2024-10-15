namespace ccDiaryApi
{
    using System.Reflection;
    using Asp.Versioning.ApiExplorer;
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

        public void Configure(SwaggerGenOptions options)
        {
            // Add a swagger document for each discovered API version
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = $"{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>()?.Product}",
                    Version = description.ApiVersion.ToString(),
                    Description = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description +
                        $"<p><strong>Build: </strong>{Assembly.GetExecutingAssembly().GetName().Version}</p>" +
                        $"<p><strong>Environment: </strong>{_webHostEnvironment.EnvironmentName}</p>",
                });
            }

            options.OperationFilter<AuthorizeCheckOperationFilter>();
            var scopes = new Dictionary<string, string>();
            scopes.Add($"{_configuration["Entra:ApplicationIdUri"]}/Diary.Update", "Diary.Update");
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
