using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Knowit.Grpc.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Knowit.Grpc.Correlation.Tests
{
    public class Tests : SideChannelServiceTests<Echo.EchoClient, Service>, ILogEventSink
    {
        private List<LogEvent> _logEvents;

        [Test]
        public void TestBlockingInterception()
        {
            Client.Empty(new Empty());
            AssertPopulated();
        }

        [Test]
        public async Task TestAsyncInterception()
        {
            await Client.EmptyAsync(new Empty());
            AssertPopulated();
        }

        private void AssertPopulated()
        {
            var logEvent = _logEvents.FirstOrDefault(@event => @event.MessageTemplate.Text == "Message");
            Assert.NotNull(logEvent);
            
            Assert.Contains("CorrelationId", logEvent.Properties.Keys.ToList());
            Assert.IsInstanceOf<ScalarValue>(logEvent.Properties["CorrelationId"]);
            
            var property = logEvent.Properties["CorrelationId"] as ScalarValue;
            Assert.AreEqual(SideChannel.correlationId, property?.Value);
        }

        [SetUp]
        public void Setup() => _logEvents = new List<LogEvent>();
        
        public void Emit(LogEvent logEvent) => _logEvents.Add(logEvent);

        protected override void ConfigureHost(IHostBuilder host) => host
            .UseSerilog((context, configuration) => configuration
                .Enrich.FromLogContext()
                .WriteTo.Sink(this));

        protected override void Configure(IApplicationBuilder app)
        {
            app.UseCorrelationId();
            base.Configure(app);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddCorrelationId();
        }

        protected override void ConfigureGrpcClient(IHttpClientBuilder client) =>
            client.AddCorrelationId();
    }

    public class Service : Echo.EchoBase
    {
        private readonly CorrelationId _correlationId;
        private readonly ILogger<Service> _logger;
        private readonly dynamic _sideChannel;

        public Service(CorrelationId correlationId, ILogger<Service> logger, dynamic sideChannel)
        {
            _correlationId = correlationId;
            _logger = logger;
            _sideChannel = sideChannel;
        }

        public override Task<Empty> Empty(Empty request, ServerCallContext context)
        {
            _sideChannel.correlationId = _correlationId.Value;
            _logger.LogInformation("Message");
            return Task.FromResult(request);
        }
    }
}