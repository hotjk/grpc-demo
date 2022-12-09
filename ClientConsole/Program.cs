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

var content = new StringContent(
@"{
  ""userName"": ""joydip"",
  ""password"": ""joydip123""
}", Encoding.UTF8, "application/json");

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

var credentials = CallCredentials.FromInterceptor((context, metadata) =>
{
    if (!string.IsNullOrEmpty(token))
    {
        metadata.Add("Authorization", $"Bearer {token}");
    }
    return Task.CompletedTask;
});

var channel = GrpcChannel.ForAddress("https://localhost:7017", new GrpcChannelOptions
{
    Credentials = ChannelCredentials.Create(new SslCredentials(), credentials)
});

var calculator = channel.CreateGrpcService<ICalculator>();
var result = await calculator.MultiplyAsync(new MultiplyRequest { X = 12, Y = 4 }, CallContext.Default);
Console.WriteLine(result.Result);

var clock = channel.CreateGrpcService<ITimeService>();
var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(2));
var options = new CallOptions(cancellationToken: cancel.Token);
try
{
    await foreach (var time in clock.SubscribeAsync(new CallContext(options)))
    {
        Console.WriteLine($"The time is now: {time.Time}");
    }
}
catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled)
{
    Console.WriteLine("Cancelled.");
}

Console.ReadLine();