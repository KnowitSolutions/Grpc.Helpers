using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Knowit.Grpc.Backoff
{
    internal class ExponentialBackoffInterceptor : Interceptor
    {
        private readonly ILogger<ExponentialBackoffInterceptor> _logger;
        private readonly Random _random = new Random();


        internal int RetryInterval { private get; set; }
        internal int RetryCount { private get; set; }
        internal bool RetryForever { private get; set; }

        public ExponentialBackoffInterceptor(ILogger<ExponentialBackoffInterceptor> logger)
        {
            _logger = logger;
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            AsyncUnaryCall<TResponse> call = null;
            var headers = new TaskCompletionSource<Metadata>();
            var task = RetryWrapper(() => call = continuation(request, context), headers);
            
            return new AsyncUnaryCall<TResponse>(
                task, headers.Task, 
                () => call.GetStatus(), 
                () => call.GetTrailers(), 
                () => call.Dispose());
        }
        
        private static int Pow(int @base, int exponent) => 
            exponent == 1 ? @base : @base * Pow(@base, exponent - 1);

        private async Task<TResponse> RetryWrapper<TResponse>(
            Func<AsyncUnaryCall<TResponse>> continuation,
            TaskCompletionSource<Metadata> headers)
        {
            var attempt = 0;
            while (true)
            {
                var call = continuation();

                try
                {
                    var result = await call.ResponseAsync;
                    headers.SetResult(await call.ResponseHeadersAsync);
                    return result;
                }
                catch (RpcException exception) when (
                    exception.StatusCode == StatusCode.Internal ||
                    exception.StatusCode == StatusCode.Unavailable)
                {
                    _logger.LogWarning(exception, "");
                    call.Dispose();

                    attempt++;
                    if (RetryForever) attempt = Math.Min(RetryCount, attempt);
                    else if (attempt >= RetryCount) throw;
                }
                
                var backoff =  _random.Next(Pow(2, attempt));
                var sleep = RetryInterval * backoff;
                await Task.Delay(sleep);
            }
        }
    }
}