
using HappyGopherPages.RandomSteam.Integrations.RandomSteamGame;
using System.Net;
using System.Net.Http.Json;

namespace HappyGopherPages.RandomSteam.Tests;

public sealed class RandomSteamClientTests
{
    [Fact]
    public async Task GetRandomGameAsync_ReturnsGameDetails()
    {
        Uri? requestedUri = null;

        using HttpClient http = CreateClient((request, _) =>
        {
            requestedUri = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id = 400,
                    name = "Portal",
                    description = "A puzzle game",
                    playtimeForever = 120
                })
            });
        });

        RandomSteamClient client = new(http);

        GameDetails? game = await client.GetRandomGameAsync(76561198000000000);

        Assert.NotNull(game);
        Assert.Equal(400, game.Id);
        Assert.Equal("Portal", game.Name);
        Assert.Equal("A puzzle game", game.Description);
        Assert.Equal(120, game.PlaytimeForever);

        Assert.Equal(
            "https://randomsteam.example/api/steam/random-game/details?userId=76561198000000000",
            requestedUri?.ToString());
    }

    [Fact]
    public async Task GetRandomGameAsync_ReturnsNullForNotFound()
    {
        using HttpClient http = CreateClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        RandomSteamClient client = new(http);

        GameDetails? game = await client.GetRandomGameAsync(76561198000000000);

        Assert.Null(game);
    }

    [Fact]
    public async Task GetRandomGameAsync_ThrowsForServerError()
    {
        using HttpClient http = CreateClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        RandomSteamClient client = new(http);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetRandomGameAsync(76561198000000000));
    }

    [Fact]
    public async Task GetRandomGameAsync_RejectsInvalidSteamId()
    {
        using HttpClient http = CreateClient((_, _) =>
            throw new InvalidOperationException("HTTP should not be called"));

        RandomSteamClient client = new(http);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            client.GetRandomGameAsync(0));
    }

    [Fact]
    public async Task GetRandomGameAsync_HonorsCancellation()
    {
        using HttpClient http = CreateClient(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        RandomSteamClient client = new(http);

        using CancellationTokenSource cts = new();

        Task<GameDetails?> request = client.GetRandomGameAsync(
            76561198000000000, cts.Token);

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
    }

    private static HttpClient CreateClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    {
        return new HttpClient(new FakeHandler(respond))
        {
            BaseAddress = new Uri("https://randomsteam.example/")
        };
    }

    private sealed class FakeHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return respond(request, cancellationToken);
        }
    }
}
