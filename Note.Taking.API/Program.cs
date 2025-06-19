
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Note.Taking.API.Common.Extensions;
using Note.Taking.API.Features.Auth;
using Note.Taking.API.Infrastructure.Database;
using Note.Taking.API.Infrastructure.Services;
using System.Text;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services.AddDbContext<AppDbContext>(
      optionsBuilder =>
      {
          optionsBuilder.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
      }
      );

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddSingleton<TokenProvider>();

builder.Services.AddAuthorization();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters 
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            
        };
    });

builder.Services.AddEndpoints();


builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddValidatorsFromAssemblyContaining<RefreshToken>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Host.UseSerilog((ctx, config) =>
    config.ReadFrom.Configuration(ctx.Configuration));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();


app.UseAuthentication();



//var summaries = new[]
//{
//    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
//};

//app.MapGet("/weatherforecast", () =>
//{
//    var forecast = Enumerable.Range(1, 5).Select(index =>
//        new WeatherForecast
//        (
//            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//            Random.Shared.Next(-20, 55),
//            summaries[Random.Shared.Next(summaries.Length)]
//        ))
//        .ToArray();
//    return forecast;
//});

app.MapEndpoints();

app.Run();

//internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
//{
//    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
//}
