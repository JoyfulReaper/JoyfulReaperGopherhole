using HappyGopher.Extensibility;
using HappyGopherPages.RandomSteam.Integrations.RandomSteamGame;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HappyGopherPages.RandomSteam;

public sealed class RandomSteamPlugin : IHappyGopherPlugin
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RandomSteamOptions>()
            .Bind(configuration.GetSection(RandomSteamOptions.SectionName))
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out Uri? uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
                "RandomSteam:BaseUrl must be an absolute HTTP or HTTPS URL.")
            .Validate(
                options => options.TimeoutMilliseconds > 0,
                "RandomSteam:TimeoutMilliseconds must be positive.")
            .ValidateOnStart();

        services.AddHttpClient<IRandomSteamClient, RandomSteamClient>((serviceProvider, client) =>
        {
            RandomSteamOptions options = serviceProvider
                .GetRequiredService<IOptions<RandomSteamOptions>>()
                .Value;

            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromMilliseconds(options.TimeoutMilliseconds);
        });
    }
}