using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using StockPulse.WebAPI.BackgroundServices;
using StockPulse.WebAPI.Context;
using StockPulse.WebAPI.Helpers;
using StockPulse.WebAPI.Hubs;
using StockPulse.WebAPI.Interfaces;
using StockPulse.WebAPI.Middlewares;
using StockPulse.WebAPI.Models.Settings;
using StockPulse.WebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

builder.Services.AddOptions<DbSettings>().BindConfiguration("Db").ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<RedisSettings>().BindConfiguration("Redis").ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<ClientSettings>().BindConfiguration("Client").ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<DemoUserSettings>().BindConfiguration("DemoUser").ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<JwtSettings>().BindConfiguration("Jwt").ValidateDataAnnotations().ValidateOnStart();

builder.Services.AddSingleton<DapperContext>();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisSettings = builder.Configuration.GetSection("Redis").Get<RedisSettings>() ?? throw new InvalidOperationException("RedisSettings configuration is missing.");
    var options = ConfigurationOptions.Parse(redisSettings.ConnectionString);
    options.AbortOnConnectFail = false;
    options.ConnectTimeout = 5000;

    return ConnectionMultiplexer.Connect(options);
});

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IStockService, StockService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<StockSimulatorWorker>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<StockSimulatorWorker>());
builder.Services.AddHostedService<DatabaseHostedService>();
builder.Services.AddHostedService<StockPersistenceWorker>();
builder.Services.AddHostedService<RedisRouteToSignalRWorker>();

builder.Services.AddControllers();

builder.Services.AddSignalR();

builder.Services.AddHealthChecks();

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? throw new InvalidOperationException("JwtSettings configuration is missing.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var rsaKey = RsaKeyLoader.LoadPublicKey(jwtSettings.PublicKeyPath);
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = rsaKey,

            NameClaimType = JwtRegisteredClaimNames.Email
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var path = context.HttpContext.Request.Path;
                if (path.StartsWithSegments("/stockHub"))
                {
                    var accessToken = context.Request.Query["access_token"];

                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

var clientSettings = builder.Configuration.GetSection("Client").Get<ClientSettings>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        if (clientSettings != null && !string.IsNullOrEmpty(clientSettings.BaseUrl))
        {
            policy.WithOrigins(clientSettings.BaseUrl)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseRouting();

app.UseCors("CorsPolicy");

app.UseAuthentication();

app.UseMiddleware<RequestMetadataMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

app.UseAuthorization();

app.MapHub<StockHub>("/stockHub");
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
