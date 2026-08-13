// <copyright file="Program.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using ccDiaryApi;
using ccDiaryApi.Authorization;
using ccDiaryApi.Data.Model;
using ccDiaryApi.Data.Storage;
using ccDiaryApi.Endpoints;
using ccDiaryApi.Extensions;
using ccDiaryApi.Health;
using ccDiaryApi.Infrastructure;
using ccDiaryApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using OpenTelemetry.Metrics;
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
    || builder.Environment.IsEnvironment("localcompose")
    || builder.Environment.IsEnvironment("LocalCompose")
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
Program.ValidateStorageConfiguration(builder.Configuration);

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.AddSingleton<ITableStore, TableStore>();
builder.Services.AddSingleton<IBlobStore, BlobStore>();

// Creates the tables and containers, records the running version and seeds the first
// administrator. Throwing here stops the host from starting, which the deployment
// workflow already treats as a failed revision — that is what replaces the old
// pending-migrations health gate.
builder.Services.AddHostedService<StorageBootstrapper>();

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

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("DiaryAdmin", p => p.RequireRole(AppRole.DiaryAdmin.ToString()))
    .AddPolicy("DiaryContributor", p => p.RequireRole(
        AppRole.DiaryAdmin.ToString(),
        AppRole.DiaryContributor.ToString()));

// Add services to the container.
builder.Services.AddScoped<IDiaryService, DiaryService>();

builder.Services.AddScoped<IDiaryEntryService, DiaryEntryService>();

builder.Services.AddScoped<IDiaryArchiveService, DiaryArchiveService>();

builder.Services.AddScoped<IAppInfoService, AppInfoService>();

builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IAccessRequestService, AccessRequestService>();

builder.Services.AddScoped<IGraphService, GraphService>();

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddHttpClient();

builder.Services.AddHttpClient("MapTileProxy", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "ccDiary/1.0 (https://github.com/cookingcode/ccdiary; dear_paul_sinclair@hotmail.com)");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<IMapTileService, MapTileService>();

builder.Services.AddConfigurationDiscoveryClient(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower)));

// Add Steeltoe actuators
builder.Services.AddSingleton<IHealthContributor, StorageHealthContributor>();

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

builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();

builder.Services.AddCcDiaryOpenTelemetry(
    builder.Configuration,
    serviceName: "ccDiaryApi",
    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown");

var app = builder.Build();

// Flush all pending spans and metrics before the container exits.
// ApplicationStopping fires on SIGTERM, giving the batch exporters time to drain.
var hostLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
hostLifetime.ApplicationStopping.Register(() =>
{
    app.Services.GetRequiredService<TracerProvider>().ForceFlush(5000);
    app.Services.GetRequiredService<MeterProvider>().ForceFlush(5000);
});

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

app.UseExceptionHandler(exceptionHandlerApp =>
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync("{\"title\":\"An error occurred.\",\"status\":500}");
    }));

app.UseCors("cors");

app.UseAuthentication();

app.UseObservabilityUserContext();

app.UseAppUserEnrichment();

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

    /// <summary>
    /// Fails startup when storage is not configured, rather than letting the first
    /// request fail with an unrelated error.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <remarks>
    /// A connection string is used locally against Azurite; in Azure only the account
    /// name is set and the Container App's managed identity supplies the credential, so
    /// no secret is stored anywhere.
    /// </remarks>
    internal static void ValidateStorageConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(StorageOptions.SectionName);
        var connectionString = section["ConnectionString"];
        var accountName = section["AccountName"];

        if (string.IsNullOrWhiteSpace(connectionString) && string.IsNullOrWhiteSpace(accountName))
        {
            throw new InvalidOperationException(
                "Storage is not configured. Set Storage:ConnectionString (Azurite) or Storage:AccountName (managed identity).");
        }
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
}
