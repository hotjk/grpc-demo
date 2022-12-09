using Grpc.Core;
using Grpc.Net.Client;
using System.Text;
using System.Net.Http.Headers;
using System.Globalization;
using Hyper;
using Contracts;
using Google.Protobuf.WellKnownTypes;

var content = new StringContent(
@"{
  ""userName"": ""user"",
  ""password"": ""password""
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

var calculator = new Calculator.CalculatorClient(channel);
var result = await calculator.MultiplyAsync(new MultiplyRequest { X = 12, Y = 4 });
Console.WriteLine(result.Result);

var timeService = new TimeService.TimeServiceClient(channel);
var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(2));
var options = new CallOptions(cancellationToken: cancel.Token);
try
{
    await foreach (var time in timeService.Subscribe(new Empty(), options).ResponseStream.ReadAllAsync())
    {
        Console.WriteLine($"The time is now: {time.Time}");
    }
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
{
    Console.WriteLine("Cancelled.");
}

Console.ReadLine();