using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Note.Taking.API.Common.Extensions;
using Note.Taking.API.Infrastructure.Database;
using Note.Taking.API.Infrastructure.Services;

namespace Note.Taking.API.Features.Auth
{
    public class RefreshToken
    {
        public record Request(string Email, string RefreshToken);
        public record Response(string FullName, string AccessToken, string RefreshToken);

        public sealed class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(r => r.Email).NotEmpty().EmailAddress();
                RuleFor(r => r.RefreshToken).NotEmpty();
            }
        }

        public sealed class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("/auth/refresh-token", Handler).WithTags("Auth");
            }
        }

        public static async Task<IResult> Handler(Request request, AppDbContext context, IValidator<Request> validator,TokenProvider token, ILogger<RefreshToken> logger, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogWarning("Refresh token validation failed for {Email}", request.Email);
                return Results.BadRequest(validationResult.Errors);
            }

            var user = await context.Users.FirstOrDefaultAsync(
                u => u.Email == request.Email,
                cancellationToken);

            if (user is null)
            {
                logger.LogWarning("Refresh token failed: user not found {Email}", request.Email);
                return Results.Unauthorized();
            } 

            if (string.IsNullOrWhiteSpace(user.StoredRefreshToken) || user.StoredRefreshToken != request.RefreshToken)
            {
                logger.LogWarning("Invalid refresh token attempt for {Email}", request.Email);
                return Results.Problem("Invalid refresh token.", statusCode: StatusCodes.Status401Unauthorized);
            }

            var newAccessToken = token.Create(user);
            var newRefreshToken = Guid.NewGuid().ToString();

            user.StoredAccessToken = newAccessToken;
            user.StoredRefreshToken = newRefreshToken;
          
            context.Users.Update(user);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Refresh token succeeded for {Email}", request.Email);

            return Results.Ok(new Response(user.FullName, newAccessToken, newRefreshToken));
        }
    }
}
