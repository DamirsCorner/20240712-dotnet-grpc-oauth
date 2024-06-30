using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcOAuth.Tests;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace GrpcOauth.Tests;

public class GrpcTests
{
    private WebApplicationFactory<Program> factory;
    private GrpcChannel channel;

    private static X509Certificate2 CreateCertificate()
    {
        var rsa = RSA.Create();
        var certificateRequest = new CertificateRequest(
            "CN=WebApiOAuth",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
        var certificate = certificateRequest.CreateSelfSigned(
            DateTimeOffset.Now,
            DateTimeOffset.Now.AddDays(1)
        );
        return certificate;
    }

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        factory = new WebApplicationFactory<Program>();

        var options = new GrpcChannelOptions { HttpClient = factory.CreateClient() };

        channel = GrpcChannel.ForAddress(factory.Server.BaseAddress, options);
    }

    [Test]
    public async Task UnauthorizedCallFails()
    {
        var client = new Greeter.GreeterClient(channel);
        var request = new HelloRequest { Name = "World" };

        var action = async () => await client.SayHelloAsync(request);

        await action
            .Should()
            .ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unauthenticated);
    }

    [Test]
    public async Task AuthorizedCallSucceeds()
    {
        var token = JwtBuilder
            .Create()
            .WithAlgorithm(new RS256Algorithm(CreateCertificate()))
            .AddClaim(ClaimName.Issuer, "https://localhost:5000")
            .AddClaim(ClaimName.Audience, "web-api-oauth-test")
            .AddClaim(ClaimName.Subject, "test")
            .AddClaim(
                ClaimName.ExpirationTime,
                DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            )
            .Encode();

        var client = new Greeter.GreeterClient(channel);
        var request = new HelloRequest { Name = "World" };
        var metadata = new Metadata { { "Authorization", $"Bearer {token}" } };

        var response = await client.SayHelloAsync(request, metadata);

        response.Message.Should().Be("Hello World");
    }

    [Test]
    public async Task AuthorizedCallWithExpiredTokenFails()
    {
        var token = JwtBuilder
            .Create()
            .WithAlgorithm(new RS256Algorithm(CreateCertificate()))
            .AddClaim(ClaimName.Issuer, "https://localhost:5000")
            .AddClaim(ClaimName.Audience, "web-api-oauth-test")
            .AddClaim(ClaimName.Subject, "test")
            .AddClaim(
                ClaimName.ExpirationTime,
                DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds()
            )
            .Encode();

        var client = new Greeter.GreeterClient(channel);
        var request = new HelloRequest { Name = "World" };
        var metadata = new Metadata { { "Authorization", $"Bearer {token}" } };

        var action = async () => await client.SayHelloAsync(request, metadata);

        await action
            .Should()
            .ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unauthenticated);
    }

    [Test]
    public async Task AuthorizedCallWithInvalidAudienceFails()
    {
        var token = JwtBuilder
            .Create()
            .WithAlgorithm(new RS256Algorithm(CreateCertificate()))
            .AddClaim(ClaimName.Issuer, "https://localhost:5000")
            .AddClaim(ClaimName.Audience, "invalid")
            .AddClaim(ClaimName.Subject, "test")
            .AddClaim(
                ClaimName.ExpirationTime,
                DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
            )
            .Encode();

        var client = new Greeter.GreeterClient(channel);
        var request = new HelloRequest { Name = "World" };
        var metadata = new Metadata { { "Authorization", $"Bearer {token}" } };

        var action = async () => await client.SayHelloAsync(request, metadata);

        await action
            .Should()
            .ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unauthenticated);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        channel.Dispose();
        factory.Dispose();
    }
}
