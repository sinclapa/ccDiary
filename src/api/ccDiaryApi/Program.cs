// <copyright file="Program.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Asp.Versioning;
using ccDiaryApi;
using ccDiaryApi.Data.Context;
using ccDiaryApi.Data.Migration;
using ccDiaryApi.Endpoints;
using ccDiaryApi.Extensions;
using ccDiaryApi.Health;
using ccDiaryApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Info;

var builder = WebApplication.CreateBuilder(args);
var startupActivitySource = new ActivitySource("ccDiaryApi.Startup");
builder.Configuration.AddEnvironmentVariables();
if (builder.Environment.IsEnvironment("local"))
{
    // Keep lowercase ASPNETCORE_ENVIRONMENT value while still loading existing Local settings file.
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
}

if (builder.Environment.IsEnvironment("Local")
    || builder.Environment.IsEnvironment("local")
    || builder.Environment.IsEnvironment("LocalContainer")
    || builder.Environment.IsEnvironment("localcontainer"))
{
    builder.Configuration.AddUserSecrets<Program>();
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture)
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .WriteTo.OpenTelemetry(o => OpenTelemetryExtensions.ConfigureSerilogOtelSink(o, builder.Configuration), ignoreEnvironment: true)
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

Log.Logger.Information("ASPNETCORE_ENVIRONMENT = {Environment}", builder.Configuration["ASPNETCORE_ENVIRONMENT"]);
string connectionString = Program.GetRequiredConnectionString(builder.Configuration);

var connStrBuilder = new SqlConnectionStringBuilder(connectionString);

if (!string.IsNullOrEmpty(builder.Configuration["SA_PASSWORD"]))
{
    connStrBuilder.Password = builder.Configuration["SA_PASSWORD"];
}

builder.Services.AddDbContext<DiaryDatabaseContext>(opts =>
    opts.UseSqlServer(connStrBuilder.ConnectionString));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(
                    jwtBearerOptions =>
                    {
                        Program.ConfigureJwtBearer(jwtBearerOptions);
                    },
                    microsoftIdentityOptions => builder.Configuration.Bind("Entra", microsoftIdentityOptions));

builder.Services.AddApiVersioning(options =>
    {
        Program.ConfigureApiVersioning(options);
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        Program.ConfigureApiExplorer(options);
    });

// Add services to the container.
builder.Services.AddScoped<IDiaryService, DiaryService>();

builder.Services.AddScoped<IDiaryEntryService, DiaryEntryService>();

builder.Services.AddScoped<IDiaryArchiveService, DiaryArchiveService>();

builder.Services.AddScoped<IAppInfoService, AppInfoService>();

builder.Services.AddConfigurationDiscoveryClient(builder.Configuration);

builder.Services.AddControllers();

// Add Steeltoe actuators
builder.Services.AddSingleton<IHealthContributor, DatabaseHealthContributor>();

builder.Services.AddHealthActuator();

builder.Services.AddInfoActuator();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

builder.Services.Add(new ServiceDescriptor(typeof(IWebHostEnvironment), builder.Environment));

builder.Services.AddCors(p => p.AddPolicy("cors", builder =>
{
    builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
}));

builder.Services.AddCcDiaryOpenTelemetry(
    builder.Configuration,
    serviceName: "ccDiaryApi",
    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown");

var app = builder.Build();

// Register graceful shutdown for OpenTelemetry batch exporter
var hostLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
hostLifetime.ApplicationStopping.Register(() =>
{
    var tracerProvider = app.Services.GetRequiredService<TracerProvider>();
    tracerProvider.ForceFlush(30000);
    tracerProvider.Dispose();
});

if (app.Configuration.GetValue<bool>("RUN_MIGRATIONS", true))
{
    Program.RunDatabaseMigration(app, startupActivitySource);
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

app.UseRequestCompletionLogging();

app.UseSwagger();

app.AddSwaggerUI(builder.Configuration);

// Only use HTTPS redirection when not behind a proxy (e.g., true localhost, not Codespaces)
if (!app.Configuration.GetValue<bool>("DisableHttpsRedirection", false))
{
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();

app.UseStaticFiles();

app.UseCors("cors");

app.UseAuthentication();

app.UseObservabilityUserContext();

app.UseAuthorization();

app.MapAssemblyInfo();

app.MapControllers();

app.MapAllActuators();

await app.RunAsync();

/// <summary>
/// Create partial class to aid unit testing.
/// </summary>
public partial class Program
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Program"/> class.
    /// Required to satisfy static analysis (S1118); not intended for direct instantiation.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Utility constructor required by S1118; never instantiated directly.")]
    protected Program()
    {
    }

    // Missing connection string causes startup failure; not testable in integration tests
    // because the test factory always provides a connection string via appsettings.
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Startup guard; requires removing all connection string config sources to trigger — not testable in standard integration tests.")]
    internal static string GetRequiredConnectionString(IConfiguration configuration)
    {
        var cs = configuration["AZURE_SQL_CONNECTIONSTRING"] ?? configuration["ConnectionStrings:SqlConnection"];
        if (string.IsNullOrEmpty(cs))
        {
            throw new InvalidOperationException("A valid SQL connection string must be provided in configuration.");
        }

        return cs;
    }

    internal static void ConfigureJwtBearer(JwtBearerOptions jwtBearerOptions)
    {
        AuthenticationLoggingExtensions.ConfigureJwtBearerEvents(jwtBearerOptions);
    }

    internal static void ConfigureApiVersioning(ApiVersioningOptions options)
    {
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    }

    internal static void ConfigureApiExplorer(Asp.Versioning.ApiExplorer.ApiExplorerOptions options)
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    }

    internal static void RunDatabaseMigration(
        WebApplication app,
        ActivitySource activitySource,
        Action<WebApplication>? migrateAction = null)
    {
        var migrationStopwatch = Stopwatch.StartNew();
        using var migrationActivity = activitySource.StartActivity("database.migrate", ActivityKind.Internal);
        migrationActivity?.SetTag("db.operation", "migrate");
        migrationActivity?.SetTag("service.name", "ccDiaryApi");

        (migrateAction ?? (webApp => webApp.MigrateDatabase()))(app);
        migrationStopwatch.Stop();
        migrationActivity?.SetStatus(ActivityStatusCode.Ok);
        migrationActivity?.SetTag("migration.duration.ms", migrationStopwatch.ElapsedMilliseconds);
        Log.Logger.Information("Database migration completed in {MigrationDurationMs}ms", migrationStopwatch.ElapsedMilliseconds);
    }
}
