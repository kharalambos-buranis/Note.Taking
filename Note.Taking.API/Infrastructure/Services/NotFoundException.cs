using System.Net;

namespace Note.Taking.API.Infrastructure.Services
{
    public class NotFoundException : BaseException
    {
        public NotFoundException(string? message) : base(message)
        {
        }

        public override string Title => "Not found";
        public override HttpStatusCode StatusCode => HttpStatusCode.NotFound;
    }
}
