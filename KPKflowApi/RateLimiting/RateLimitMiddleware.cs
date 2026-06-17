using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KPKflowApi.RateLimiting
{
    public class RateLimitMiddleware : ActionFilterAttribute
    {
        private readonly int _limit; // In Numbers

        private readonly TimeSpan _period; // In Minutes

        private static readonly ConcurrentDictionary<string, RateLimitState> _clients
            = new ConcurrentDictionary<string, RateLimitState>();

        public RateLimitMiddleware(int limit, int periodMinutes)
        {
            _limit = limit;
            _period = TimeSpan.FromMinutes(periodMinutes);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var path = context.HttpContext.Request.Path.ToString();

            var key = $"{ip}:{path}";
            var now = DateTime.UtcNow;

            var state = _clients.GetOrAdd(key, _ =>
                new RateLimitState
                {
                    Count = 0,
                    WindowStart = now
                });

            lock (state)
            {
                // Reset window
                if (now - state.WindowStart >= _period)
                {
                    state.WindowStart = now;
                    state.Count = 0;
                }

                // Enforce limit (atomic)
                if (state.Count >= _limit)
                {
                    context.Result = new ObjectResult("Too Many Requests")
                    {
                        StatusCode = (int)HttpStatusCode.TooManyRequests
                    };
                    return;
                }

                state.Count++;
            }
        }

    }

    public sealed class RateLimitState
    {
        public int Count;
        public DateTime WindowStart;
    }
}