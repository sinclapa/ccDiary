// <copyright file="Program.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

using System.Globalization;
using Asp.Versioning;
using ccDiaryApi;
using ccDiaryApi.Data.Context;
using ccDiaryApi.Data.Migration;
using ccDiaryApi.Extensions;
using ccDiaryApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Serilog;
using Serilog.Events;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Metrics;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
if (builder.Environment.IsEnvironment("Local") || builder.Environment.IsEnvironment("LocalContainer"))
{
    builder.Configuration.AddUserSecrets<Program>();
}

var environment = builder.Environment.EnvironmentName;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture)
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

Log.Logger.Information($"ASPNETCORE_ENVIRONMENT = {builder.Configuration["ASPNETCORE_ENVIRONMENT"]}");
string? connectionString = builder.Configuration["AZURE_SQL_CONNECTIONSTRING"] ?? builder.Configuration["ConnectionStrings:SqlConnection"];
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("A valid SQL connection string must be provided in configuration.");
}

var connStrBuilder = new SqlConnectionStringBuilder(connectionString);

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

builder.Services.AddScoped<IDiaryArchiveService, DiaryArchiveService>();

builder.Services.AddScoped<IAppInfoService, AppInfoService>();

builder.Services.AddConfigurationDiscoveryClient(builder.Configuration);

builder.Services.AddControllers();

// Add Steeltoe actuators
builder.Services.AddHealthActuator();

builder.Services.AddInfoActuator();

builder.Services.AddMetricsActuator();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

builder.Services.Add(new ServiceDescriptor(typeof(IWebHostEnvironment), builder.Environment));

builder.Services.AddCors(p => p.AddPolicy("cors", builder =>
{
    builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
}));

var app = builder.Build();

if (app.Configuration.GetValue<bool>("RUN_MIGRATIONS", true))
{
    app.MigrateDatabase();
}
else
{
    Log.Logger.Information("Skipping database migration (RUN_MIGRATIONS is not set)");
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownNetworks = { },
    KnownProxies = { },
});

app.UseSwagger();

app.AddSwaggerUI(builder.Configuration);

// Only use HTTPS redirection when not behind a proxy (e.g., true localhost, not Codespaces)
if (!app.Configuration.GetValue<bool>("DisableHttpsRedirection", false))
{
    app.UseHttpsRedirection();
}

app.UseCors("cors");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapAllActuators();

app.Run();

/// <summary>
/// Create partial class to aid unit testing.
/// </summary>
public partial class Program
{
}
