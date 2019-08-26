using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Knowit.Grpc.Validation
{
    internal class ValidationInterceptor : Interceptor
    {
        private readonly Dictionary<Type, IValidator> _validators = new Dictionary<Type, IValidator>();
        private readonly IValidatorFactory _validatorFactory;
        private readonly ILogger<ValidationInterceptor> _logger;

        public ValidationInterceptor(IValidatorFactory validatorFactory, ILogger<ValidationInterceptor> logger)
        {
            _validatorFactory = validatorFactory;
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
            var validator = GetValidator<TRequest>();

            if (validator != null)
            {
                var result = validator.Validate(request);
                if (!result.IsValid)
                {
                    var message = string.Join(" ", result.Errors.Select(err => err.ToString()));
                    throw new RpcException(new Status(StatusCode.InvalidArgument, message), message);
                }
            }
            else
            {
                _logger.LogWarning("No validators for the type '{MessageType}' found.", typeof(TRequest).Name);
            }

            return continuation(request, context);
        }


        /// <summary>
        ///     Looks for a validator for a type in a dictionary. Creates a new validator if not found.
        ///     Returns <c>null</c> if no validator implementation exists for the type.
        ///     The validator instance is cached in a dictionary and reused next call.
        /// </summary>
        /// <typeparam name="TRequest">type of the request</typeparam>
        /// <returns>a validator or null if none exist</returns>
        private IValidator<TRequest>? GetValidator<TRequest>()
        {
            var requestType = typeof(TRequest);
            var validator = _validators.GetValueOrDefault(requestType);
            if (validator == null)
            {
                validator = _validatorFactory.GetValidator<TRequest>();
                if (validator != null)
                {
                    _validators[requestType] = validator;
                }
            }

            return (IValidator<TRequest>?) validator;
        }
    }
}
