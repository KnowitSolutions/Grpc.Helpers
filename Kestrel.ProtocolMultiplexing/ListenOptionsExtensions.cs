using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Knowit.Kestrel.ProtocolMultiplexing
{
    public static class ListenOptionsExtensions
    {
        public static IConnectionBuilder UseProtocolMultiplexing(this ListenOptions options) =>
            options.Use(next => new ProtocolMultiplexingMiddleware(next).OnConnectionAsync);
    }
}