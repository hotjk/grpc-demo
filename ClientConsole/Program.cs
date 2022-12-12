using Contracts;
using Grpc.Core;
using Grpc.Net.Client;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;
using System.Text.Json.Nodes;
using System.Text;
using System;
using System.Threading.Channels;
using System.Net.Http.Headers;
using System.Net.Http;
using static System.Net.WebRequestMethods;
using Grpc.Net.Client.Configuration;

var content = new StringContent(
@"{
  ""userName"": ""user"",
  ""password"": ""password""
}", Encoding.UTF8, "application/json");

string token = await Login(content);

GrpcChannel channel = CreateChannel(token);

await Unary(channel);

await Stream(channel);

Console.ReadLine();

static async Task<string> Login(StringContent content)
{
    HttpClient client = new HttpClient();
    var resp = await client.PostAsync(@"https://localhost:7017/security/createToken", content);
    var token = await resp.Content.ReadAsStringAsync();
    token = token.Trim('"');
    Console.WriteLine(token);

    using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://localhost:7017/security/getMessage"))
    {
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var msg = await client.SendAsync(requestMessage);
        Console.WriteLine(msg.StatusCode.ToString());
        Console.WriteLine(await msg.Content.ReadAsStringAsync());
    }

    return token;
}

static GrpcChannel CreateChannel(string token)
{
    var credentials = CallCredentials.FromInterceptor((context, metadata) =>
    {
        if (!string.IsNullOrEmpty(token))
        {
            metadata.Add("Authorization", $"Bearer {token}");
        }
        return Task.CompletedTask;
    });

    var methodConfig = new MethodConfig
    {
        Names = { MethodName.Default },
        RetryPolicy = new RetryPolicy
        {
            MaxAttempts = 3,
            InitialBackoff = TimeSpan.FromSeconds(0.5),
            MaxBackoff = TimeSpan.FromSeconds(0.5),
            BackoffMultiplier = 1,
            RetryableStatusCodes = { StatusCode.Unavailable, StatusCode.Unknown }
        }
    };

    var channel = GrpcChannel.ForAddress("https://localhost:7017", new GrpcChannelOptions
    {
        Credentials = ChannelCredentials.Create(new SslCredentials(), credentials),
        ServiceConfig = new ServiceConfig { MethodConfigs = { methodConfig } }
    });
    return channel;
}

static async Task Unary(GrpcChannel channel)
{
    var calculator = channel.CreateGrpcService<ICalculator>();
    var result = await calculator.MultiplyAsync(new MultiplyRequest { X = 12, Y = 4 }, CallContext.Default);
    Console.WriteLine(result.Result);
}

static async Task Stream(GrpcChannel channel)
{
    var clock = channel.CreateGrpcService<ITimeService>();
    var counter = channel.CreateGrpcService<ICounter>();
    var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(2));
    var options = new CallOptions(cancellationToken: cancel.Token);
    try
    {
        await foreach (var time in clock.SubscribeAsync(new CallContext(options)))
        {
            Console.WriteLine($"The time is now: {time.Time}");
            var currentInc = await counter.IncrementAsync(new IncrementRequest { Inc = 1 });
            Console.WriteLine($"Time received {currentInc.Result} times");
        }
    }
    catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
    {
        Console.WriteLine("Cancelled");
    }
    catch (OperationCanceledException) { }
}