using Reddio.DataImport;

namespace Reddio.UnitTests.DataImport
{
    public class JsonSnakeCaseNamingPolicyTests
    {
        [Theory]
        [InlineData("foobar", "foobar")]
        [InlineData("Foobar", "foobar")]
        [InlineData("fooBar", "foo_bar")]
        [InlineData("FooBar", "foo_bar")]
        [InlineData("FooBarI", "foo_bar_i")]
        public void ConvertName_ReturnsSnakeCaseName(string inputName, string expectedName)
        {
            var policy = new JsonSnakeCaseNamingPolicy();

            var result = policy.ConvertName(inputName);

            Assert.Equal(expectedName, result);
        }
    }
}
