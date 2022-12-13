using Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using ProtoBuf.Grpc.Server;
using ServerMinimal;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var rsaPrivateKey = Convert.FromBase64String(builder.Configuration["Jwt:RsaPrivateKey"]);
using RSA rsaWithPrivateKey = RSA.Create();
//rsaWithPrivateKey.ImportRSAPrivateKey(rsaPrivateKey, out _);
rsaWithPrivateKey.ImportFromPem(File.ReadAllText("private-key.pem").ToCharArray());

var rsaPublicKey = Convert.FromBase64String(builder.Configuration["Jwt:RsaPublicKey"]);
using RSA rsaWithPublicKey = RSA.Create();
//rsaWithPublicKey.ImportRSAPublicKey(rsaPublicKey, out _);
rsaWithPublicKey.ImportFromPem(File.ReadAllText("public-key.pem").ToCharArray());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCodeFirstGrpc(options =>
{
    options.MaxReceiveMessageSize = 1 * 1024 * 1024; // 1 MB
    options.MaxSendMessageSize = 1 * 1024 * 1024; // 1 MB
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        //IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:HmacKey"])),
        IssuerSigningKey = new RsaSecurityKey(rsaWithPublicKey),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
    };
});

builder.Services.AddAuthorization();

builder.Services.AddSingleton<ICounter, MyCounter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/MyCalculatorWebApi", (int x, int y) =>
{
    return new ValueTask<MultiplyResult>(new MultiplyResult { Result = x * y });
}).RequireAuthorization();

app.MapGet("/security/getMessage", () =>"Hello World!").RequireAuthorization();

app.MapPost("/security/createToken",
[AllowAnonymous] (User user) =>
{
    if (user.UserName == "user" && user.Password == "password")
    {
        var issuer = builder.Configuration["Jwt:Issuer"];
        var audience = builder.Configuration["Jwt:Audience"];
        var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:HmacKey"]);
        //var stringToken = JWTHelper.GenerateHmacToken(issuer, audience, key, user.UserName);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
                {
                new Claim("Id", Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Email, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
             }),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = issuer,
            Audience = audience,
            //SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),SecurityAlgorithms.HmacSha512Signature)
            SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsaWithPrivateKey),SecurityAlgorithms.RsaSha256)
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);

        return Results.Ok(jwtToken);
    }
    return Results.Unauthorized();
});

app.UseAuthentication();
app.UseAuthorization();


//app.MapGrpcService<MyCalculator>().RequireAuthorization();
app.MapGrpcService<MyCalculator>();
app.MapGrpcService<MyTimeService>();
app.MapGrpcService<ICounter>();

app.Run();
