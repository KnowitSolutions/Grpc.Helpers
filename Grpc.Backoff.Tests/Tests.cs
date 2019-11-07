using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Knowit.Grpc.Testing;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Knowit.Grpc.Backoff.Tests
{
    public class Tests : SideChannelServiceTests<Echo.EchoClient, Service>
    {
        [Test]
        public async Task TestSuccess()
        {
            SideChannel.alwaysFail = false;
            await Client.EmptyAsync(new Empty());
        }
        
        [Test]
        public void TestFailure()
        {
            SideChannel.alwaysFail = true;
            Assert.ThrowsAsync<RpcException>(async () => 
                await Client.EmptyAsync(new Empty()));
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddBackoff();
        }

        protected override void ConfigureGrpcClient(IHttpClientBuilder client) => 
            client.AddExponentialBackoff(3, 1000);
    }

    public class Service : Echo.EchoBase {
        private readonly dynamic _sideChannel;

        public Service(dynamic sideChannel)
        {
            _sideChannel = sideChannel;
        }

        public override Task<Empty> Empty(Empty request, ServerCallContext context)
        {
            if (_sideChannel.alwaysFail) throw new RpcException(new Status(StatusCode.Unavailable, ""));
            
            try
            {
                _sideChannel.calls++;
            }
            catch (RuntimeBinderException)
            {
                _sideChannel.calls = 1;
            }

            switch (_sideChannel.calls)
            {
                case 1:
                    throw new RpcException(new Status(StatusCode.Unavailable, ""));
                case 2:
                    throw new RpcException(new Status(StatusCode.Internal, ""));
                default:
                    return Task.FromResult(request);
            }
        }
    }
}