
using HappyGopher.Extensibility;
using HappyGopherPages.Quotes.Integrations.HappyQotd;
using HappyGopherPages.Quotes.Pages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HappyGopherPages.Quotes;

public sealed class QuotesPlugin : IHappyGopherPlugin
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>($"{HappyQotdOptions.SectionName}:Enabled"))
        {
            return;
        }

        services.AddOptions<HappyQotdOptions>()
            .Bind(configuration.GetSection(HappyQotdOptions.SectionName))
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
                "HappyQotd:BaseUrl must be an absolute URI when the integration is enabled.")
            .Validate(
                options => options.TimeoutMilliseconds > 0,
                "HappyQotd:TimeoutMilliseconds must be positive when the integration is enabled.")
            .ValidateOnStart();

        services.AddHttpClient<IHappyQotdClient, HappyQotdClient>((serviceProvider, client) =>
        {
            HappyQotdOptions options = serviceProvider
                .GetRequiredService<IOptions<HappyQotdOptions>>()
                .Value;

            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromMilliseconds(options.TimeoutMilliseconds);
        });
        services.AddScoped<IGopherPage, QuoteOfTheDayPage>();
        services.AddScoped<IGopherPage, RandomQuotePage>();
    }
}
