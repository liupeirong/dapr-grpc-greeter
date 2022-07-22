using Dapr.Client;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcGreeter;


if (args.Length > 0 && int.TryParse(args[0], out var index))
{
    var name = "World";
    var request = new HelloRequest { Name = name };

    if (index == 1)
    {
        // when running with "dapr run", DAPR_GRPC_PORT will be set automatically, 
        // when running with native grpc client, just use the native grpc service port.
        var native_grpc_service_port = "5070";
        var port = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT") ?? native_grpc_service_port;
        var serverAddress = $"http://localhost:{port}";
        Console.WriteLine($"Invoke SayHello on {serverAddress} with {name}");
        var channel = GrpcChannel.ForAddress(serverAddress);
        var client = new Greeter.GreeterClient(channel);

        // tell dapr the target service; no effect if not running with dapr. 
        var metadata = new Metadata { { "dapr-app-id", "greeter-service" } };
        var response = client.SayHello(request, metadata);
        Console.WriteLine($"Greeting: {response.Message}");
    }
    else{
        var broker = "mqtt-pubsub"; //must match the broker that the server registers with in ListTopicSubscriptions()
        var topic = "greeter"; //must match the topic that the server subscribes to in ListTopicSubscriptions()
        Console.WriteLine($"Publish to topic {topic} on broker {broker} with {name}");
        var client = new DaprClientBuilder().Build();
        //serialize proto to json explicitly as Dapr default serialization may cause it difficult to deserialize complex protobuf on the server.
        var data = JsonFormatter.Default.Format(request);
        await client.PublishEventAsync(broker, topic, data);
        Console.WriteLine("Publish done.");
    }
}
else
{
    Console.WriteLine("Usage: dotnet run <index>");
    Console.WriteLine("<index> is 1 for InvokeMethodGrpc, 2 for PublishEvent");
}