using FluentAssertions;
using Grpc.Net.Client;
using GrpcOAuth.Tests;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GrpcOauth.Tests;

public class GrpcTests
{
    private WebApplicationFactory<Program> factory;
    private GrpcChannel channel;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        factory = new WebApplicationFactory<Program>();

        var options = new GrpcChannelOptions { HttpClient = factory.CreateClient() };

        channel = GrpcChannel.ForAddress(factory.Server.BaseAddress, options);
    }

    [Test]
    public async Task UnauthorizedCallSucceeds()
    {
        var client = new Greeter.GreeterClient(channel);
        var request = new HelloRequest { Name = "World" };

        var response = await client.SayHelloAsync(request);

        response.Message.Should().Be("Hello World");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        channel.Dispose();
        factory.Dispose();
    }
}
