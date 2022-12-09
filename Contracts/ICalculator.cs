using Grpc.Core;
using ProtoBuf.Grpc;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Xml.Linq;

namespace Contracts
{
    [DataContract]
    public class MultiplyRequest
    {
        [DataMember(Order = 1)]
        public int X { get; set; }

        [DataMember(Order = 2)]
        public int Y { get; set; }
    }

    [DataContract]
    public class MultiplyResult
    {
        [DataMember(Order = 1)]
        public int Result { get; set; }
    }

    [ServiceContract(Name = "Hyper.Calculator")] // or [Service("Hyper.Calculator")]
    public interface ICalculator
    {
        ValueTask<MultiplyResult> MultiplyAsync(MultiplyRequest request, CallContext context);
    }
}