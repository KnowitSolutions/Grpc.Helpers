using Microsoft.AspNetCore.Builder;

namespace Knowit.Grpc.Web
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGrpcWeb(this IApplicationBuilder app)
        {
            app.UseMiddleware<GrpcWebMiddleware>();
            return app;
        }
    }
}