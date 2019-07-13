using System;

namespace Grpc.Correlation
{
    public class CorrelationId
    {
        public Guid Value { get; internal set; }
    }
}