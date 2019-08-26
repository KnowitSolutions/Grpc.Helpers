using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Knowit.Grpc.Validation
{
    internal class ValidationInterceptor : Interceptor
    {
        private readonly Dictionary<Type, IValidator> _validators = new Dictionary<Type, IValidator>();
        private readonly IServiceProvider _services;
        private readonly ILogger<ValidationInterceptor> _logger;

        public ValidationInterceptor(IServiceProvider services, ILogger<ValidationInterceptor> logger)
        {
            _services = services;
            _logger = logger;
        }

        /// <summary>
        ///     Intercepts all RPC requests and performs validations if a validator exists for the request message type.
        ///     Any validators must be added to the current service collection to allow for dependency injection.
        ///     <see cref="ServiceCollectionExtensions.AddValidator{TValidator}"/>
        /// </summary>
        /// <exception cref="RpcException">
        ///     If any validation fails, this exception is thrown with status code <see cref="StatusCode.InvalidArgument" />
        ///     and the error messages from the validation.
        /// </exception>
        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            var validator = _services.GetService<IValidator<TRequest>>();
            if (validator == null)
            {
                _logger.LogWarning("No validators for the type '{MessageType}' found.", typeof(TRequest).Name);
                return continuation(request, context);
            }

            var result = validator.Validate(request);
            if (result.IsValid) return continuation(request, context);
            
            var message = string.Join(" ", result.Errors.Select(err => err.ToString()));
            throw new RpcException(new Status(StatusCode.InvalidArgument, message));
        }
    }
}
