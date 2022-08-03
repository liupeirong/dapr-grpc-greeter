using Xunit;
using Moq;
using Grpc.Core;
using Google.Protobuf;
using Dapr.Client;
using GrpcGreeter;

namespace Tests.Client.UnitTests
{
    public class WorkerTests
    {
        [Fact]
        public async Task SayHello_Success_GrpcCalled()
        {
            // Arrange
            var mockClient = new Mock<Greeter.GreeterClient>();
            mockClient
                .Setup(x => x.SayHelloAsync(It.IsAny<HelloRequest>(), It.IsAny<Metadata>(), null, default))
                .Returns(new AsyncUnaryCall<HelloReply>(
                    Task.FromResult(new HelloReply { Message = "Hello World" }),
                    Task.FromResult(new Metadata()),
                    () => Status.DefaultSuccess,
                    () => new Metadata(),
                    () => { }));
            
            var caller = new GrpcCaller(mockClient.Object);

            // Act
            var reply = await caller.CallServer(
                new HelloRequest {Name = "foo"},
                new Metadata());

            // Assert
            Assert.Equal("Hello World", reply.Message);
        }

        [Fact]
        public async Task Publish_CalledWith_Json()
        {
            // Arrange
            var mockClient = new Mock<DaprClient>();
            mockClient
                .Setup(x => x.PublishEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
                .Returns(Task.CompletedTask);
            
            var caller = new DaprCaller(mockClient.Object);
            var request = new HelloRequest {Name = "foo"};
            var request_json = JsonFormatter.Default.Format(request);

            // Act
            await caller.Publish("broker", "topic", request);

            // Assert
            mockClient.Verify(x => x.PublishEventAsync("broker", "topic", request_json, default), Times.Once);
        }
    }
}