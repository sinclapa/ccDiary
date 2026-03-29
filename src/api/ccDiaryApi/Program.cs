// <copyright file="Program.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

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
using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Info;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
if (builder.Environment.IsEnvironment("Local") || builder.Environment.IsEnvironment("LocalContainer"))
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
    .WriteTo.OpenTelemetry(o =>
    {
        var endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrEmpty(endpoint))
        {
            o.Endpoint = endpoint.TrimEnd('/') + "/v1/logs";
            o.Protocol = OtlpProtocol.HttpProtobuf;
            o.ResourceAttributes["service.name"] = "ccDiaryApi";
            var headers = builder.Configuration["OTEL_EXPORTER_OTLP_HEADERS"];
            if (!string.IsNullOrEmpty(headers))
            {
                foreach (var part in headers.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var idx = part.IndexOf('=', StringComparison.Ordinal);
                    if (idx > 0)
                    {
                        o.Headers.Add(part[..idx].Trim(), part[(idx + 1)..].Trim());
                    }
                }
            }
        }
    }, ignoreEnvironment: true)
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

app.UseDefaultFiles();

app.UseStaticFiles();

app.UseCors("cors");

app.UseAuthentication();

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
}
