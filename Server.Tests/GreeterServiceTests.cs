using Xunit;
using Moq;
using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using GrpcGreeter.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Tests.Server.UnitTests
{
    public class WorkerTests
    {
        [Fact]
        public async Task SayHelloUnaryTest()
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
        public async Task ListSubscription_returns_subscriptions()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string> {
                {"Subscription:Broker", "mqtt-broker"},
                {"Subscription:Topic", "greeter"},
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var mockLogger = new Mock<ILogger<GreeterService>>();
            var service = new GreeterSubscriptionService(mockLogger.Object, configuration);
            var callContext = new Mock<ServerCallContext>();

            // Act
            var response = await service.ListTopicSubscriptions(
                new Empty(), callContext.Object);

            // Assert
            Assert.Equal(1, response.Subscriptions.Count);
            Assert.Equal("mqtt-broker", response.Subscriptions[0].PubsubName);
            Assert.Equal("greeter", response.Subscriptions[0].Topic);
        }

        public async Task SayHello_called_with_string()
        {
            // Arrange

            // Act

            // Assert
        }
    }
}