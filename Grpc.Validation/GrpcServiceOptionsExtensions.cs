using Grpc.AspNetCore.Server;

namespace Grpc.Validation
{
    public static class GrpcServiceOptionsExtensions
    {
        /// <summary>
        ///     Add a validator interceptor that will perform validations on all incoming request messages if a validator
        ///     exists for the message type. Validators can be added by calling
        ///     <see cref="ServiceCollectionExtensions.AddValidator{TValidator}"/>.
        /// </summary>
        /// <param name="serviceOptions">the gRPC service options</param>
        /// <returns>the gRPC service options to allow chaining</returns>
        public static GrpcServiceOptions AddValidationInterceptor(this GrpcServiceOptions serviceOptions)
        {
            serviceOptions.Interceptors.Add<ValidationInterceptor>();
            
            return serviceOptions;
        }
    }
}
