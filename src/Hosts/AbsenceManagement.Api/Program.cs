using Absences.Api;
using Common.Api;
using Common.Infrastructure.Database;
using Employees.Api;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Telemetry, health checks and resilience (shared with every future service).
builder.AddServiceDefaults();

// Problem details and JSON settings that every bounded context shares.
builder.Services.AddCommonApi();
builder.Services.AddOpenApi();

// Only has an effect while `dotnet build` generates the OpenAPI document: that starts the host
// without a database, and the bounded contexts below would otherwise refuse to register.
builder.AddPlaceholderConnectionStrings(
    EmployeesBoundedContext.ConnectionStringName,
    AbsencesBoundedContext.ConnectionStringName);

// --- Bounded contexts ----------------------------------------------------
// One line per bounded context. Each one reads its own connection string and registers its own use
// cases, repositories and database initializer. The order is irrelevant: they never call each
// other during registration.
builder.AddEmployeesBoundedContext();
builder.AddAbsencesBoundedContext();
// -------------------------------------------------------------------------

var app = builder.Build();

app.UseCommonApi();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // interactive API documentation at /scalar/v1
    await app.Services.InitializeBoundedContextsAsync(); // migrate and seed every one of them
}

app.MapDefaultEndpoints();

// --- Bounded context routes ----------------------------------------------
app.MapEmployeesBoundedContext();
app.MapAbsencesBoundedContext();
// -------------------------------------------------------------------------

app.Run();
