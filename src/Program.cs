using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Define some important constants to initialize tracing with
var serviceName = builder.Configuration["ServiceName"]!;
var serviceVersion = "1.0.0";

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource((c) =>
    {
        c.AddService(serviceName: serviceName, serviceVersion: serviceVersion);
    })
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddSqlClientInstrumentation(options => options.SetDbStatementForText = true)
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
            })
            .AddSource("SayHello")
            .SetResourceBuilder(
                ResourceBuilder
                    .CreateDefault()
                    .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            )
            .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));
    })
    .WithMetrics(b =>
    {
        b.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddMeter("SayHello")
            // .AddEventCountersInstrumentation(c =>
            // {
            //     // https://learn.microsoft.com/en-us/dotnet/core/diagnostics/available-counters?WT.mc_id=DT-MVP-5003978
            //     c.AddEventSources(
            //         "Microsoft.AspNetCore.Hosting",
            //         "System.Net.Http",
            //         "System.Net.Sockets",
            //         "System.Net.NameResolution",
            //         "System.Net.Security",
            //         "Microsoft-AspNetCore-Server-Kestrel" 
            //     );
            // })
            ;
        b.AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
        });
        // The rest of your setup code goes here too
    });

// builder.Logging.ClearProviders();
builder.Logging.AddEventSourceLogger();
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;

    var resBuilder = ResourceBuilder.CreateDefault();
    resBuilder.AddService(serviceName);
    options.SetResourceBuilder(resBuilder);

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
    app.UseDeveloperExceptionPage();
    app.UseStatusCodePages();
}

app.UseHttpsRedirection();

var MyActivitySource = new ActivitySource("SayHello");

app.MapGet(
    "/hello", async ([FromServices] ILogger<Program> logger, [FromServices] IHttpClientFactory httpClientFactory) =>
    {
        // Track work inside of the request
        using var activity = MyActivitySource.StartActivity(name: "Test2");

        activity?.SetTag("foo", 1);
        activity?.SetTag("bar", "Hello, World!");
        activity?.SetTag("baz", new int[] { 1, 2, 3 });


        var client = httpClientFactory.CreateClient();

        var response = await client.GetAsync("http://localhost:5058/backend-hello");
        var name = await response.Content.ReadAsStringAsync();
        activity?.AddEvent(new ActivityEvent($"Said hello to {name}"));
        
        logger.LogInformation("COUCOU PETITE perruche");
        return $"Hello {name}!";
    }
);

app.MapGet(
    "/sql", async ([FromServices] IHttpClientFactory httpClientFactory) =>
    {
        using var activity = MyActivitySource.StartActivity(name: "Test-sql");

        activity?.SetTag("foo", 1);


        var client = httpClientFactory.CreateClient();

        var response = await client.GetAsync("http://localhost:5058/backend-sql");


        var list = await response.Content.ReadFromJsonAsync < List<Friend>>();
        var rng = new Random();

        var idx = rng.Next(0, list.Count);
        activity?.AddEvent(new ActivityEvent($"Picking friend #{idx}"));

        return list[idx].Name;
    }
);
app.Run();
