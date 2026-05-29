using Lms.EnrollmentService.Infrastructure;
using Lms.EnrollmentService.Infrastructure.Middleware;
using Lms.EnrollmentService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();


/*//try
//{
//    using var scope = app.Services.CreateScope();
//    var dbContext = scope.ServiceProvider.GetRequiredService<EnrollmentDbContext>();

//    if (dbContext.Database.IsRelational())
//        dbContext.Database.Migrate();
//    else
//        dbContext.Database.EnsureCreated();
//}
//catch (Exception ex)
//{
//    Log.Error(ex, "Database migration failed during startup");
//    throw;
//}*/

app.MapGet("/", () => "LMS Enrollment API is running.");

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    environment = app.Environment.EnvironmentName,
    time = DateTimeOffset.UtcNow
}));

app.MapOpenApi("/openapi/{documentName}.json");

app.MapScalarApiReference("/scalar/v1", options =>
{
    options.Title = "LMS Enrollment Service";
    options.Theme = ScalarTheme.Purple;
    options.OpenApiRoutePattern = "/openapi/{documentName}.json";
});

app.UseSerilogRequestLogging();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();