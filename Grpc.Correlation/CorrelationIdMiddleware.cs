using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Context;

namespace Knowit.Grpc.Correlation
{
    internal class CorrelationIdMiddleware
    {
        private static readonly string HeaderName = "X-CorrelationId".ToLower();
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var header = context.Request.Headers[HeaderName];
            if (!Guid.TryParse(header, out var value))
            {
                value = Guid.NewGuid();
            }

            var container = context.RequestServices.GetRequiredService<CorrelationId>();
            container.Value = value;

            using (LogContext.PushProperty("CorrelationId", value))
            {
                await _next(context);
            }
        }
    }
}