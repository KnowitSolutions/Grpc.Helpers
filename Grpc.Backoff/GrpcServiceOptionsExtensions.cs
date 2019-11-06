using System;
using Grpc.AspNetCore.Server;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.DependencyInjection;

namespace Knowit.Grpc.Backoff
{
    public static class GrpcServiceOptionsExtensions
    {
        public static void AddExponentialBackoff(this IHttpClientBuilder client, int retryInterval, int retryCount, bool retryForever = false)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            client.AddInterceptor(services =>
            {
                var interceptor = services.GetRequiredService<ExponentialBackoffInterceptor>();
                interceptor.RetryInterval = retryInterval;
                interceptor.RetryCount = retryCount;
                interceptor.RetryForever = retryForever;
                return interceptor;
            });
            client.AddInterceptor<ExponentialBackoffInterceptor>();
        }
    }
}