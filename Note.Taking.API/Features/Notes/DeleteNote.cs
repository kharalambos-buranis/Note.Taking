using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Note.Taking.API.Common.Extensions;
using Note.Taking.API.Infrastructure.Database;
using Note.Taking.API.Infrastructure.Services;
using System.Security.Claims;

namespace Note.Taking.API.Features.Notes
{
    public class DeleteNote
    {
        public sealed class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapDelete("api/notes/{id:int}", Handler)
                   .RequireAuthorization()
                   .WithTags("Notes");
            }
        }

        public static async Task<IResult> Handler([FromRoute]int id, ClaimsPrincipal user, AppDbContext context, ILogger<DeleteNote> logger, CancellationToken cancellationToken)
        {
            var userId = int.Parse(user.FindFirst("userId")?.Value!);

            var note = await context.Notes
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsDeleted, cancellationToken);

            if (note is null)
            {
                logger.LogWarning("Delete attempt on nonexistent note {NoteId} by user {UserId}", id, userId);
                throw new NotFoundException($"Note with ID {id} not found.");
            }

            note.IsDeleted = true;
            note.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} soft-deleted note {NoteId}", userId, id);

            return Results.Ok("Note deleted successfully");
        }
    }
}
