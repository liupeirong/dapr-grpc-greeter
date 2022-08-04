using Dapr.AppCallback.Autogen.Grpc.v1;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Newtonsoft.Json;
using System.Text.Json;

namespace GrpcGreeter.Services;

public class GreeterSubscriptionService : AppCallback.AppCallbackBase
{
    private readonly ILogger<GreeterService> _logger;
    private readonly String _broker;
    private readonly String _topic;
    private readonly GreeterService _greeterService;

    public GreeterSubscriptionService(ILogger<GreeterService> logger, IConfiguration configuration, GreeterService greeterService)
    {
        _logger = logger;
        _broker = configuration.GetValue<string>("Subscription:Broker");
        _topic = configuration.GetValue<string>("Subscription:Topic");
        _greeterService = greeterService;
    }

    readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

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
        _logger.LogDebug($"Received {request.Topic} on {request.PubsubName}: {request.Data.ToStringUtf8()}");
        if (request.PubsubName == _broker)
        {
            var data = JsonConvert.DeserializeObject<string>(request.Data.ToStringUtf8());
            var input = JsonParser.Default.Parse<HelloRequest>(data);
            if (input != null)
            {
                if (request.Topic == _topic)
                {
                    _greeterService.SayHello(input, context);
                }
            }
        }

        return Task.FromResult(new TopicEventResponse());
    }
}