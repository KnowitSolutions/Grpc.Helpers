using System;
using Grpc.AspNetCore.Server;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.DependencyInjection;

namespace Knowit.Grpc.Backoff
{
    public static class HttpClientBuilderExtensions
    {
        public static void AddExponentialBackoff(this IHttpClientBuilder client,  int retryCount, int retryInterval = 0, bool retryForever = false)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            client.AddInterceptor(services =>
            {
                var interceptor = services.GetRequiredService<ExponentialBackoffInterceptor>();
                interceptor.RetryCount = retryCount;
                interceptor.RetryInterval = retryInterval;
                interceptor.RetryForever = retryForever;
                return interceptor;
            });
        }
    }
}