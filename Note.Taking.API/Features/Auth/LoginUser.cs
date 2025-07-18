﻿using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Note.Taking.API.Common.Extensions;
using Note.Taking.API.Common.Models;
using Note.Taking.API.Infrastructure.Database;
using Note.Taking.API.Infrastructure.Services;

namespace Note.Taking.API.Features.Auth
{
    public class LoginUser
    {
        public record Request(string Email, string Password);

        public record Response(string FullName, string AccessToken, string RefreshToken);

        public sealed class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(u => u.Email).NotEmpty();
                RuleFor(u => u.Password)
                 .NotEmpty()
                 .MinimumLength(8)
                 .WithMessage("The Passwordhash must contain at least 8 symbols");
            }
        }

        public sealed class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("api/auth/login", Handler).WithTags("Auth");
            }
        }

        public static async Task<IResult> Handler(
            [FromBody] Request request,
            AppDbContext context,
            IValidator<Request> validator,
            IPasswordHasher<User> passwordHasher,
            TokenProvider token,
            CancellationToken cancellationToken,
            ILogger<LoginUser> logger)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogWarning("Invalid login input for {Email}", request.Email);
                return Results.BadRequest(validationResult.Errors);
            }

            var user = await context.Users.FirstOrDefaultAsync(
                u => u.Email == request.Email,
                cancellationToken);

            if (user is null)
            {
                logger.LogWarning("Login failed. Email not found: {Email}", request.Email);
                return Results.Unauthorized();
            }

            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (result != PasswordVerificationResult.Success)
            {
                logger.LogWarning("Login failed: invalid password - {Email}", request.Email);
                return Results.Unauthorized();
            }

            var accessToken = token.Create(user);
            var refreshToken = Guid.NewGuid().ToString();

            user.StoredAccessToken = accessToken;
            user.StoredRefreshToken = refreshToken;

            await context.SaveChangesAsync();

            logger.LogInformation("User {Email} logged in successfully", request.Email);

            return Results.Ok(new Response(user.FullName, accessToken, refreshToken));
        }
    }

}

