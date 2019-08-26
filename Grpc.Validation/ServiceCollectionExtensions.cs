using System;
using System.Linq;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Knowit.Grpc.Validation
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add a FluentValidation validator to a service collection. The validator must implement the
        /// <see cref="IValidator{T}"/> interface.
        /// </summary>
        /// <param name="services">the service collection</param>
        /// <typeparam name="TValidator">type of the validator</typeparam>
        /// <returns>the service collection to allow chaining</returns>
        public static IServiceCollection AddValidator<TValidator>(this IServiceCollection services)
            where TValidator : class, IValidator
        {
            var implementationType = typeof(TValidator);
            var interfaceType = implementationType
                                    .GetInterfaces()
                                    .FirstOrDefault(type => type.GetGenericTypeDefinition() == typeof(IValidator<>))
                                ?? throw new InvalidOperationException(
                                    "A validator must implement the generic interface 'FluentValidation.IValidator<>'.");

            services.AddSingleton(interfaceType, implementationType);
            return services;
        }
    }
}
