using Google.Protobuf;
using GrpcGreeter;
using Dapr.Client;

public class DaprCaller
{
    private readonly DaprClient _client;

    public DaprCaller(DaprClient client)
    {
        _client = client;
    }

    public async Task Publish(string broker, string topic, HelloRequest request)
    {
        var data = JsonFormatter.Default.Format(request);
        await _client.PublishEventAsync<string>(broker, topic, data);
    }
}