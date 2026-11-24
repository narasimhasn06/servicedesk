using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using ServiceDesk.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();                                  // generates /openapi/v1.json
builder.Services.AddSingleton<InMemoryStore>();                 // one shared store (Day 2 only)

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                                                // the JSON description
    app.MapScalarApiReference();                                     // the docs UI at /scalar
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();