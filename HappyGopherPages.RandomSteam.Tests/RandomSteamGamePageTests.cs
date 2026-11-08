
using HappyGopher.Extensibility;
using System.Text;

namespace HappyGopherPages.RandomSteam.Tests;

public sealed class RandomSteamGamePageTests
{
    [Fact]
    public async Task WriteAsync_ReturnsValidGopherMenu()
    {
        RandomSteamGamePage page = new(
            new GopherServerInfo("gopher.example.org", 70));

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
}
