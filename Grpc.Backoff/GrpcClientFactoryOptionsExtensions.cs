using System;
using System.Collections.Generic;
using Grpc.AspNetCore.Server;
using Grpc.Core.Interceptors;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.DependencyInjection;

namespace Knowit.Grpc.Backoff
{
    public static class GrpcClientFactoryOptionsExtensions
    {
        public static void AddExponentialBackoff(
            this IList<Interceptor> interceptors,
            IServiceProvider services,
            int retryCount,
            int retryInterval = 0,
            bool retryForever = false)
        {
            if (interceptors == null)
            {
                throw new ArgumentNullException(nameof(interceptors));
            }

            var interceptor = services.GetRequiredService<ExponentialBackoffInterceptor>();
            interceptor.RetryCount = retryCount;
            interceptor.RetryInterval = retryInterval;
            interceptor.RetryForever = retryForever;
            interceptors.Add(interceptor);
        }
    }
}