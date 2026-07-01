using SplitDesk.Api.Repositories;
using SplitDesk.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// Allow multipart form uploads (for image scan endpoint)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
});

builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<IBillScanService, BillScanService>();
builder.Services.AddSingleton<IBillRepository, InMemoryBillRepository>();

// CORS — in Docker the nginx proxy makes calls same-origin, so CORS is only
// needed for direct local dev (without Docker).
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",   // Docker nginx
                "http://localhost:5173",   // Vite dev
                "http://localhost:5174",
                builder.Configuration["AllowedOrigins:0"] ?? ""
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");

// Health check endpoint — used by Docker Compose healthcheck
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
