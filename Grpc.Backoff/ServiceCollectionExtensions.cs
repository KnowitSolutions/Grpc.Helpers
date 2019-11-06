using System;
using Knowit.Grpc.Backoff;
using Microsoft.Extensions.DependencyInjection;

namespace Knowit.Grpc.Correlation
{
    public static class ServiceCollectionExtensions
    {
        public static void AddBackoff(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<ExponentialBackoffInterceptor>();
        }
    }
}