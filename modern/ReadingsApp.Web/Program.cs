using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.EntityFrameworkCore;
using ReadingsApp.Data;
using ReadingsApp.Services;
using ReadingsApp.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Passwordless: the connection string uses "Authentication=Active Directory Default"
// (DefaultAzureCredential), so the managed identity signs in on Azure and your az login signs in locally.
builder.Services.AddDbContext<ReadingsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ReadingsDb")));
builder.Services.AddScoped<IReadingRepository, ReadingRepository>();
builder.Services.AddScoped<IReadingService, ReadingService>();
builder.Services.AddControllersWithViews();

// /health returns 200 only when the database answers. App Service uses it to pull a bad instance.
builder.Services.AddHealthChecks().AddDbContextCheck<ReadingsDbContext>();

// Requests, dependencies (SQL), logs and exceptions to Application Insights through OpenTelemetry.
// App Service supplies APPLICATIONINSIGHTS_CONNECTION_STRING; without it (local runs) telemetry is off.
if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
    builder.Services.AddOpenTelemetry().UseAzureMonitor();

var app = builder.Build();
app.UseStaticFiles();
app.MapHealthChecks("/health");
app.MapControllerRoute("default", "{controller=Readings}/{action=Index}/{id?}");
app.Run();
