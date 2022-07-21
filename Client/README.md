# A simple dotnet 6 Dapr grpc and pubsub client

This example creates a Dapr grpc client to invoke methods in [the Dapr grpc server](../Server), mimicing the [Greeter dotnet grpc tutorial](https://docs.microsoft.com/en-us/aspnet/core/tutorials/grpc/grpc-start?view=aspnetcore-6.0&tabs=visual-studio), using [dotnet core 6 minimal API](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-6.0). It also creates a Dapr pubsub client to test the same server that's also registered to subscribe to Dapr pubsub messages.

## How is it different from a regular grpc client?

With the recent support of `proxy.grpc` in Dapr, you no longer need to invoke grpc service using a Dapr client. You only need to make the following modifications to the client:
  * Use environment variable `DAPR_GRPC_PORT` when running with `dapr run` so that the client connects to the Dapr sidecar instead of directly to the grpc service. When running without dapr, the same client can connect to the native grpc service.
  * Add `Metadata { { "dapr-app-id", "greeter-service" } };` when making the grpc call so that the dapr proxy knows to route it to the native grpc service.

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
* Note down the Dapr app id of the grpc server and make sure it matches the `dapr-app-id` metadata added in [Program.cs](./Program.cs).
* Make sure the pubsub broker and topic that the server subscribes to matches the `broker` and `topic` variables in [Program.cs](./Program.cs) this client publishes to. 
* Run the following command:

```bash
# to invoke grpc method natively
dot net run 1
# to invoke grpc method with dapr
dapr run --app-id greeter-client -- dotnet run 1 
# to publish an event to message bus
dapr run --app-id greeter-client -- dotnet run 2 
```

* To debug in vscode, simply hit F5.