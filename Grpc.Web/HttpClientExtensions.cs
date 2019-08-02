using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;

namespace Grpc.Web
{
    public static class HttpClientExtensions
    {
        private const byte Uncompressed = 0x00;
        private const byte Compressed = 0x01;
        private const byte Trailers = 0x80;

        public static async Task<TResponse> PostGrpcWebAsync<TRequest, TResponse>(
            this HttpClient client,
            string requestUri,
            TRequest request,
            bool text = true)
            where TRequest : IMessage<TRequest>, new()
            where TResponse : IMessage<TResponse>, new()
        {
            var content = await Encode(request, text);
            var result = await client.PostAsync(requestUri, content);
            var enumerator = Decode<TResponse>(result.Content).GetAsyncEnumerator();

            if (!await enumerator.MoveNextAsync())
            {
                var headers = new Metadata();
                foreach (var (key, values) in result.Headers) headers.Add(key, values.FirstOrDefault());
                
                HandleException(headers);
                throw InternalException("Missing response and exception");
            }

            if (!(enumerator.Current is TResponse message)) throw InternalException("Expected message");
            if (!await enumerator.MoveNextAsync()) throw InternalException("Unexpected end of response"); 
            if (!(enumerator.Current is Metadata trailers)) throw InternalException("Expected trailers");
            if (await enumerator.MoveNextAsync()) throw InternalException("Expected response to end");

            HandleException(trailers);
            return message;
        }

        private static Task<HttpContent> Encode<T>(T message, bool text) where T : IMessage<T>
        {
            var bytes = message.ToByteArray();

            var length = BitConverter.GetBytes((uint) bytes.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(length);

            var data = new[] {new[] {Uncompressed}, length, bytes}
                .SelectMany(x => x)
                .ToArray();

            var content = new ByteArrayContent(data);
            content.Headers.ContentType = new MediaTypeHeaderValue(
                text ? "application/grpc-web-text+protobuf" : "application/grpc-web+protobuf");
            return Task.FromResult<HttpContent>(content);
        }

        private static async IAsyncEnumerable<object> Decode<T>(HttpContent content)
            where T : IMessage<T>, new()
        {
            var input = PipeReader.Create(await content.ReadAsStreamAsync());

            while (true)
            {
                var result = await input.ReadAsync();
                if (result.Buffer.Length == 0)
                {
                    input.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                    if (result.IsCompleted) break;
                }

                if (result.Buffer.Length < 5)
                {
                    input.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                    if (result.IsCompleted) throw InternalException("Incomplete message");
                    continue;
                }

                var headerBuffer = result.Buffer.Slice(0, 5);
                var headerData = headerBuffer.ToArray();
                var (isCompressed, isTrailers, length) = DecodeMeta(headerData);
                if (isCompressed) throw new NotImplementedException();
                
                if (result.Buffer.Length < length + 5)
                {
                    input.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                    if (result.IsCompleted) throw InternalException("Incomplete message");
                    continue;
                }

                var messageBuffer = result.Buffer.Slice(5, length);
                var messageData = messageBuffer.ToArray();
                input.AdvanceTo(messageBuffer.End);
                
                if (isTrailers) yield return DecodeTrailers(messageData);
                else yield return DecodeMessage<T>(messageData);
            }
        }

        private static (bool, bool, uint) DecodeMeta(byte[] data)
        {
            var isCompressed = Convert.ToBoolean(data[0] & Compressed);
            var isTrailers = Convert.ToBoolean(data[0] & Trailers);
            if (BitConverter.IsLittleEndian) Array.Reverse(data, 1, 4);
            var length = BitConverter.ToUInt32(data[1..5]);
            
            return (isCompressed, isTrailers, length);
        }

        private static T DecodeMessage<T>(byte[] data) where T : IMessage<T>, new()
        {
            var message = new T();
            message.MergeFrom(data);
            return message;
        }

        private static Metadata DecodeTrailers(byte[] data)
        {
            var trailers = new Metadata();
            
            var entries = Encoding.ASCII
                .GetString(data)
                .Trim()
                .Split("\r\n");
            
            foreach (var entry in entries)
            {
                var split = entry.Split(": ", 2);
                if (split.Length == 2) trailers.Add(split[0], split[1]);
            }

            return trailers;
        }

        private static RpcException InternalException(string message) =>
            new RpcException(new Status(StatusCode.Internal, message));

        private static void HandleException(Metadata trailers)
        {
            var statusCodeString = trailers.FirstOrDefault(x => x.Key == "grpc-status")?.Value;
            if (string.IsNullOrWhiteSpace(statusCodeString) || !int.TryParse(statusCodeString, out var statusCode))
            {
                throw new RpcException(new Status(StatusCode.Internal, "Missing status code"));
            }

            if (statusCode == 0) return;

            var statusMessage = trailers.FirstOrDefault(x => x.Key == "grpc-message")?.Value ?? "";
            throw new RpcException(new Status((StatusCode) statusCode, statusMessage), trailers);
        }
    }
}