# A simple dotnet 6 Dapr grpc server

This example creates a Dapr grpc service, mimicing the [Greeter dotnet grpc tutorial](https://docs.microsoft.com/en-us/aspnet/core/tutorials/grpc/grpc-start?view=aspnetcore-6.0&tabs=visual-studio), using [dotnet core 6 minimal API](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-6.0). The grpc service can be:
* invoked by a client for each method it supports. 
* registered as an event handler for pubsub topics.

## How is it different from a regular grpc server?
Since Dapr injects a side-car to communicate with your grpc server and client, your server must be built in a certain way for Dapr to interact with.

1. The grpc service must inherit from `AppCallback.AppCallbackBase` and [override its virtual methods as needed](https://github.com/dapr/dotnet-sdk/tree/0c9d6a45c8d3792a92d7141056c390bea098d02b/examples/AspNetCore/GrpcServiceSample).
2. You don't need to specify grpc service apis in `.proto`. In fact, Grpc server code generation must be disabled by setting `GrpcServices="None"` in `.csproj`. Without the generated code, you must do serialization and deserialization yourself.
3. Dapr adds metadata to the grpc request using `InvokeRequest` defined in `Dapr.Client.Autogen.Grpc.v1`, whereas a regular grpc request is defined in your proto, such as `HelloRequest` in the [Greeter example](https://docs.microsoft.com/en-us/aspnet/core/tutorials/grpc/grpc-start?view=aspnetcore-6.0&tabs=visual-studio). If you have a regular grpc service, its methods must be retrofitted for a Dapr grpc client to call. Vice versa, if you have a Dapr grpc service, a regular grpc client will have trouble calling it as-is.

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
* Install Dapr in your environment.
* Check `appsettings.{environment}.json` to note
  * the Kestrel server port that grpc will be listening on
  * the Dapr pubsub broker
* Make sure you have the specified Dapr pubsub broker installed, for example, it can be a [Redis Streams](https://docs.dapr.io/reference/components-reference/supported-pubsub/setup-redis-pubsub/). 
* Run the following command:

```bash
dotnet restore
dotnet build
dapr run --app-id greeter-service --app-port <must-match-Kestrel-port> --app-protocol grpc -- dotnet run
```

* To debug in vscode, make sure the task `daprd-debug` in [tasks.json](.vscode/tasks.json) has the correct `appPort`. Choose `Dapr .NET Core Launch (web)` profile and hit F5 to debug.
