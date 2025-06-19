using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using BitByBitTrashAPI.Service;




var builder = WebApplication.CreateBuilder(args);

var sqlConnectionString =
    Environment.GetEnvironmentVariable("connectionString") ??
    builder.Configuration.GetValue<string>("connectionString");

var sqlConnectionStringFound = !string.IsNullOrWhiteSpace(sqlConnectionString);

// if (!sqlConnectionStringFound)
// {
//     throw new InvalidOperationException("Database connection string is not configured. Please add 'connectionString'.");
// }

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpClient<IWeatherApiService, WeatherApiService>(client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com/v1/forecast?latitude=51.571915&longitude=4.768323&current=temperature_2m,is_day&timezone=Europe%2FBerlin&forecast_days=1");
});


// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Your API", 
        Version = "v1",
        Description = "Your API Description"
    });
    
    // Add JWT Authentication support in Swagger
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

builder.Services.AddDbContext<LitterDbContext>(options =>
    options.UseSqlServer(sqlConnectionString));


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(sqlConnectionString));

// Configure Identity with Entity Framework stores and API endpoints
builder.Services
    .AddIdentityApiEndpoints<IdentityUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 10;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();



builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<IAuthenticationService, AspNetIdentityAuthenticationService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});


builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5091); // THIS IS WHAT AZURE NEEDS
});

builder.Services.AddScoped<GeocodingService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Enable Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Your API V1");
        c.RoutePrefix = "swagger";
    });
}
app.MapGet("/", () => $"BitByBit project WebAPI is up. Connection string found: {(sqlConnectionStringFound ? "Yes" : "No")}");


app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowAllOrigins");

app.UseAuthentication();
app.UseAuthorization();

// Map Identity API endpoints (this creates /register, /login, etc.)
app.MapGroup("/account").MapIdentityApi<IdentityUser>();

// Custom logout endpoint
app.MapPost("/account/logout",
    async (SignInManager<IdentityUser> signInManager,
    [FromBody] object empty) =>
    {
        if (empty != null)
        {
            await signInManager.SignOutAsync();
            return Results.Ok();
        }
        return Results.Unauthorized();
    })
    .RequireAuthorization();

using (var scope = app.Services.CreateScope())
{
    await SeedRoles.SeedAsync(scope.ServiceProvider);
}


app.MapControllers();

app.Run();

// Make the implicit Program class public so tests can access it
public partial class Program { }
