using Microsoft.EntityFrameworkCore;
using Note.Taking.API.Common.Extensions;
using Note.Taking.API.Infrastructure.Database;

namespace Note.Taking.API.Features.Tags
{
    public class GetTags
    {
        public sealed class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapGet("/tags", Handler).WithTags("Tags");
            }
        }

        public static async Task<IResult> Handler(string? search, AppDbContext context, CancellationToken cancellationToken)
        {
            var query = context.Tags.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t => t.Name.ToLower().Contains(search.ToLower()));
            }

            var tags = await query
                .OrderBy(t => t.Name)
                .Select(t => t.Name)
                .ToListAsync(cancellationToken);

            return Results.Ok(tags);
        }
    }
}
