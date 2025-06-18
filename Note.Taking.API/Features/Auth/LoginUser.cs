using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Note.Taking.API.Common.Extensions;
using Note.Taking.API.Common.Models;
using Note.Taking.API.Infrastructure.Database;
using Note.Taking.API.Infrastructure.Services;

namespace Note.Taking.API.Features.Auth
{
    public class LoginUser
    {
        public record Request(string Email, string PasswordHash);

        public record Response(string FullName, string AccessToken, string RefreshToken);

        public sealed class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(u => u.Email).NotEmpty();
                RuleFor(u => u.PasswordHash)
                 .NotEmpty()
                 .MinimumLength(8)
                 .WithMessage("The Passwordhash must contain at least 8 symbols");
            }
        }

        public sealed class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapGet("logineduser", Handler).WithTags("LoginUsers");
            }
        }

        public static async Task<IResult> Handler(
        Request request,
        AppDbContext context,
        IValidator<Request> validator,
        IPasswordHasher<User> passwordHasher,
        TokenProvider token,
        CancellationToken cancellationToken)
        {

            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage);
                return Results.BadRequest(new { Errors = errors });
            }


            var user = await context.Users.FirstOrDefaultAsync(
                u => u.Email == request.Email && u.PasswordHash == request.PasswordHash,
                cancellationToken);

            if (user is null)
            {
                return Results.Unauthorized();
            }


            var accessToken = token.Create(user);
            var refreshToken = Guid.NewGuid().ToString();


            var response = new Response(user.FullName, accessToken, refreshToken);
            return Results.Ok(response);
        }
    }

}
}
