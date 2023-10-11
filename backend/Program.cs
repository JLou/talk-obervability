using System.Diagnostics;
using System.Diagnostics.Metrics;
using backend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var serviceName = "backend";
var serviceVersion = "2.3.0";

var meter = new Meter(serviceName);
var counter = meter.CreateCounter<long>("backend.fetched-branches");

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<PdsContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("dbcontext"));
});

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddSqlClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
            })
            .AddSource("serviceName")
            .AddSource("PickName")
            .SetResourceBuilder(
                ResourceBuilder
                    .CreateDefault()
                    .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            )
            .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
    })
    .WithMetrics(meterProviderBuilder =>
    {
        meterProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddMeter(meter.Name)
            .AddHttpClientInstrumentation()
            .AddEventCountersInstrumentation(c =>
            {
                // https://learn.microsoft.com/en-us/dotnet/core/diagnostics/available-counters?WT.mc_id=DT-MVP-5003978
                c.AddEventSources(
                    "Microsoft.AspNetCore.Hosting",
                    "System.Net.Http",
                    "System.Net.Sockets",
                    "System.Net.NameResolution",
                    "System.Net.Security"
                );
            });
        meterProviderBuilder.AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
        });
        // The rest of your setup code goes here too
    });

// builder.Logging.ClearProviders();
builder.Logging.AddEventSourceLogger();
builder.Logging.AddOpenTelemetry(options =>
{
    options.AddOtlpExporter(otlpOptions =>
    {
        // Use IConfiguration directly for Otlp exporter endpoint option.
        otlpOptions.Endpoint = new Uri("http://localhost:4317");
    });
    options.AddConsoleExporter();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

var MyActivitySource = new ActivitySource("PickName");

app.MapGet(
    "/backend-hello",
    (ILogger<Program> logger) =>
    {
        using var activity = MyActivitySource.StartActivity("Pick a name");

        var names = new[] { "Jonathan", "Martin", "Martin 🐼", "JL" };
        var rng = new Random();

        var idx = rng.Next(0, names.Length);

        activity?.AddEvent(new ActivityEvent($"Picked {names[idx]}"));
        logger.LogInformation("[Log] picked {Name}", names[idx]);

        return names[idx];
    }
);

app.MapGet(
    "/backend-sql",
    async ([FromServices] PdsContext db) =>
    {
        using var activity = MyActivitySource.StartActivity("Getting all branches");
        activity?.AddEvent(new ActivityEvent("Fetching all branches via sql"));
        var r = await db.Friends.ToListAsync();

        counter.Add(r.Count);
        return r;
    }
);

app.Run();
