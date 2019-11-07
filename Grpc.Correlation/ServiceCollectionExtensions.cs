using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Knowit.Grpc.Correlation
{
    public static class ServiceCollectionExtensions
    {
        public static void AddCorrelationId(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            
            services.AddHttpContextAccessor();
            services.TryAddScoped<CorrelationId>();
            services.TryAddSingleton<CorrelationIdInterceptor>();
        }
    }
}