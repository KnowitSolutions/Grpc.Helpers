using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Knowit.Grpc.Backoff
{
    public static class ServiceCollectionExtensions
    {
        public static void AddBackoff(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<ExponentialBackoffInterceptor>();
        }
    }
}