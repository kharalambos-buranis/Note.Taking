using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Note.Taking.API.Common.Extensions;
using Note.Taking.API.Infrastructure.Database;
using System.Security.Claims;

namespace Note.Taking.API.Features.Notes
{
    public class UpdateNote
    {
        public record Request(string Title, string Content);

        public record Response(int Id, string Title, string Content, DateTime UpdatedAt);

        public sealed class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(n => n.Title).NotEmpty();
                RuleFor(n => n.Content).NotEmpty();
            }
        }

        public sealed class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPut("api/notes/{id:int}", Handler)
                    .RequireAuthorization()
                    .WithTags("Notes");
            }
        }

        public static async Task<IResult> Handler(
            [FromRoute] int id,
            [FromBody] Request request,
            ClaimsPrincipal user,
            AppDbContext context,
            IValidator<Request> validator,
            ILogger<UpdateNote> logger,
            CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(validationResult.Errors);
            }

            var userId = int.Parse(user.FindFirst("userId")?.Value!);

            var note = await context.Notes
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsDeleted, cancellationToken);

            if (note is null)
            {
                logger.LogWarning("Update attempt on nonexistent note {NoteId} by user {UserId}", id, userId);
                return Results.NotFound(new { Message = "Note not found." });
            }

            note.Title = request.Title;
            note.Content = request.Content;
            note.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} updated note {NoteId}", userId, id);

            return Results.Ok(new Response(note.Id, note.Title, note.Content, note.UpdatedAt));
        }

    }

}
