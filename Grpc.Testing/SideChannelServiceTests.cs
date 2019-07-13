using System.Dynamic;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Grpc.Testing
{
    public class SideChannelServiceTests<TClient, TService> : ServiceTests<TClient, TService>
        where TClient : ClientBase<TClient>
        where TService : class
    {
        protected dynamic SideChannel { get; private set; }

        [SetUp]
        public void ResetSideChannel()
        {
            SideChannel = new ExpandoObject();
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddScoped(provider => SideChannel);
        }
    }
}