
using HappyGopher.Extensibility;
using HappyGopherPages.RandomSteam.Integrations.RandomSteamGame;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;
using System.Text.Json;

namespace HappyGopherPages.RandomSteam.Tests;

public sealed class RandomSteamGamePageTests
{
    [Fact]
    public async Task WriteAsync_ReturnsValidGopherMenu()
    {
        RandomSteamGamePage page = new(
            new GopherServerInfo("gopher.example.org", 70),
            new StubRandomSteamClient(),
            NullLogger<RandomSteamGamePage>.Instance);

        await using MemoryStream output = new();

        GopherResponseKind kind = await page.WriteAsync(
            new GopherRequest("/randomsteam", null),
            output,
            CancellationToken.None);

        string response = Encoding.UTF8.GetString(output.ToArray());

        Assert.Equal("/randomsteam", page.Selector);
        Assert.Equal(GopherResponseKind.Menu, kind);
        Assert.Contains("iRandom Steam Game Picker\t", response);
        Assert.Contains(
            "1Back to main menu\t\tgopher.example.org\t70\r\n",
            response);
        Assert.EndsWith(".\r\n", response);
    }

    [Fact]
    public void RandomSteamGamePage_IsMarkedForAutoRegistration()
    {
        Assert.True(typeof(RandomSteamGamePage).IsDefined(
            typeof(AutoRegisterGopherPageAttribute),
            inherit: false));

        Assert.True(typeof(IGopherPage).IsAssignableFrom(
            typeof(RandomSteamGamePage)));
    }


    [Fact]
    public async Task WriteAsync_ValidSteamId_DisplaysSelectedGame()
    {
        StubRandomSteamClient client = new(new GameDetails
        {
            Id = 620,
            Name = "Portal 2",
            Description = "A puzzle game",
            PlaytimeForever = 125
        });

        string response = await RenderAsync("76561198000000000", client);

        Assert.Equal(1, client.RequestCount);
        Assert.Equal(76561198000000000L, client.LastSteamId);
        Assert.Contains("Your game: Portal 2", response);
        Assert.Contains("Steam App ID: 620", response);
        Assert.Contains("Playtime: 2.1 hours", response);
        Assert.Contains(
            "hView game on Steam\tURL:https://store.steampowered.com/app/620/",
            response);
        Assert.EndsWith(".\r\n", response);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-steam-id")]
    [InlineData("7656119800000000")]
    [InlineData("765611980000000000")]
    [InlineData("00000000000000000")]
    public async Task WriteAsync_InvalidSteamId_DoesNotCallApi(string input)
    {
        StubRandomSteamClient client = new();

        string response = await RenderAsync(input, client);

        Assert.Equal(0, client.RequestCount);
        Assert.Contains("Invalid Steam ID.", response);
        Assert.Contains("Enter a 17-digit SteamID64.", response);
    }

    [Fact]
    public async Task WriteAsync_NoGameFound_DisplaysHelpfulMessage()
    {
        StubRandomSteamClient client = new();

        string response = await RenderAsync("76561198000000000", client);

        Assert.Equal(1, client.RequestCount);
        Assert.Contains("No game found.", response);
        Assert.EndsWith(".\r\n", response);
    }


    [Fact]
    public async Task WriteAsync_Subselector_PicksGameUsingSteamId()
    {
        StubRandomSteamClient client = new(new GameDetails
        {
            Id = 620,
            Name = "Portal 2"
        });

        string response = await RenderAsync(
            null,
            client,
            "/randomsteam/76561198000000000");

        Assert.Equal(1, client.RequestCount);
        Assert.Equal(76561198000000000L, client.LastSteamId);
        Assert.Contains("Your game: Portal 2", response);
        Assert.Contains(
            "1Pick another random game\t/randomsteam/76561198000000000\tgopher.example.org\t70\r\n",
            response);
        Assert.EndsWith(".\r\n", response);
    }

    [Fact]
    public async Task WriteAsync_SearchInput_IncludesRerollLink()
    {
        StubRandomSteamClient client = new(new GameDetails
        {
            Id = 400,
            Name = "Portal"
        });

        string response = await RenderAsync("76561198000000000", client);

        Assert.Equal(1, client.RequestCount);
        Assert.Contains(
            "1Pick another random game\t/randomsteam/76561198000000000\tgopher.example.org\t70\r\n",
            response);
    }

    [Theory]
    [InlineData("/randomsteam/")]
    [InlineData("/randomsteam/not-a-steam-id")]
    [InlineData("/randomsteam/76561198000000000/extra")]
    public async Task WriteAsync_InvalidSubselector_DoesNotCallApi(string selector)
    {
        StubRandomSteamClient client = new();

        string response = await RenderAsync(null, client, selector);

        Assert.Equal(0, client.RequestCount);
        Assert.Contains("Invalid Steam ID.", response);
        Assert.DoesNotContain("1Pick another random game", response);
        Assert.EndsWith(".\r\n", response);
    }


    private static async Task<string> RenderAsync(
        string? input,
        IRandomSteamClient client,
        string selector = "/randomsteam")
    {
        RandomSteamGamePage page = new(
            new GopherServerInfo("gopher.example.org", 70),
            client,
            NullLogger<RandomSteamGamePage>.Instance);

        await using MemoryStream output = new();

        GopherResponseKind result = await page.WriteAsync(
            new GopherRequest(selector, input),
            output,
            CancellationToken.None);

        Assert.Equal(GopherResponseKind.Menu, result);

        return Encoding.UTF8.GetString(output.ToArray());
    }


    [Theory]
    [InlineData("http", "RandomSteam is temporarily unavailable.")]
    [InlineData("json", "RandomSteam returned an invalid response.")]
    [InlineData("timeout", "RandomSteam request timed out.")]
    public async Task WriteAsync_ApiFailure_DisplaysHelpfulMessage(
        string failureType,
        string expectedMessage)
    {
        Exception exception = failureType switch
        {
            "http" => new HttpRequestException("Service unavailable"),
            "json" => new JsonException("Invalid JSON"),
            "timeout" => new TaskCanceledException("Request timed out"),
            _ => throw new ArgumentOutOfRangeException(nameof(failureType))
        };

        string response = await RenderAsync(
            "76561198000000000",
            new ThrowingRandomSteamClient(exception));

        Assert.Contains(expectedMessage, response);
        Assert.Contains("7Enter Steam ID / Pick another game", response);
        Assert.EndsWith(".\r\n", response);
    }


    [Fact]
    public async Task WriteAsync_Description_StripsHtmlAndDecodesEntities()
    {
        StubRandomSteamClient client = new(new GameDetails
        {
            Id = 620,
            Name = "Portal 2",
            Description = "<p>A <strong>clever</strong> game &amp; a lot of <em>fun</em>.</p>"
        });

        string response = await RenderAsync("76561198000000000", client);

        Assert.Contains("iDescription:\t", response);
        Assert.Contains("iA clever game & a lot of fun.\tfake\t(NULL)\t0", response);
        Assert.DoesNotContain("<strong>", response);
        Assert.DoesNotContain("&amp;", response);
    }

    [Fact]
    public async Task WriteAsync_LongDescription_WrapsAndTruncates()
    {
        StubRandomSteamClient client = new(new GameDetails
        {
            Id = 620,
            Name = "Portal 2",
            Description = string.Join(' ', Enumerable.Repeat("portal", 200))
        });

        string response = await RenderAsync("76561198000000000", client);

        string[] lines = response.Split("\r\n");
        int headingIndex = Array.FindIndex(
            lines,
            line => line.StartsWith("iDescription:\t", StringComparison.Ordinal));

        Assert.True(headingIndex >= 0);

        string[] descriptionLines = lines
            .Skip(headingIndex + 1)
            .TakeWhile(line => line.StartsWith('i') && !line.StartsWith("i\t"))
            .ToArray();

        Assert.NotEmpty(descriptionLines);

        foreach (string line in descriptionLines)
        {
            string display = line[1..line.IndexOf('\t')];
            Assert.InRange(display.Length, 1, 70);
        }

        Assert.EndsWith("...", descriptionLines[^1].Split('\t')[0]);
        Assert.EndsWith(".\r\n", response);
    }

    [Fact]
    public async Task WriteAsync_EmptyDescription_OmitsDescriptionSection()
    {
        StubRandomSteamClient client = new(new GameDetails
        {
            Id = 620,
            Name = "Portal 2",
            Description = "<p>   </p>"
        });

        string response = await RenderAsync("76561198000000000", client);

        Assert.DoesNotContain("iDescription:\t", response);
        Assert.Contains("Your game: Portal 2", response);
        Assert.EndsWith(".\r\n", response);
    }


    private sealed class ThrowingRandomSteamClient(Exception exception) : IRandomSteamClient
    {
        public Task<GameDetails?> GetRandomGameAsync(
            long steamId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromException<GameDetails?>(exception);
        }
    }


    private sealed class StubRandomSteamClient(GameDetails? game = null) : IRandomSteamClient
    {
        public int RequestCount { get; private set; }
        public long? LastSteamId { get; private set; }

        public Task<GameDetails?> GetRandomGameAsync(
            long steamId,
            CancellationToken cancellationToken = default)
        {
            RequestCount++;
            LastSteamId = steamId;
            return Task.FromResult(game);
        }
    }
}
