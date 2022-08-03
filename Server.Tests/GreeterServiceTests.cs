using Xunit;
using Moq;
using Grpc.Core;
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
    }
}