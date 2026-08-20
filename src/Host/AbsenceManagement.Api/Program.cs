using Absences.Api;
using Common.Api;
using Common.Infrastructure.Database;
using Employees.Api;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Telemetry, health checks and resilience (shared with every future service).
builder.AddServiceDefaults();

// Problem details and JSON settings that every module shares.
builder.Services.AddCommonApi();
builder.Services.AddOpenApi();

// Only has an effect while `dotnet build` generates the OpenAPI document: that starts the host
// without a database, and the modules below would otherwise refuse to register.
builder.AddPlaceholderConnectionStrings(
    EmployeesModule.ConnectionStringName,
    AbsencesModule.ConnectionStringName);

// --- Modules -------------------------------------------------------------
// One line per module. Each module reads its own connection string and registers its own use
// cases, repositories and database initializer. The order is irrelevant: modules never call each
// other during registration.
builder.AddEmployeesModule();
builder.AddAbsencesModule();
// -------------------------------------------------------------------------

var app = builder.Build();

app.UseCommonApi();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // interactive API documentation at /scalar/v1
    await app.Services.InitializeModulesAsync(); // migrate and seed every module
}

app.MapDefaultEndpoints();

// --- Module routes -------------------------------------------------------
app.MapEmployeesModule();
app.MapAbsencesModule();
// -------------------------------------------------------------------------

app.Run();
