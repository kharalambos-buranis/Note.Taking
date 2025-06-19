using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Note.Taking.API.Common.Extensions;
using Note.Taking.API.Common.Models;
using Note.Taking.API.Infrastructure.Database;

namespace Note.Taking.API.Features.Auth
{
    public class RegisterUser
    {
        public record Request(string Email, string PasswordHash, string FullName);
        public record Response(int id,string Email, string FullName);

        public sealed class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(u => u.Email).NotEmpty().EmailAddress();
                RuleFor(u => u.PasswordHash).NotEmpty().MinimumLength(8).WithMessage("The Passwordhash must contain at least 8 symbols");
                RuleFor(u => u.FullName).NotEmpty();

            }
        }

        public sealed class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("users", Handler).WithTags("Users");
            }
        } 

        public static async Task<IResult> Handler (Request request, AppDbContext context, IValidator<Request> validator,CancellationToken cancellationToken, ILogger<RegisterUser> logger)
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
                logger.LogWarning("Attempt to register with already used email: {Email}", request.Email);
                return Results.BadRequest("Email is already registered.");
            }

            var user = new User { Email = request.Email, PasswordHash = request.PasswordHash, FullName = request.FullName, CreatedAt = DateTime.UtcNow };

            context.Users.Add(user);

            await context.SaveChangesAsync();

            logger.LogInformation("User registered: {Email}", request.Email);

            return Results.Ok(new Response(user.Id,user.Email,user.FullName));
        }
    }
}
