using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using XpBro.Api;
using XpBro.Api.Seeders;


//TODO: Fix Login method (email/pass or google) in db for google
//TODO: What is and where is Account Type - Standard in db
//TODO: Add Azure logging maybe
//TODO: Add try catch in program.cs with logging
//TODO: Add advertisment to frontend
//TODO: Add SEO meta tags to frontend and other stuff (see nemestats repo)
//TODO: Add payment system to frontend and backend
//TODO: Add email confirmation system to backend and frontend
//TODO: Add forgot password system to backend and frontend
//TODO: Add email notification system to backend and frontend
//TODO: Add Cloudflare ddos and caching to frontend and backend - free
//TODO: Add Azure Front Door to frontend and backend maybe???? - costs

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

ConfigurationManager configuration = builder.Configuration;

// Add services to the container.
builder.Services.AddProjectServices();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();

//
// CORS
//
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:5173", "https://xpbro.com",
                "https://www.xpbro.com")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

//
// AUTHENTICATION
//
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

        // Temporary cookie for Google OAuth
        options.DefaultSignInScheme = "ExternalCookie";
    })

    //
    // JWT
    //
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],

            ValidAudience = configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
        };
    })

    //
    // GOOGLE OAUTH
    //
    .AddCookie("ExternalCookie", options =>
    {
        options.Cookie.Name = ".XpBro.External";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddGoogle(options =>
    {
        options.ClientId = configuration["Google:ClientId"]!;
        options.ClientSecret = configuration["Google:ClientSecret"]!;
        options.CallbackPath = "/signin-google";
        options.SignInScheme = "ExternalCookie";
        options.SaveTokens = true;
    });

//
// AUTHORIZATION
//
builder.Services.AddAuthorization();

builder.ConfigureSerilog();

WebApplication app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        // Extract IP address (proxy/load balancer through X-Forwarded-For)
        var ip = httpContext.Connection.RemoteIpAddress?.ToString();
        if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            ip = forwardedFor.FirstOrDefault();
        }

        diagnosticContext.Set("ClientIp", ip);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
    };
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//
// CORS
//
app.UseCors("frontend");

//
// AUTH
//
app.UseAuthentication();
app.UseAuthorization();

//
// CONTROLLERS
//
app.MapControllers();

//TODO: Optional seed
//
// ADMIN SEED
//
using (var scope = app.Services.CreateScope())
{
    IAdminSeederService seeder = scope.ServiceProvider.GetRequiredService<IAdminSeederService>();

    await seeder.Seed();
}

await app.RunAsync();
