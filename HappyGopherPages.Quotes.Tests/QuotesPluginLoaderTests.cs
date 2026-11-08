
using HappyGopher.Extensibility;
using HappyGopher.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.Loader;

namespace HappyGopherPages.Quotes.Tests;

public sealed class QuotesPluginLoaderTests
{
    [Fact]
    public void AddGopherPlugins_LoadsQuotesAndResolvesBothPages()
    {
        string pluginDirectory = Path.Combine(AppContext.BaseDirectory, "plugins");

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GopherPages:PluginDirectory"] = pluginDirectory,
                ["HappyQotd:Enabled"] = "true",
                ["HappyQotd:BaseUrl"] = "http://localhost:8080",
                ["HappyQotd:TimeoutMilliseconds"] = "2000"
            })
            .Build();

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(new GopherServerInfo("example.org", 70));

        services.AddGopherPlugins(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        IGopherPage[] pages = scope.ServiceProvider
            .GetServices<IGopherPage>()
            .OrderBy(page => page.Selector)
            .ToArray();

        Assert.Equal(2, pages.Length);
        Assert.Equal("/qotd", pages[0].Selector);
        Assert.Equal("/random-quote", pages[1].Selector);

        GopherLoadedPlugin loaded = Assert.Single(
            provider.GetServices<GopherLoadedPlugin>());

        Assert.Equal("JoyfulReaper.Quotes", loaded.Descriptor.Id);

        AssemblyLoadContext? loadContext =
            AssemblyLoadContext.GetLoadContext(loaded.EntryAssembly);

        Assert.NotNull(loadContext);
        Assert.NotSame(AssemblyLoadContext.Default, loadContext);
    }


    [Fact]
    public void AddGopherPlugins_WhenQotdDisabled_DoesNotRegisterQuotePages()
    {
        string pluginDirectory = Path.Combine(AppContext.BaseDirectory, "plugins");

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GopherPages:PluginDirectory"] = pluginDirectory,
                ["HappyQotd:Enabled"] = "false"
            })
            .Build();

        ServiceCollection services = new();

        services.AddGopherPlugins(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Empty(provider.GetServices<IGopherPage>());

        GopherLoadedPlugin loaded = Assert.Single(
            provider.GetServices<GopherLoadedPlugin>());

        Assert.Equal("JoyfulReaper.Quotes", loaded.Descriptor.Id);
    }

}
