using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using ServiceDesk.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();                                  // generates /openapi/v1.json
// builder.Services.AddSingleton<InMemoryStore>();                 // one shared store (Day 2 only)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ServiceDesk")));  // Day 3 only

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Local convenience: apply migrations and seed on startup.
    // On Day 5 the live database is migrated as a separate, deliberate step.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);

    app.MapOpenApi();                                                // the JSON description
    app.MapScalarApiReference();                                     // the docs UI at /scalar
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
