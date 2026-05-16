using System.Text;
using System.Threading.RateLimiting;
using DwBuilder.Core.Interfaces;
using DwBuilder.Infrastructure.Data;
using DwBuilder.Infrastructure.Extensions;
using DwBuilder.Infrastructure.Repositories;
using DwBuilder.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

var builder = WebApplication.CreateBuilder(args);

// Override configuration with environment variables (for production deployment)
var envDbConnection = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
if (!string.IsNullOrEmpty(envDbConnection))
{
    builder.Configuration["ConnectionStrings:DwBuilder"] = envDbConnection;
}

var envJwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
if (!string.IsNullOrEmpty(envJwtKey))
{
    builder.Configuration["Jwt:Key"] = envJwtKey;
}

var envEncryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");
if (!string.IsNullOrEmpty(envEncryptionKey))
{
    builder.Configuration["Encryption:Key"] = envEncryptionKey;
}

var envCorsOrigins = Environment.GetEnvironmentVariable("ALLOWED_CORS_ORIGINS");
if (!string.IsNullOrEmpty(envCorsOrigins))
{
    builder.Configuration["AllowedCorsOrigins"] = envCorsOrigins;
}

// Configure Serilog
var columnOptions = new ColumnOptions();
columnOptions.Store.Remove(StandardColumn.Properties);
columnOptions.Store.Add(StandardColumn.LogEvent);

var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/dwbuilder-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30);

// Il sink MSSqlServer richiede che il database esista già (lo crea EF Core).
// In caso di errore all'avvio (es. DB non ancora inizializzato) si degrada
// gracefully a Console + File, senza crashare l'applicazione.
try
{
    loggerConfig.WriteTo.MSSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DwBuilder"),
        sinkOptions: new MSSqlServerSinkOptions
        {
            TableName = "Logs",
            SchemaName = "_meta",
            AutoCreateSqlTable = true
        },
        columnOptions: columnOptions);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[Serilog] SQL Server sink non disponibile all'avvio: {ex.Message}");
}

Log.Logger = loggerConfig.CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddDbContext<DwBuilderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DwBuilder")));

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<DwBuilderDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] 
    ?? throw new InvalidOperationException("JWT key is not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] 
    ?? throw new InvalidOperationException("JWT issuer is not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] 
    ?? throw new InvalidOperationException("JWT audience is not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var username = context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        
        return RateLimitPartition.GetFixedWindowLimiter(username, _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(1),
            PermitLimit = 100,
            QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
            QueueLimit = 10
        });
    });
    
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new 
        { 
            error = "Too many requests. Please try again later.",
            retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) 
                ? retryAfter.TotalSeconds 
                : 60
        }, cancellationToken: cancellationToken);
    };
});

// Register application services
builder.Services.AddScoped<ISourceRepository, SourceRepository>();
builder.Services.AddScoped<ISourceConnectionService, SourceConnectionService>();
builder.Services.AddScoped<ISourceTableRepository, SourceTableRepository>();
builder.Services.AddScoped<ISourceFieldRepository, SourceFieldRepository>();
builder.Services.AddScoped<IDdlGeneratorService, DdlGeneratorService>();
builder.Services.AddScoped<IBimlGenerator, DwBuilder.Biml.BimlGenerator>();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

// Add controllers
builder.Services.AddControllers();

// Configure CORS
var allowedOrigins = builder.Configuration["AllowedCorsOrigins"]?.Split(',') 
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo { Title = "DW-Builder API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    c.AddSecurityRequirement(_ => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DwBuilderDbContext>("database");

var app = builder.Build();

// Apply EF Core migrations at startup
app.MigrateDatabase();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Security Headers Middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;");
    
    await next();
});

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllers();

try
{
    Log.Information("Starting DW-Builder API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
