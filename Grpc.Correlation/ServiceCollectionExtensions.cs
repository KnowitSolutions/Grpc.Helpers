using System;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace Grpc.Correlation
{
    public static class ServiceCollectionExtensions
    {
        public static void AddCorrelationId(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            
            services.AddScoped<CorrelationId>();
            services.AddScoped<Interceptor, CorrelationIdInterceptor>();
        }
    }
}