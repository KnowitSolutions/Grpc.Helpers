using System;
using System.IO.Pipelines;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.CSharp.RuntimeBinder;

namespace Knowit.Kestrel.ProtocolMultiplexing
{
    internal class ProtocolMultiplexingMiddleware
    {
        private const string Http2Preface = "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n";
        private readonly ConnectionDelegate _next;

        public ProtocolMultiplexingMiddleware(ConnectionDelegate next)
        {
            _next = next;
        }

        public async Task OnConnectionAsync(ConnectionContext context)
        {
            var preface = await GetPreface(context.Transport.Input);
            SetProtocols(_next.Target, preface == Http2Preface ? HttpProtocols.Http2 : HttpProtocols.Http1);
            await _next(context);
        }

        private static async Task<string> GetPreface(PipeReader input)
        {
            ReadResult result;
            do
            {
                result = await input.ReadAsync();
                input.AdvanceTo(result.Buffer.Start);
            } while (result.Buffer.Length < 24 || result.IsCompleted);

            var idx = 0;
            var length = Math.Min(result.Buffer.Length, 24);
            var buffer = new byte[length];
            foreach (var slice in result.Buffer.Slice(0, length))
            {
                slice.CopyTo(buffer.AsMemory(idx));
                idx += slice.Length;
            }

            return Encoding.ASCII.GetString(buffer);
        }

        private static void SetProtocols(object target, HttpProtocols protocols)
        {
            var field = target
                .GetType()
                .GetField("_protocols", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new RuntimeBinderException("Couldn't bind to Kestrel's protocol field");
            field.SetValue(target, protocols);
        }
    }
}