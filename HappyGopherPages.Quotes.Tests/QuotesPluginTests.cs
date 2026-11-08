
using HappyGopher.Extensibility;
using HappyGopherPages.Quotes.Integrations.HappyQotd;
using HappyGopherPages.Quotes.Pages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HappyGopherPages.Quotes.Tests;

public sealed class QuotesPluginTests
{
    [Fact]
    public void ConfigureServices_WhenEnabled_RegistersClientAndPages()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HappyQotd:Enabled"] = "true"
            })
            .Build();

        ServiceCollection services = new();

        new QuotesPlugin().ConfigureServices(services, configuration);

        Assert.Contains(services, service => service.ServiceType == typeof(IHappyQotdClient));

        Assert.Contains(services, service =>
            service.ServiceType == typeof(IGopherPage) &&
            service.ImplementationType == typeof(QuoteOfTheDayPage) &&
            service.Lifetime == ServiceLifetime.Scoped);

        Assert.Contains(services, service =>
            service.ServiceType == typeof(IGopherPage) &&
            service.ImplementationType == typeof(RandomQuotePage) &&
            service.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void ConfigureServices_WhenDisabled_DoesNotRegisterClientOrPages()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HappyQotd:Enabled"] = "false"
            })
            .Build();

        ServiceCollection services = new();

        new QuotesPlugin().ConfigureServices(services, configuration);

        Assert.DoesNotContain(services, service => service.ServiceType == typeof(IHappyQotdClient));
        Assert.DoesNotContain(services, service => service.ServiceType == typeof(IGopherPage));
    }
}
