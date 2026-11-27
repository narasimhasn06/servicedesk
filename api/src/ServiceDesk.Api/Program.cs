using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using ServiceDesk.Api.Data;
using ServiceDesk.Api.Infrastructure;
using Microsoft.Extensions.Options;
using ServiceDesk.Api.Payments;
using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;
using ServiceDesk.Api.Ai;

var builder = WebApplication.CreateBuilder(args);

// Logging: console + one file per day, settings from appsettings.json
builder.Host.UseSerilog((context, services, logger) =>
{
    logger.ReadFrom.Configuration(context.Configuration)
          .ReadFrom.Services(services)
          .Enrich.FromLogContext()
          .WriteTo.Console();

    // Log files only on your laptop; in the cloud, the platform keeps console logs.
    if (context.HostingEnvironment.IsDevelopment())
        logger.WriteTo.File("logs/servicedesk-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7);
});

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

builder.Services.Configure<RazorpayOptions>(builder.Configuration.GetSection("Razorpay"));
builder.Services.AddHttpClient<RazorpayClient>((sp, http) =>
{
    http.BaseAddress = new Uri(sp.GetRequiredService<IOptions<RazorpayOptions>>().Value.BaseUrl);
    http.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("Ai"));
builder.Services.AddChatClient(sp =>
{
    var ai = sp.GetRequiredService<IOptions<AiOptions>>().Value;
    var key = string.IsNullOrWhiteSpace(ai.ApiKey) ? "not-configured" : ai.ApiKey;
    return new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions
        {
            Endpoint = new Uri(ai.Endpoint),            // Gemini's OpenAI-compatible endpoint
            NetworkTimeout = TimeSpan.FromSeconds(30)
        })
        .GetChatClient(ai.Model)
        .AsIChatClient();                               // the provider-neutral interface
});
builder.Services.AddScoped<LeadTriageService>();

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

app.UseExceptionHandler();        // unexpected errors -> safe 500 ProblemDetails
app.UseStatusCodePages();         // empty 404/405 etc. -> ProblemDetails too
app.UseSerilogRequestLogging();   // one log line per request

var seedDemoData = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("SeedDemoData");
if (seedDemoData)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Only your local database is migrated automatically.
    // The live database is migrated as a separate, deliberate step (Day 5, Step 3).
    if (app.Environment.IsDevelopment())
        await db.Database.MigrateAsync();

    await DbSeeder.SeedAsync(db);   // does nothing if customers already exist
}

// Public documentation. Calling the API still requires X-Api-Key.
app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health");   // "Healthy" when the database answers

app.Run();