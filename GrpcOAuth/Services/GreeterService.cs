using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace GrpcOAuth.Services;

[Authorize]
public class GreeterService : Greeter.GreeterBase
{
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HelloReply { Message = "Hello " + request.Name });
    }
}
