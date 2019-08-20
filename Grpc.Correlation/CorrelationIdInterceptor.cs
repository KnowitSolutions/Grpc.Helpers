using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Serilog.Context;

namespace Knowit.Grpc.Correlation
{
    internal class CorrelationIdInterceptor : Interceptor
    {
        private static readonly string HeaderName = "X-CorrelationId".ToLower();
        private readonly CorrelationId _correlationId;

        public CorrelationIdInterceptor(CorrelationId correlationId)
        {
            _correlationId = correlationId;
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            if (_correlationId.Value == Guid.Empty)
            {
                _correlationId.Value = Guid.NewGuid();
            }

            var headers = context.Options.Headers ?? new Metadata();
            headers.Add(HeaderName, _correlationId.Value.ToString());
            context = new ClientInterceptorContext<TRequest, TResponse>(
                context.Method, context.Host, context.Options.WithHeaders(headers));

            return continuation(request, context);
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            if (_correlationId.Value == Guid.Empty)
            {
                _correlationId.Value = Guid.NewGuid();
            }

            var headers = context.Options.Headers ?? new Metadata();
            headers.Add(HeaderName, _correlationId.Value.ToString());
            context = new ClientInterceptorContext<TRequest, TResponse>(
                context.Method, context.Host, context.Options.WithHeaders(headers));

            return continuation(request, context);
        }

        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            var correlationIdHeader = context
                .RequestHeaders
                .FirstOrDefault(entry => entry.Key == HeaderName)
                ?.Value;

            if (!Guid.TryParse(correlationIdHeader, out var correlationId))
            {
                correlationId = Guid.NewGuid();
            }

            _correlationId.Value = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                return continuation(request, context);
            }
        }

        // TODO: All the streaming interceptors
    }
}