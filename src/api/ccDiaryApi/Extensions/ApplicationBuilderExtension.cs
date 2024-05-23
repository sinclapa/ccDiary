using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;

namespace ccDiaryApi.Extensions
{
    public static class ApplicationBuilderExtensions
    {

        public static IApplicationBuilder AddSwaggerUI(this IApplicationBuilder applicationBuilder, IConfigurationRoot configuration)
            => applicationBuilder.UseSwaggerUI(options =>
                {
                    var descriptions = applicationBuilder.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>().ApiVersionDescriptions;
                    foreach (var groupName in descriptions.Select(x => x.GroupName))
                    {
                        options.SwaggerEndpoint($"/swagger/{groupName}/swagger.json",
                            groupName.ToUpperInvariant());
                    }
                    
                    options.DisplayRequestDuration();
                });
    }
}
