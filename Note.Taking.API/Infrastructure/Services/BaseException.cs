using System.Net;

namespace Note.Taking.API.Infrastructure.Services
{
    public abstract class BaseException : Exception
    {
        protected BaseException(string? message) : base(message)
        {
        }

        public abstract string Title { get; }
        public abstract HttpStatusCode StatusCode { get; }
    }
}
