using Grpc.Core;
using GrpcGreeter;

public class GrpcCaller
{
    private readonly Greeter.GreeterClient _client;

    public GrpcCaller(Greeter.GreeterClient client)
    {
        _client = client;
    }

    public async Task<HelloReply> CallServer(HelloRequest request, Metadata metadata)
    {
        return await _client.SayHelloAsync(request, metadata);
    }
}