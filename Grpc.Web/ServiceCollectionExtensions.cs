using Microsoft.Extensions.DependencyInjection;

namespace Knowit.Grpc.Web
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGrpcWeb(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddSingleton<BinaryTranscoder>();
            services.AddSingleton<Base64Transcoder>();
            return services;
        }
    }
}