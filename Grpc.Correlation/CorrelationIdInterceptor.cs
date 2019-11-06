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
            BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var correlationId = _context.HttpContext.RequestServices.GetRequiredService<CorrelationId>();

            if (correlationId.Value == Guid.Empty)
            {
                correlationId.Value = Guid.NewGuid();
            }

            var headers = context.Options.Headers ?? new Metadata();
            headers.Add(HeaderName, correlationId.Value.ToString());
            context = new ClientInterceptorContext<TRequest, TResponse>(
                context.Method, context.Host, context.Options.WithHeaders(headers));

            return continuation(request, context);
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var correlationId = _context.HttpContext.RequestServices.GetRequiredService<CorrelationId>();

            if (correlationId.Value == Guid.Empty)
            {
                correlationId.Value = Guid.NewGuid();
            }

            var headers = context.Options.Headers ?? new Metadata();
            headers.Add(HeaderName, correlationId.Value.ToString());
            context = new ClientInterceptorContext<TRequest, TResponse>(
                context.Method, context.Host, context.Options.WithHeaders(headers));

            return continuation(request, context);
        }

        // TODO: All the streaming interceptors
    }
}