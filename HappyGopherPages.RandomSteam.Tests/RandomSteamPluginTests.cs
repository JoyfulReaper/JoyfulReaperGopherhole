
using HappyGopherPages.RandomSteam.Integrations.RandomSteamGame;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HappyGopherPages.RandomSteam.Tests;

public sealed class RandomSteamPluginTests
{
    [Fact]
    public void ConfigureServices_RegistersRandomSteamClient()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new();

        new RandomSteamPlugin().ConfigureServices(services, configuration);

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsType<RandomSteamClient>(
            provider.GetRequiredService<IRandomSteamClient>());
    }

    [Fact]
    public void ConfigureServices_BindsConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RandomSteam:BaseUrl"] = "https://steam.example/",
                ["RandomSteam:TimeoutMilliseconds"] = "3000"
            })
            .Build();

        ServiceCollection services = new();
        new RandomSteamPlugin().ConfigureServices(services, configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        RandomSteamOptions options = provider
            .GetRequiredService<IOptions<RandomSteamOptions>>().Value;

        Assert.Equal("https://steam.example/", options.BaseUrl);
        Assert.Equal(3000, options.TimeoutMilliseconds);
    }

    [Theory]
    [InlineData("not-a-url", "5000")]
    [InlineData("https://steam.example/", "0")]
    public void ConfigureServices_RejectsInvalidConfiguration(
        string baseUrl, string timeout)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RandomSteam:BaseUrl"] = baseUrl,
                ["RandomSteam:TimeoutMilliseconds"] = timeout
            })
            .Build();

        ServiceCollection services = new();
        new RandomSteamPlugin().ConfigureServices(services, configuration);

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<RandomSteamOptions>>().Value);
    }
}
