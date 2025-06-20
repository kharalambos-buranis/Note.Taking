﻿using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Note.Taking.API.Common.Extensions;
using Note.Taking.API.Common.Models;
using Note.Taking.API.Infrastructure.Database;
using ServiceStack.Auth;

namespace Note.Taking.API.Features.Auth
{
    public class RegisterUser
    {
        public record Request(string Email, string Password, string FullName);
        public record Response(int Id, string Email, string FullName);

        public sealed class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(u => u.Email).NotEmpty().EmailAddress();
                RuleFor(u => u.Password).NotEmpty().MinimumLength(8).WithMessage("The Password must contain at least 8 symbols");
                RuleFor(u => u.FullName).NotEmpty();
            }
        }

        public sealed class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("api/users", Handler).WithTags("Users");
            }
        }

        public static async Task<IResult> Handler([FromBody] Request request, AppDbContext context, IValidator<Request> validator, CancellationToken cancellationToken, ILogger<RegisterUser> logger, IPasswordHasher<User> passwordHasher)
        {
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return Results.BadRequest(validationResult.Errors);
            }

            var existingUser = await context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (existingUser is not null)
            {
                logger.LogWarning("Attempt to register with already used email {Email}", request.Email);
                return Results.BadRequest("Email is already registered.");
            }

            var user = new User { Email = request.Email, FullName = request.FullName, CreatedAt = DateTime.UtcNow };

            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

            context.Users.Add(user);

            await context.SaveChangesAsync();

            logger.LogInformation("User registered: {Email}", request.Email);

            return Results.Ok(new Response(user.Id, user.Email, user.FullName));
        }
    }
}
