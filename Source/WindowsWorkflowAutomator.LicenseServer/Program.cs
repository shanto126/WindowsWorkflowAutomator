using Microsoft.EntityFrameworkCore;
using WindowsWorkflowAutomator.LicenseServer.Data;
using WindowsWorkflowAutomator.LicenseServer.Services;

var builder = WebApplication.CreateBuilder(args);

var databasePath = Path.Combine(
    builder.Environment.ContentRootPath,
    "license-server.db");

builder.Services.AddDbContext<LicenseDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));
builder.Services.AddScoped<ILicenseValidationService, LicenseValidationService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<AdminQueryService>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<UserDashboardService>();
builder.Services.AddControllers();

var app = builder.Build();
app.Urls.Add("http://localhost:5078");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LicenseDbContext>();
    db.Database.EnsureCreated();
    await DemoDataSeeder.EnsureSchemaAsync(db);
    await DemoDataSeeder.SeedAsync(db);
}

app.MapControllers();
app.Run();