using System;
using System.Text.RegularExpressions;
using Grpc.Net.ClientFactory;
using Knowit.Grpc.Backoff;
using Knowit.Grpc.Correlation;
using Microsoft.Extensions.Options;

namespace Knowit.Grpc.Client
{
    internal class Client
    {
        private readonly IOptionsMonitor<GrpcClientOptions> _options;
        private readonly IServiceProvider _services;

        public Client(IOptionsMonitor<GrpcClientOptions> options, IServiceProvider services)
        {
            _options = options;
            _services = services;
        }

        public void Configure(string name, GrpcClientFactoryOptions client)
        {
            var options = _options.Get(name);
            
            if (options.Address != null)
            {
                var address = new Regex(@"^(?!\w+:\/\/)")
                    .Replace(options.Address, "http://");
                client.Address = new Uri(address);
            }

            client.Interceptors.AddCorrelationId(_services);
            
            if (options.RetryCount != null)
            {
                client.Interceptors.AddExponentialBackoff(_services, 
                    options.RetryCount.Value, options.RetryInterval, options.RetryForever);
            }
        }
    }
}