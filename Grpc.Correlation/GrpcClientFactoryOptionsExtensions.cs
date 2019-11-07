using System;
using System.Collections.Generic;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace Knowit.Grpc.Correlation
{
    public static class GrpcClientFactoryOptionsExtensions
    {
        public static void AddCorrelationId(this IList<Interceptor> interceptors, IServiceProvider services)
        {
            if (interceptors == null)
            {
                throw new ArgumentNullException(nameof(interceptors));
            }

            interceptors.Add(services.GetRequiredService<CorrelationIdInterceptor>());
        }
    }
}