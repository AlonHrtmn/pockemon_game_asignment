using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using pokemon_backend.Data;
using pokemon_backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure dynamic port binding for cloud deployment
var port = Environment.GetEnvironmentVariable("PORT") ?? "5088";
builder.WebHost.UseUrls($"http://*:{port}");

// 1. Add Controllers support
builder.Services.AddControllers();

// 2. Configure DB Provider dynamically (SQLite fallback if MSSQL is not reachable/selected)
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "Sqlite";
if (dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnection")));
}
else if (dbProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("SupabaseConnection")));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection")));
}

// 3. Register Services for Dependency Injection
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPokemonService, PokemonService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IAiCoachService, AiCoachService>();

// 4. Configure JWT Bearer Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var keyString = jwtSettings.GetValue<string>("Key") ?? "DefaultSuperSecretKeyThatIsTooShortAndShouldBeReplaced!";
var key = Encoding.UTF8.GetBytes(keyString);

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
        ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
        ValidAudience = jwtSettings.GetValue<string>("Audience"),
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

// 5. Configure CORS for Angular Frontend integration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://pokemon-trainer-portal.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 6. Configure Swagger with JWT Bearer security schemes
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Pokemon Trainer API", Version = "v1" });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 7. Ensure Database migrations/creation are executed on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        
        // This will create the database and tables if they don't exist
        context.Database.EnsureCreated();
        
        // Optionally pre-populate the Pokemon Cache on startup for maximum speed
        var pokemonService = services.GetRequiredService<IPokemonService>();
        _ = Task.Run(async () =>
        {
            try
            {
                // Trigger an initial request to cache standard icons and entries in the background
                await pokemonService.GetAllPokemonsAsync();
            }
            catch {}
        });
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while setting up the database.");
    }
}

// 8. Configure HTTP Request Pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pokemon Trainer API v1");
});

app.UseCors("AllowAngular");

// Authentication must run before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
