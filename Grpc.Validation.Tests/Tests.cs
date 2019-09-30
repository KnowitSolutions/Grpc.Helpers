using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Knowit.Grpc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Knowit.Grpc.Validation.Tests
{
    public class Tests : ServiceTests<Echo.EchoClient, Service>
    {
        [Test]
        public void TestValid()
        {
            var request = new BytesValue {Value = ByteString.CopyFrom("Hello", Encoding.Default)};
            Client.Bytes(request);
        }

        [Test]
        public void TestInvalid()
        {
            var request = new BytesValue {Value = ByteString.Empty};
            var exception = Assert.Throws<RpcException>(() => Client.Bytes(request));
            Assert.AreEqual(StatusCode.InvalidArgument, exception.StatusCode, exception.Message);
        }
        
        protected override void ConfigureGrpc(GrpcServiceOptions options)
        {
            options.AddValidationInterceptor();
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddValidator<BytesValueValidator>();
        }
    }


    public class Service : Echo.EchoBase
    {
        public override Task<Empty> Empty(Empty request, ServerCallContext context) => 
            Task.FromResult(request);

        public override Task<BytesValue> Bytes(BytesValue request, ServerCallContext context) =>
            Task.FromResult(request);
    }

    public class BytesValueValidator : AbstractValidator<BytesValue>
    {
        public BytesValueValidator()
        {
            RuleFor(bytes => bytes.Value).NotEmpty();
        }
    }
}
