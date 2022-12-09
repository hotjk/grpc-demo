using ClientWeb;
using Contracts;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.ClientFactory;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

Action<GrpcClientFactoryOptions> configureClient = options =>
{
    options.Address = new Uri("https://localhost:7017");
    options.InterceptorRegistrations.Add(new InterceptorRegistration(InterceptorScope.Channel, sp => new ClientLoggingInterceptor(sp.GetRequiredService<ILoggerFactory>())));
    options.ChannelOptionsActions.Add(o =>
    {
        o.HttpHandler = new SocketsHttpHandler()
        {
            // keeps connection alive
            PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
            KeepAlivePingDelay = TimeSpan.FromSeconds(60),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(30),

            // allows channel to add additional HTTP/2 connections
            EnableMultipleHttp2Connections = true
        };
    });
};

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

builder.Services.AddCodeFirstGrpcClient<ICalculator>(configureClient).AddCallCredentials((context, metadata) =>
{
    if (!string.IsNullOrEmpty(token))
    {
        metadata.Add("Authorization", $"Bearer {token}");
    }
    return Task.CompletedTask;
});
builder.Services.AddCodeFirstGrpcClient<ITimeService>(configureClient);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/time", async (ITimeService timeService) =>
{
    var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(1));
    var options = new CallOptions(cancellationToken: cancel.Token);
    try
    {
        await foreach (var time in timeService.SubscribeAsync(new CallContext(options)))
        {
            Console.WriteLine($"The time is now: {time.Time}");
        }
    }
    catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled)
    {
        Console.WriteLine("Cancelled.");
    }
});

app.MapGet("/calc", async (ICalculator calculator) =>
{
    var result = await calculator.MultiplyAsync(new MultiplyRequest { X = 12, Y = 4 }, CallContext.Default);
    Console.WriteLine(result.Result);
});

app.Run();
