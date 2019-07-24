using Microsoft.Extensions.DependencyInjection;

namespace Grpc.Web
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGrpcWeb(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddTransient<BodyRedirector>();
            services.AddSingleton<BinaryTranscoder>();
            services.AddSingleton<Base64Transcoder>();
            return services;
        }
    }
}