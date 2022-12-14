using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    [ServiceContract]
    public interface ICounter
    {
        ValueTask<IncrementResult> IncrementAsync(IncrementRequest request);
    }

    [DataContract]
    public class IncrementRequest
    {
        [DataMember(Order = 1)]
        public int Inc { get; set; }
    }

    [DataContract]
    public class IncrementResult
    {
        [DataMember(Order = 1)]
        public int Result { get; set; }
    }
}
