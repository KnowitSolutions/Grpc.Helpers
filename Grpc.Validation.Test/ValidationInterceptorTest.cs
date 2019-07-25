using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.AspNetCore;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Knowit.Grpc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Grpc.Validation.Test
{
    public class ValidationInterceptorTest : ServiceTests<Echo.EchoClient, Service>
    {
        protected override void ConfigureGrpc(GrpcServiceOptions options)
        {
            options.AddValidationInterceptor();
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddMvc().AddFluentValidation();
            services.AddValidator<MessageValidator>();
        }

        [Test]
        public void ValidMessageShouldNotThrowException()
        {
            Assert.DoesNotThrow(() =>
            {
                Client.Bytes(new BytesValue
                {
                    Value = ByteString.CopyFrom("Hello", Encoding.Default)
                });
            });
        }

        [Test]
        public void InvalidMessageShouldThrowException()
        {
            var exception = Assert.Throws<RpcException>(() =>
            {
                Client.Bytes(new BytesValue {Value = ByteString.Empty});
            });

            Assert.AreEqual(StatusCode.InvalidArgument, exception.StatusCode);
        }
    }


    public class Service : Echo.EchoBase
    {
        public override Task<Empty> Empty(Empty request, ServerCallContext context) => Task.FromResult(request);

        public override Task<BytesValue> Bytes(BytesValue request, ServerCallContext context) =>
            Task.FromResult(request);
    }

    public class MessageValidator : AbstractValidator<BytesValue>
    {
        public MessageValidator()
        {
            RuleFor(bytes => bytes.Value).NotEmpty();
        }
    }
}
