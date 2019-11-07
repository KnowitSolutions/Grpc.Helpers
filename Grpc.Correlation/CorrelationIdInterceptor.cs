using System;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Knowit.Grpc.Correlation
{
    internal class CorrelationIdInterceptor : Interceptor
    {
        private static readonly string HeaderName = "X-CorrelationId".ToLower();
        private readonly IHttpContextAccessor _context;

        public CorrelationIdInterceptor(IHttpContextAccessor context)
        {
            _context = context;
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            BlockingUnaryCallContinuation<TRequest, TResponse> continuation) =>
            continuation(request, SetHeader(context));

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation) =>
            continuation(request, SetHeader(context));

        private ClientInterceptorContext<TRequest, TResponse> SetHeader<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context)
            where TRequest : class
            where TResponse : class
        {
            var correlationId = _context
                .HttpContext?
                .RequestServices
                .GetService<CorrelationId>()?
                .Value;
            
            if (correlationId == null || correlationId == Guid.Empty)
            {
                correlationId = Guid.NewGuid();
            }

            var headers = context.Options.Headers ?? new Metadata();
            headers.Add(HeaderName, correlationId.ToString());
            
            return new ClientInterceptorContext<TRequest, TResponse>(
                context.Method, context.Host, context.Options.WithHeaders(headers));
        }

        // TODO: All the streaming interceptors
    }
}