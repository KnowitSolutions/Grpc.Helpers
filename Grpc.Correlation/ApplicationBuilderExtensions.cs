using Microsoft.AspNetCore.Builder;

namespace Knowit.Grpc.Correlation
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        {
            app.UseMiddleware<CorrelationIdMiddleware>();
            return app;
        }
    }
}