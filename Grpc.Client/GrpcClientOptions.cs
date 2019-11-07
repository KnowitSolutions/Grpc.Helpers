namespace Knowit.Grpc.Client
{
    public class GrpcClientOptions
    {
        public string Address { get; set; }
        public int? RetryCount { get; set; }
        public int RetryInterval { get; set; }
        public bool RetryForever { get; set; }
    }
}