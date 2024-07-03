using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using ccDiaryApi.Data.Context;
using ccDiaryApi.Data.Migration;
using ccDiaryApi.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog;
using System.Reflection;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using ccDiaryApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using ccDiaryApi;

var builder = WebApplication.CreateBuilder(args);

var environment = builder.Environment.EnvironmentName;
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .Build();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Debug()
    .WriteTo.Console()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

var connStrBuilder = new SqlConnectionStringBuilder(
    configuration.GetConnectionString("SqlConnection"));

if (!string.IsNullOrEmpty(builder.Configuration["SA_PASSWORD"]))
{
    connStrBuilder.Password = builder.Configuration["SA_PASSWORD"];
}

builder.Services.AddDbContext<DiaryDatabaseContext>(opts =>
    opts.UseSqlServer(connStrBuilder.ConnectionString));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("Entra"));

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
builder.Services.AddScoped<IDiaryService, DiaryService>();
builder.Services.AddScoped<IDiaryEntryService, DiaryEntryService>();

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
            options.SwaggerDoc(description.GroupName, new Microsoft.OpenApi.Models.OpenApiInfo
            {                
                Title = $"{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>()?.Product}",  
                Version = description.ApiVersion.ToString(),
                Description = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description +
                    $"<p><strong>Build: </strong>{Assembly.GetExecutingAssembly().GetName().Version}</p>" +
                    $"<p><strong>Environment: </strong>{environment}</p>"
            });
        }

        options.OperationFilter<AuthorizeCheckOperationFilter>();
        var scopes = new Dictionary<string, string>();
        scopes.Add($"api://{builder.Configuration["Entra:ClientId"]}/Diary.Update", "Diary.Update");
        options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows()
            {
                AuthorizationCode = new OpenApiOAuthFlow()
                {
                    AuthorizationUrl = new Uri($"{builder.Configuration["Entra:Instance"]}{builder.Configuration["Entra:TenantId"]}/oauth2/v2.0/authorize"),
                    TokenUrl = new Uri($"{builder.Configuration["Entra:Instance"]}{builder.Configuration["Entra:TenantId"]}/oauth2/v2.0/token"),
                    Scopes = scopes
                }
            }
        });        
    });

builder.Services.AddCors(p => p.AddPolicy("cors", builder =>
{
    builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
}));
// Add our services

var app = builder.Build();

app.MigrateDatabase();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI();
}

app.UseSwagger();

app.AddSwaggerUI(builder.Configuration);

app.UseHttpsRedirection();

app.UseCors("cors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

#pragma warning disable S1118 // Utility classes should not have public constructors
public partial class Program { }
#pragma warning restore S1118 // Utility classes should not have public constructors
