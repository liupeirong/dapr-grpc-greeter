using Xunit;
using Moq;
using Grpc.Core;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using GrpcGreeter.Services;
using GrpcGreeter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Dapr.AppCallback.Autogen.Grpc.v1;

namespace Tests.Server.UnitTests
{
    public class WorkerTests
    {
        [Fact]
        public async Task SayHello_Name_ReturnsHelloName()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<GreeterService>>();
            var mockConfiguration = new Mock<IConfiguration>();
            var service = new GreeterService(mockLogger.Object, mockConfiguration.Object);
            var callContext = new Mock<ServerCallContext>();

            // Act
            var response = await service.SayHello(
                new GrpcGreeter.HelloRequest { Name = "Joe" }, callContext.Object);

            // Assert
            Assert.Equal("Hello Joe!", response.Message);
        }

        [Fact]
        public async Task ListSubscription_FromConfig_ReturnsSubscriptions()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<GreeterService>>();
            var inMemorySettings = new Dictionary<string, string> {
                {"Subscription:Broker", "mqtt-broker"},
                {"Subscription:Topic", "greeter"},
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            var mockGreeterService = new Mock<GreeterService>(mockLogger.Object, configuration);

            var service = new GreeterSubscriptionService(mockLogger.Object, configuration, mockGreeterService.Object);
            var callContext = new Mock<ServerCallContext>();

            // Act
            var response = await service.ListTopicSubscriptions(
                new Empty(), callContext.Object);

            // Assert
            Assert.Single(response.Subscriptions);
            Assert.Equal("mqtt-broker", response.Subscriptions[0].PubsubName);
            Assert.Equal("greeter", response.Subscriptions[0].Topic);
        }

        [Fact]
        public async Task OnTopicEvent_RequestData_DeserializedToHelloRequest()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<GreeterService>>();
            var inMemorySettings = new Dictionary<string, string> {
                {"Subscription:Broker", "mqtt-broker"},
                {"Subscription:Topic", "greeter"},
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            var mockGreeterService = new Mock<GreeterService>(mockLogger.Object, configuration);
            mockGreeterService.Setup<Task>(s => s.SayHello(It.IsAny<HelloRequest>(), It.IsAny<ServerCallContext>()))
                .Returns(Task.FromResult(new HelloReply { Message = "Hello Joe!" }));

            var service = new GreeterSubscriptionService(mockLogger.Object, configuration, mockGreeterService.Object);
            var topicEventRequest = new TopicEventRequest
            {
                Data = ByteString.CopyFromUtf8("\"{ \\\"name\\\": \\\"Joe\\\" }\""),
                PubsubName = "mqtt-broker",
                Topic = "greeter"
            }; 
            var callContext = new Mock<ServerCallContext>();

            // Act
            await service.OnTopicEvent(topicEventRequest, callContext.Object);

            // Assert
            mockGreeterService.Verify(s => s.SayHello(It.Is<HelloRequest>(r => r.Name == "Joe"), It.IsAny<ServerCallContext>()), Times.Once);
        }
    }
}