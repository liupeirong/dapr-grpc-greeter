using Dapr.Client;
using GrpcGreeter;

if (args.Length > 0 && int.TryParse(args[0], out var index))
{
    var client = new DaprClientBuilder().Build();
    var name = "World";
    var request = new HelloRequest { Name = name };
    if (index == 1)
    {
        var server = "greeter-service"; //must match the dapr --app-id of the server
        var method = "SayHello"; //must match the method name defined in the server
        Console.WriteLine($"Invoke {method} on {server} with {name}");
        var resp = await client.InvokeMethodGrpcAsync<HelloRequest, HelloReply>(server, method, request);
        Console.WriteLine($"{server} replied: {resp.Message}");
    }
    else
    {
        var broker = "mqtt-pubsub"; //must match the broker that the server registers with in ListTopicSubscriptions()
        var topic = "greeter"; //must match the topic that the server subscribes to in ListTopicSubscriptions()
        Console.WriteLine($"Publish to topic {topic} on broker {broker} with {name}");
        await client.PublishEventAsync<HelloRequest>(broker, topic, request);
        Console.WriteLine("Publish done.");
    }
}