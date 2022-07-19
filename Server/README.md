# A simple Dapr dotnet Grpc Server sample

## How is it different from a regular Grpc server?
TODO

## What are the gotchas?
### Invoke Service
TODO
### Pubsub
TODO

## How to run?

```bash
dotnet restore
dotnet build
dapr run --app-id greeter-server --app-port 5070 --app-protocol grpc -- dotnet run
```
