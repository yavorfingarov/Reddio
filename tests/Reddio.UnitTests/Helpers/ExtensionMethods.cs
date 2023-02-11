using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Reddio.UnitTests.Helpers
{
    internal static class ExtensionMethods
    {
        public static void Setup<T>(this Mock<ILogger<T>> logger, LogLevel logLevel)
        {
            logger.Setup(l => l.Log(logLevel, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
        }

        public static void Verify<T>(this Mock<ILogger<T>> logger, LogLevel logLevel, string messagePart,
            Exception? exception = null)
        {
            logger.Verify(l => l.Log(logLevel, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(messagePart)),
                exception, It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), Times.Once);
        }

        public static int? GetStatusCode(this IResult result) => (int?)result.GetType().GetProperty("StatusCode")?.GetValue(result);

        public static T? GetValue<T>(this IResult result) => (T?)result.GetType().GetProperty("Value")?.GetValue(result);
    }
}
