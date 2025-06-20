using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Note.Taking.API.Common.Extensions;
using Note.Taking.API.Infrastructure.Database;
using Note.Taking.API.Infrastructure.Services;
using System.Security.Claims;

namespace Note.Taking.API.Features.Notes
{
    public class GetNoteById
    {
        public record Response(int Id, string Title, string Content, List<string> Tags);

        public sealed class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapGet("api/notes/{id:int}", Handler)
                   .RequireAuthorization()
                   .WithTags("Notes");
            }
        }

        public static async Task<IResult> Handler([FromRoute] int id, ClaimsPrincipal user, AppDbContext context, ILogger<GetNoteById> logger, CancellationToken cancellationToken)
        {
            var userId = int.Parse(user.FindFirst("userId")?.Value!);

            var note = await context.Notes
              .Include(n => n.NoteTags)
              .ThenInclude(nt => nt.Tag)
              .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsDeleted, cancellationToken);

            if (note is null)
            {
                logger.LogWarning("Note {NoteId} not found for user {UserId}", id, userId);
                throw new NotFoundException($"Note with ID {id} not found.");
            }

            logger.LogInformation("User {UserId} fetched note {NoteId}", userId, id);

            return Results.Ok(new Response(note.Id, note.Title, note.Content, note.NoteTags.Select(nt => nt.Tag.Name).ToList()));
        }
    }
}
