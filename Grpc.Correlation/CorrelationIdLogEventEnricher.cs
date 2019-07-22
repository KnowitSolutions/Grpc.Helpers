using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace Grpc.Correlation
{
    public class CorrelationIdLogEventEnricher : ILogEventEnricher
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CorrelationIdLogEventEnricher(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var correlationId = _httpContextAccessor.HttpContext.RequestServices.GetService<CorrelationId>();
            if (correlationId.Value == Guid.Empty) return;

            var property = propertyFactory.CreateProperty("CorrelationId", correlationId.Value);
            logEvent.AddOrUpdateProperty(property);
        }
    }
}