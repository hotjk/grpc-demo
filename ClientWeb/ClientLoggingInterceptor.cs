using Grpc.Core.Interceptors;
using Grpc.Core;
using static Grpc.Core.Interceptors.Interceptor;

namespace ClientWeb
{
    public class ClientLoggingInterceptor : Interceptor
    {
        private readonly ILogger _logger;

        public ClientLoggingInterceptor(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ClientLoggingInterceptor>();
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            _logger.LogInformation($"Starting call. Type: {context.Method.Type}. " +
                $"Method: {context.Method.Name}.");
            return continuation(request, context);
        }
    }
}
