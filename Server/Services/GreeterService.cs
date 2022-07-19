using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Dapr.AppCallback.Autogen.Grpc.v1;
using Dapr.Client.Autogen.Grpc.v1;
using System.Text.Json;

namespace GrpcGreeter.Services;

public class GreeterService : AppCallback.AppCallbackBase
{
    private readonly ILogger<GreeterService> _logger;
    private readonly String _broker;
    private readonly String _topic;
    public GreeterService(ILogger<GreeterService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _broker = configuration.GetValue<string>("Subscription:Broker");
        _topic = configuration.GetValue<string>("Subscription:Topic");
    }

    readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public override Task<InvokeResponse> OnInvoke(InvokeRequest request, ServerCallContext context)
    {
        _logger.LogDebug($"Invoked {request.Method} with {request.Data}");
        var response = new InvokeResponse();
        switch (request.Method)
        {
            case "SayHello":
                var input = request.Data.Unpack<HelloRequest>();
                var output = SayHello(input, context);
                response.Data = Any.Pack(output);
                break;
            default:
                break;
        }
        return Task.FromResult(response);
    }

    public override Task<ListTopicSubscriptionsResponse> ListTopicSubscriptions(Empty request, ServerCallContext context)
    {
        var result = new ListTopicSubscriptionsResponse();
        result.Subscriptions.Add(new TopicSubscription
        {
            PubsubName = _broker,
            Topic = _topic
        });
        return Task.FromResult(result);
    }

    public override Task<TopicEventResponse> OnTopicEvent(TopicEventRequest request, ServerCallContext context)
    {
        if (request.PubsubName == _broker)
        {
            var input = JsonSerializer.Deserialize<HelloInternal>(request.Data.ToStringUtf8(), this.jsonOptions);
            if (input != null)
            {
                var input_request = new HelloRequest() { Name = input.name };
                if (request.Topic == _topic)
                {
                    SayHello(input_request, context);
                }
            }
        }

        return Task.FromResult(new TopicEventResponse());
    }

    public HelloReply SayHello(HelloRequest request, ServerCallContext context)
    {
        return new HelloReply() { Message = $"Hello {request.Name}!" };
    }

    record HelloInternal(string name);
}
