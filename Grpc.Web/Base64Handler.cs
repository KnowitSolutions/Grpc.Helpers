using System.IO.Pipelines;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Knowit.Grpc.Web
{
    internal class Base64Handler : DelegatingHandler
    {
        public Base64Handler()
        {
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Content = await Transcode(request.Content, Base64Pipe.Encode);
            var response = await base.SendAsync(request, cancellationToken);
            response.Content = await Transcode(response.Content, Base64Pipe.Decode);
            return response;
        }

        private delegate Task<long> Transcoder(PipeReader input, PipeWriter output);

        private static async Task<HttpContent> Transcode(HttpContent input, Transcoder transcoder)
        {
            var pipe = new Pipe();
            var reader = PipeReader.Create(await input.ReadAsStreamAsync());

            // TODO: Figure out if I can do fire and forget without this pragma
#pragma warning disable 4014
            Task.Run(async () =>
#pragma warning restore 4014
            {
                await transcoder(reader, pipe.Writer);
                reader.Complete();
                pipe.Writer.Complete();
            });

            var output = new StreamContent(pipe.Reader.AsStream());
            foreach (var (key, value) in input.Headers)
            {
                output.Headers.Add(key, value);
            }

            return output;
        }
    }
}