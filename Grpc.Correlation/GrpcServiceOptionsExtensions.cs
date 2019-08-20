using System;
using Grpc.AspNetCore.Server;

namespace Knowit.Grpc.Correlation
{
    public static class GrpcServiceOptionsExtensions
    {
        public static void AddCorrelationId(this GrpcServiceOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            
            options.Interceptors.Add<CorrelationIdInterceptor>();
        }
    }
}