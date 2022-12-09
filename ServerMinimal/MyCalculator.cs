using Contracts;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using ProtoBuf.Grpc;
using System.Security.Claims;

namespace ServerMinimal
{
    public class MyCalculator : ICalculator
    {
        ValueTask<MultiplyResult> ICalculator.MultiplyAsync(MultiplyRequest request, CallContext context)
        {
            var username = context.ServerCallContext.GetHttpContext().User.Identity.Name;
            Console.WriteLine(username);
            return new ValueTask<MultiplyResult>(new MultiplyResult { Result = request.X * request.Y });
        }
    }
}
