using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using ccDiaryApi.Extensions;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

var environment = builder.Environment.EnvironmentName;
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .Build();

builder.Services.AddApiVersioning(options =>
{
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddMvc() 
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
   options =>  
    {
        var provider = builder.Services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();
        // Add a swagger document for each discovered API version  
        foreach (var description in provider.ApiVersionDescriptions)
        {
            //var x = this.GetType().Name;
            options.SwaggerDoc(description.GroupName, new Microsoft.OpenApi.Models.OpenApiInfo
            {                
                Title = $"{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>()?.Product}",  
                Version = description.ApiVersion.ToString(),
                Description = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description +
                    $"<p><strong>Build: </strong>{Assembly.GetExecutingAssembly().GetName().Version}</p>" +
                    $"<p><strong>Environment: </strong>{environment}</p>"
            });
        }

        
    });

builder.Services.AddCors(p => p.AddPolicy("cors", builder =>
{
    builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
}));
// Add our services

var app = builder.Build();

app.UseSwagger();
app.AddSwaggerUI(configuration);
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("cors");

app.UseAuthorization();

app.MapControllers();

app.Run();
