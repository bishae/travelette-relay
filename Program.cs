using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Travelette.Relay.Application.Services;
using Travelette.Relay.Data;
using Travelette.Relay.Domain.Repositories;
using Travelette.Relay.Domain.Services;
using Travelette.Relay.Infrastructure.HealthChecks;
using Travelette.Relay.Infrastructure.Repositories;
using Travelette.Relay.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:3000",
                "http://localhost:5175",
                "http://localhost:5176",
                "https://localhost:5173",
                "https://localhost:5174",
                "https://travelette.netlify.app",
                "https://traveletteadmin.netlify.app"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=travelette;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register repositories (Domain interfaces -> Infrastructure implementations)
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();

// Register domain services (Domain interfaces -> Infrastructure implementations)
builder.Services.AddScoped<IPaymentService, StripePaymentService>();

// Register application services (Application interfaces -> Application implementations)
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentApplicationService, PaymentApplicationService>();

// Register infrastructure services
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Configure webhook endpoint to allow raw body reading - must be early in pipeline
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/payments/webhook"))
    {
        context.Request.EnableBuffering();
    }
    await next();
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

// Map health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();
