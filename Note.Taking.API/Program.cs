using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Note.Taking.API.Common.Extensions;
using Note.Taking.API.Common.Models;
using Note.Taking.API.Infrastructure.Database;
using Note.Taking.API.Infrastructure.Middleware;
using Note.Taking.API.Infrastructure.Services;
using Scalar.AspNetCore;
using Serilog;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Host.UseSerilog((ctx, config) =>
    config.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.CustomSchemaIds(t => t.FullName?.Replace('+', '.'));

    options.AddSecurityDefinition("Bearer",
           new OpenApiSecurityScheme
           {
               In = ParameterLocation.Header,
               Type = SecuritySchemeType.Http,
               Name = "Authorization",
               Description = "Please enter access token",
               BearerFormat = "JWT",
               Scheme = "bearer",
           });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
               new OpenApiSecurityScheme
               {
                   Reference = new OpenApiReference
                   {
                       Type=ReferenceType.SecurityScheme,
                       Id="Bearer"
                   }
               },
               Array.Empty<string>()
            }
        });
});

builder.Services.AddDbContext<AppDbContext>(
      optionsBuilder =>
      {
          optionsBuilder.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
      });

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddTransient<GlobalExceptionHandlingMiddleware>();

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

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddEndpoints();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapScalarApiReference();
}

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.MapGet("/health", () => Results.Ok("OK"));

app.MapHealthChecks("/health/db");

app.MapHealthChecks("/health/db", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                error = entry.Value.Exception?.Message
            })
        });

        await context.Response.WriteAsync(result);
    }
});

app.Run();
