using System;
using System.Collections.Generic;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace Knowit.Grpc.Correlation
{
    public static class HttpClientBuilderExtensions
    {
        public static void AddCorrelationId(this IHttpClientBuilder client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            client.AddInterceptor<CorrelationIdInterceptor>();
        }
    }
}