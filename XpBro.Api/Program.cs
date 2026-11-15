using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using XpBro.Api.Repositories;
using XpBro.Api.Seeders;
using XpBro.Api.Services;

//TODO: Add Serilog Logging
//TODO: Add Seq sink maybe
//TODO: Add separate DI file
//TODO: Add try catch in program.cs with logging
//TODO: models as records
//TODO: remove .netxxx from bin
//TODO: Add separate project for DI for repo

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

ConfigurationManager configuration = builder.Configuration;

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
                .WithOrigins("http://localhost:5173", "https://app.xpbro.com")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();


//
// AUTHENTICATION
//
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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
    });
//TODO: Add Google
    //
    // GOOGLE OAUTH
    //
    //.AddCookie()
    //.AddGoogle(options =>
    //{
    //    options.ClientId = configuration["Google:ClientId"]!;
    //    options.ClientSecret = configuration["Google:ClientSecret"]!;
    //    options.CallbackPath = "/signin-google";
    //    options.SaveTokens = true;
    //});


//
// AUTHORIZATION
//
builder.Services.AddAuthorization();

//
// SERVICES
//
builder.Services.AddScoped<IPasswordService, PasswordService>();

builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

//
// REPOSITORIES
//
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IRoleRepository, RoleRepository>();

builder.Services.AddScoped<ITokenRepository, TokenRepository>();

//
// ADMIN SEEDER
//
builder.Services.AddScoped<IAdminSeederService, AdminSeederService>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
//
// HTTPS
//
// todo app.UseHttpsRedirection();

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

//
// ADMIN SEED
//
using (var scope = app.Services.CreateScope())
{
    IAdminSeederService seeder = scope.ServiceProvider.GetRequiredService<IAdminSeederService>();

    await seeder.Seed();
}

await app.RunAsync();
