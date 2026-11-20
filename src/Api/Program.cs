using System.Text;
using Api.Errors;
using Application.Auth.UseCases;
using Application.Courts.UseCases;
using Application.Reservations.UseCases;
using Infrastructure;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("CourtBooking")
    ?? throw new InvalidOperationException("Falta la cadena de conexion 'CourtBooking'");

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Falta la seccion Jwt en la configuracion");


if (Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
    throw new InvalidOperationException("Jwt:key falta o es muy corta");

var r2Options = builder.Configuration.GetSection(R2Options.SectionName).Get<R2Options>()
    ?? throw new InvalidOperationException("Falta la seccion R2 en la configuracion");

if (string.IsNullOrWhiteSpace(r2Options.ServiceUrl) ||
    string.IsNullOrWhiteSpace(r2Options.AccessKeyId) ||
    string.IsNullOrWhiteSpace(r2Options.SecretAccessKey) ||
    string.IsNullOrWhiteSpace(r2Options.BucketName))
    throw new InvalidOperationException("Faltan variables de r2Options");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuer = true,
          ValidIssuer = jwtOptions.Issuer,

          ValidateAudience = true,
          ValidAudience = jwtOptions.Audience,

          ValidateLifetime = true,

          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),

          NameClaimType = "sub",
          RoleClaimType = "role"  
        };
    });

builder.Services.AddInfrastructure(connectionString, jwtOptions, r2Options);
builder.Services.AddScoped<CreateCourt>();
builder.Services.AddScoped<CreateReservation>();
builder.Services.AddScoped<CancelReservation>();
builder.Services.AddScoped<RegisterUser>();
builder.Services.AddScoped<LoginUser>();
builder.Services.AddScoped<GetAvailability>();
builder.Services.AddScoped<UploadCourtImage>();
builder.Services.AddScoped<GetImages>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();

var app = builder.Build();

await DatabaseMigrator.MigrateAsync(app.Services);

await IdentitySeeder.SeedRolesAsync(app.Services);
await IdentitySeeder.SeedAdminAsybc(
    app.Services,
    app.Configuration["Admin:Email"],
    app.Configuration["Admin:Password"]);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
