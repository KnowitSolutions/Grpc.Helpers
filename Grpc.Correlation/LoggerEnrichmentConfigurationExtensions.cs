using System;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Configuration;

namespace Grpc.Correlation
{
    public static class LoggerEnrichmentConfigurationExtensions
    {
        public static LoggerConfiguration WithCorrelationId(
            this LoggerEnrichmentConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var enricher = serviceProvider.GetRequiredService<CorrelationIdLogEventEnricher>();
            return configuration.With(enricher);
        }
    }
}