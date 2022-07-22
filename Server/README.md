# A simple dotnet 6 Dapr grpc server

This example creates a Dapr grpc service, mimicing the [Greeter dotnet grpc tutorial](https://docs.microsoft.com/en-us/aspnet/core/tutorials/grpc/grpc-start?view=aspnetcore-6.0&tabs=visual-studio), using [dotnet core 6 minimal API](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-6.0). The grpc service can be:
* invoked by a regular grpc client or a Dapr client. 
* registered as an event handler for pubsub topics.

## How is it different from a regular grpc server?
With `proxy.grpc` support in Dapr, existing grpc service code no longer needs to be changed for a Dapr client to call. The only required change is to add `proxy.grpc` in `.dapr/config.yaml` as shown in the following sample:

```yaml
apiVersion: dapr.io/v1alpha1
kind: Configuration
metadata:
  name: daprConfig
spec:
  tracing:
    samplingRate: "1"
    zipkin:
      endpointAddress: http://localhost:9411/api/v2/spans
  features:
    - name: proxy.grpc
      enabled: true
```

__To support pubsub message handling__ though, it's not yet possible to directly register your existing grpc apis as message handlers. You need to do the following:
* Add another grpc service that inherits from `AppCallback.AppCallbackBase` and override its virtual methods `ListTopicSubscriptions` and `OnTopicEvent` as shown in [GreeterSubscriptionService](./Services/GreeterSubscriptionService.cs).
* You need to deserialize the message yourself. To reuse the existing grpc method as the message handler as much as possible, you might want to deserialize the message to the same protobuf request the grpc method takes. To avoid SeDer incomptibility between grpc and dapr, on the client side, you might want to serialize the protobuf to a json string to put on the message bus. For example:

```c#
// On the client side, serialize a protobuf to json before putting on the message bus
using Google.Protobuf;

var request = new HelloRequest();
...
var data = JsonFormatter.Default.Format(helloRequest);
await client.PublishEventAsync(broker, topic, data, cancellationToken);
```

```c#
// On the server side, deserialize the json to protobuf
using Google.Protobuf;

// inside OnTopicEvent
var data = JsonConvert.DeserializeObject<string>(request.Data.ToStringUtf8());
var request = JsonParser.Default.Parse<HelloRequest>(data);
```

## What are the gotchas?
* To start the server, you must specify two additional parameters `--app-protocol grpc` and `--app-port`. Without them, the server will run, but Dapr doesn't know to communicate with yuor app on that port with grpc. When the client makes a call, nothing will happen.
* `--app-port` must match the port specified for the Kestrel server. In this example, it's specified in `appsettings.json`. If you specify the wrong port, the server will wait forever for the wrong port to be ready to listen on incoming requests.
* In `appsettings.[environment].json`, both http and https are enabled on the web server. If you only enable https, you must specify `--app-ssl` when running the server so that Dapr knows to communicate with the server using ssl.
* `dapr invoke` cli can be used to invoke http methods, but it doesn't seem to work to invoke grpc method at the time of this writing. 
* Dapr adds metadata in the payload of pubsub. So if you want to send a message directly to the message broker rather than using a Dapr client, you must add the same metadata for Dapr to deserialize on the subscriber side. For example, when your dapr client puts a message `{"name":"World"}` on the message bus, it lands in the `data` field of the actual message together with other fields Dapr added:

```json
{
    "type":"com.dapr.event.sent",
    "topic":"greeter",
    "traceid":"00-c90e111d312e6babf3b1c8ffd83a5a3b-d05057f15a4b6288-01",
    "tracestate":"",
    "specversion":"1.0",
    "source":"greeter-client",
    "pubsubname":"mqtt-pubsub",
    "traceparent":"00-c90e111d312e6babf3b1c8ffd83a5a3b-d05057f15a4b6288-01",
    "data":{
        "name":"World"
    },
    "id":"f67adf01-ec3d-4524-864f-0a6d8661469e",
    "datacontenttype":"application/json"
}
``` 

## How to run and debug the service?
* Install Dapr in your environment. Update `.dapr/config.yaml` to enable `proxy.grpc` as mentioned above.
* Check `appsettings.{environment}.json` to note
  * the Kestrel server port that grpc will be listening on
  * the Dapr pubsub broker
* Make sure you have the specified Dapr pubsub broker installed, for example, it can be a [Redis Streams](https://docs.dapr.io/reference/components-reference/supported-pubsub/setup-redis-pubsub/). 
* Run the following command:

```bash
dapr run --app-id greeter-service --app-port <must-match-Kestrel-port> --app-protocol grpc -- dotnet run
```

* To debug in vscode, make sure the task `daprd-debug` in [tasks.json](.vscode/tasks.json) has the correct `appPort`. Choose `Dapr .NET Core Launch (web)` profile and hit F5 to debug.
