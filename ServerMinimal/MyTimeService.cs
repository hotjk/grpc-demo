using Contracts;
using ProtoBuf.Grpc;
using System.Runtime.CompilerServices;

namespace ServerMinimal
{
    public class MyTimeService : ITimeService
    {
        public IAsyncEnumerable<TimeResult> SubscribeAsync(CallContext context = default)
            => SubscribeAsync(context.CancellationToken);

        private async IAsyncEnumerable<TimeResult> SubscribeAsync([EnumeratorCancellation] CancellationToken cancel)
        {
            while (true)
            {
                if (cancel.IsCancellationRequested) { yield break; }
                yield return new TimeResult { Time = DateTime.UtcNow };
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100), cancel);
                }
                catch (TaskCanceledException)
                {
                    yield break;
                }
            }
        }
    }
}
