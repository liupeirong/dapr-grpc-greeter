# A simple dotnet 6 Dapr grpc and pubsub client

This example creates a Dapr grpc client to invoke methods in [the Dapr grpc server](../Server), mimicing the [Greeter dotnet grpc tutorial](https://docs.microsoft.com/en-us/aspnet/core/tutorials/grpc/grpc-start?view=aspnetcore-6.0&tabs=visual-studio), using [dotnet core 6 minimal API](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-6.0). It also creates a Dapr pubsub client to test the same server that's also registered to subscribe to Dapr pubsub messages.

## How is it different from a regular grpc client?

1. The Dapr client does not use the generated grpc client code from `.proto`. In fact, grpc client code generation must be disabled by setting `GrpcServices="None"` in `.csproj`.
2. Dapr client adds metadata to your request object, ex. `HelloRequest`, to build an `InvokeRequest` defined in `Dapr.Client.Autogen.Grpc.v1` to send to the server.

## What are the gotchas? 
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

## How to run and debug the client?
* Ensure the grpc server this client talks to is already listening on its specified `--app-port`.
* Note down the Dapr app id of ther grpc server and make sure it matches the `server` variable in [Program.cs](./Program.cs).
* Make sure the pubsub broker and topic that the server subscribes to matches the `broker` and `topic` variables in [Program.cs](./Program.cs) this client publishes to. 
* Run the following command:

```bash
dotnet restore
dotnet build
# to invoke grpc method
dapr run --app-id greeter-client -- dotnet run 1 
# to publish an event to message bus
dapr run --app-id greeter-client -- dotnet run 2 
```

* To debug in vscode, simply hit F5.