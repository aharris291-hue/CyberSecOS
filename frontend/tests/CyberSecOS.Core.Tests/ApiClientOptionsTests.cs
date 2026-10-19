using CyberSecOS.Core.Api;

namespace CyberSecOS.Core.Tests;

public class ApiClientOptionsTests
{
    [Fact]
    public void ConfiguredUrlGetsTrailingSlash()
    {
        var previous = Environment.GetEnvironmentVariable("CYBERSECOS_API_URL");
        Environment.SetEnvironmentVariable("CYBERSECOS_API_URL", null);
        try
        {
            var options = ApiClientOptions.FromEnvironment("http://localhost:9000");
            Assert.Equal("http://localhost:9000/", options.BaseAddress.ToString());
        }
        finally
        {
            Environment.SetEnvironmentVariable("CYBERSECOS_API_URL", previous);
        }
    }

    [Fact]
    public void DefaultsToLocalBackend()
    {
        Assert.Equal(ApiClientOptions.DefaultBaseUrl, new ApiClientOptions().BaseAddress.ToString());
    }
}
