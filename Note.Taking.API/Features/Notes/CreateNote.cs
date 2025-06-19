using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Note.Taking.API.Common.Extensions;
using Note.Taking.API.Common.Models;
using Note.Taking.API.Infrastructure.Database;
using System.Security.Claims;

namespace Note.Taking.API.Features.Notes
{
    public class CreateNote
    {
        public record Request(string Title, string Content, List<string> Tags);

        public record Response(int Id, string Title, string Content, DateTime CreatedAt);

        public sealed class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(t => t.Title).NotEmpty();
                RuleFor(t => t.Content).NotEmpty();
            }
        }

        public sealed class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("api/notes", Handler)
                    .WithTags("Notes")
                    .RequireAuthorization();
            }
        }

        public static async Task<IResult> Handler([FromBody] Request request, ClaimsPrincipal user, AppDbContext context, IValidator<Request> validator, ILogger<CreateNote> logger, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                logger.LogWarning("CreateNote validation failed for user. Errors: {@Errors}", validationResult.Errors);
                return Results.BadRequest(validationResult.Errors);
            }

            var userId = int.Parse(user.FindFirst("userId")?.Value!);

            var tagEntities = new List<Tag>();
            var normalizedTagNames = request.Tags?.Select(t => t.Trim().ToLower()).Distinct().ToList() ?? new();

            foreach (var tagName in normalizedTagNames)
            {
                var existingTag = await context.Tags.FirstOrDefaultAsync(
                    t => t.Name.ToLower() == tagName,
                    cancellationToken);

                if (existingTag == null)
                {
                    existingTag = new Tag { Name = tagName };
                    context.Tags.Add(existingTag);
                    logger.LogInformation("Created new tag: {TagName}", tagName);
                }

                tagEntities.Add(existingTag);
            }


            var note = new Note.Taking.API.Common.Models.Note
            {
                Title = request.Title,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                UserId = userId,
                NoteTags = tagEntities.Select(tag => new NoteTag
                {
                    Tag = tag
                }).ToList()
            };

            context.Notes.Add(note);

            await context.SaveChangesAsync();

            logger.LogInformation("User {UserId} created note {NoteId} with tags: {@Tags}", userId, note.Id, normalizedTagNames);

            return Results.Ok(new Response(note.Id, note.Title, note.Content, note.CreatedAt));
        }
    }
}
