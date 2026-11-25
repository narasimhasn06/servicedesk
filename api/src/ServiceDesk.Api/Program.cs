using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using ServiceDesk.Api.Data;
using ServiceDesk.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Logging: console + one file per day, settings from appsettings.json
builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/servicedesk-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7));

builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<ApiKeyFilter>();            // 401 without a valid X-Api-Key
        options.Filters.Add<FluentValidationFilter>();  // 400 with field-by-field errors
    })
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ServiceDesk")));

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

app.UseExceptionHandler();        // unexpected errors -> safe 500 ProblemDetails
app.UseStatusCodePages();         // empty 404/405 etc. -> ProblemDetails too
app.UseSerilogRequestLogging();   // one log line per request

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);

    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health");   // "Healthy" when the database answers

app.Run();