using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Note.Taking.API.Common.Extensions;
using Note.Taking.API.Infrastructure.Database;
using System.Security.Claims;

namespace Note.Taking.API.Features.Notes
{
    public class GetAllNotes
    {
        public class Request
        {
            public int Page { get; set; }
            public int PageSize { get; set; }
            public string? Search { get; set; }
            public string? Tag { get; set; }
        }

        public record Response(List<NoteItem> Notes, int TotalCount, int Page, int PageSize);

        public record NoteItem(int Id, string Title, string Content, DateTime CreatedAt);

        public sealed class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(x => x.Page).GreaterThan(0);
                RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            }
        }

        public sealed class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapGet("api/notes", Handler)
                   .RequireAuthorization()
                   .WithTags("Notes");
            }
        }

        public static async Task<IResult> Handler(
            int page,
            int pageSize,
            string? search,
            string? tag,
            ClaimsPrincipal user,
            AppDbContext context,
            IValidator<Request> validator,
            ILogger<GetAllNotes> logger,
            CancellationToken cancellationToken)
        {
            var request = new Request
            {
                Page = page,
                PageSize = pageSize,
                Search = search,
                Tag = tag,
            };

            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.BadRequest(validationResult.Errors);
            }

            var userId = int.Parse(user.FindFirst("userId")?.Value!);

            var query = context.Notes
                .AsNoTracking()
                .Where(n => n.UserId == userId && !n.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchTerm = request.Search.ToLower();
                query = query.Where(n =>
                    n.Title.ToLower().Contains(searchTerm) ||
                    n.Content.ToLower().Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(request.Tag))
            {
                query = query
                    .Where(n => n.NoteTags.Any(nt => nt.Tag.Name == request.Tag));
            }

            var total = await query.CountAsync(cancellationToken);

            var notes = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(n => new NoteItem(n.Id, n.Title, n.Content, n.CreatedAt))
                .ToListAsync();

            logger.LogInformation("User {UserId} requested notes. Page: {Page}, Search: {Search}, Tag: {Tag}", userId, request.Page, request.Search, request.Tag);

            return Results.Ok(new Response(notes, total, request.Page, request.PageSize));
        }
    }
}
