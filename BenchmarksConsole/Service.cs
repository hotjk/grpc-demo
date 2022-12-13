using BenchmarkDotNet.Attributes;
using Contracts;
using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Grpc.Net.Client;
using ProtoBuf.Grpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using ProtoBuf.Grpc.Client;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace BenchmarksConsole
{
    public class Service
    {
        private async Task<string> Login(StringContent content)
        {
            HttpClient client = new HttpClient();
            var resp = await client.PostAsync(@"https://localhost:7017/security/createToken", content);
            var token = await resp.Content.ReadAsStringAsync();
            token = token.Trim('"');

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://localhost:7017/security/getMessage"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var msg = await client.SendAsync(requestMessage);
                Console.WriteLine(msg.StatusCode.ToString());
                Console.WriteLine(await msg.Content.ReadAsStringAsync());
            }

            return token;
        }

        private GrpcChannel CreateChannel(string token)
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

        public Service()
        {
            StringContent content = new StringContent(
@"{
  ""userName"": ""user"",
  ""password"": ""password""
}", Encoding.UTF8, "application/json");

            string token = Login(content).Result;
            GrpcChannel channel = CreateChannel(token);
            grpcCalculator = channel.CreateGrpcService<ICalculator>();

            webApiHttpClient = new HttpClient();
            webApiHttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }

        private ICalculator grpcCalculator;
        private HttpClient webApiHttpClient;

        [Benchmark]
        public async Task Grpc()
        {
            var result = await grpcCalculator.MultiplyAsync(new MultiplyRequest { X = 12, Y = 4 }, CallContext.Default);
            //Console.WriteLine(result.Result);
        }

        [Benchmark]
        public async Task WebApi()
        {
            int x = 12;
            int y = 4;
            var resp = await webApiHttpClient.GetAsync($"https://localhost:7017/MyCalculatorWebApi?x={x}&y={y}");
            var json = await resp.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MultiplyResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true});
            //Console.WriteLine(result.Result);
        }
    }
}
