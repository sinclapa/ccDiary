namespace ccDiaryApi.Extensions
{
    using Asp.Versioning.ApiExplorer;

    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder AddSwaggerUI(this IApplicationBuilder applicationBuilder, IConfigurationRoot configuration)
            => applicationBuilder.UseSwaggerUI(options =>
                {
                    var descriptions = applicationBuilder.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>().ApiVersionDescriptions;
                    foreach (var groupName in descriptions.Select(x => x.GroupName))
                    {
                        options.SwaggerEndpoint(
                            $"/swagger/{groupName}/swagger.json",
                            groupName.ToUpperInvariant());
                    }

                    options.DisplayRequestDuration();
                    options.OAuthAppName("ccDiaryAPI Swagger Client");
                    options.OAuthClientId(configuration["Entra:ClientId"]);
                    options.OAuthUsePkce();
                });
    }
}
