using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Grpc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Grpc.Correlation.Tests
{
    public class Tests : SideChannelServiceTests<Echo.EchoClient, Service>
    {
        private Guid CorrelationId => Services.GetService<CorrelationId>().Value;

        [Test]
        public void TestBlockingInterception()
        {
            Assert.AreEqual(Guid.Empty, CorrelationId);
            Client.Empty(new Empty());
            Assert.AreEqual(SideChannel.correlationId, CorrelationId);
        }

        [Test]
        public async Task TestAsyncInterception()
        {
            Assert.AreEqual(Guid.Empty, CorrelationId);
            await Client.EmptyAsync(new Empty());
            Assert.AreEqual(SideChannel.correlationId, CorrelationId);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddCorrelationId();
        }

        protected override void ConfigureGrpc(GrpcServiceOptions options)
        {
            options.AddCorrelationId();
        }
    }
    
    public class Service : Echo.EchoBase
    {
        private readonly CorrelationId _correlationId;
        private readonly dynamic _sideChannel;
        
        public Service(CorrelationId correlationId, dynamic sideChannel)
        {
            _correlationId = correlationId;
            _sideChannel = sideChannel;
        }

        public override Task<Empty> Empty(Empty request, ServerCallContext context)
        {
            _sideChannel.correlationId = _correlationId.Value;
            return Task.FromResult(request);
        }
    }
}