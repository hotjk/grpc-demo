using Contracts;
using Microsoft.AspNetCore.Authorization;

namespace ServerMinimal
{
    [Authorize]
    public class MyCounter : ICounter
    {
        private int counter = 0;
        private readonly object counterLock = new object();

        ValueTask<IncrementResult> ICounter.IncrementAsync(IncrementRequest request)
        {
            lock (counterLock)
            {
                counter += request.Inc;
                var result = new IncrementResult { Result = counter };
                return new ValueTask<IncrementResult>(result);
            }
        }
    }
}
