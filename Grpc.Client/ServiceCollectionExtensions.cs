using System;
using System.Text.RegularExpressions;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Knowit.Grpc.Client
{
    public static class ServiceCollectionExtensions
    {
        private const string ConfigurationSection = "Grpc:Clients";

        public static void AddGrpcClientConfiguration<T>(
            this IServiceCollection services,
            Action<GrpcClientFactoryOptions> action)
            where T : ClientBase<T> =>
            services.AddGrpcClientConfiguration<T>(null, action);

        public static void AddGrpcClientConfiguration<T>(
            this IServiceCollection services,
            string configName = null,
            Action<GrpcClientFactoryOptions> action = null)
            where T : ClientBase<T>
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (action == null)
            {
                action = _ => { };
            }

            var type = typeof(T);
            var name = (type.Namespace != null ? $"{type.Namespace}." : "") +
                       type.Name +
                       (configName != null ? $", {configName}" : "");
            
            if (configName == null)
            {
                configName = new Regex("Client$")
                    .Replace(typeof(T).Name, "");
            }

            services
                .AddOptions<GrpcClientOptions>(name)
                .Configure<IConfiguration>((options, configuration) =>
                {
                    var section = configuration.GetSection($"{ConfigurationSection}:{configName}");
                    options.Address = section.Value;
                    section.Bind(options);
                });

            
            services.AddGrpcClient<T>(name, (provider, options) =>
            {
                var snapshot = provider.GetService<IOptionsSnapshot<GrpcClientOptions>>();
                var grpcOptions = snapshot.Get(name);

                if (grpcOptions.Address != null)
                {
                    var address = new Regex(@"^(?!\w+:\/\/)")
                        .Replace(grpcOptions.Address, "http://");
                    options.BaseAddress = new Uri(address);
                }

                action(options);
            });
        }
    }
}