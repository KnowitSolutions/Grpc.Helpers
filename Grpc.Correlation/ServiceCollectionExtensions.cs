using System;
using Microsoft.Extensions.DependencyInjection;

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
            services.AddScoped<CorrelationId>();
            services.AddSingleton<CorrelationIdInterceptor>();
        }
    }
}