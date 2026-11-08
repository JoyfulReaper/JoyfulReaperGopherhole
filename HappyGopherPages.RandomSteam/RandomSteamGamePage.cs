
using HappyGopher.Extensibility;

namespace HappyGopherPages.RandomSteam;

[AutoRegisterGopherPage]
public sealed class RandomSteamGamePage(GopherServerInfo serverInfo) : IGopherPage
{
    public string Selector => "/randomsteam";

    public async Task<GopherResponseKind> WriteAsync(
        GopherRequest request,
        Stream output,
        CancellationToken cancellationToken)
    {
        await using GopherResponseWriter writer = new(output);

        await writer.WriteInfoAsync("Random Steam Game Picker", cancellationToken);
        await writer.WriteInfoAsync("========================", cancellationToken);
        await writer.WriteInfoAsync(string.Empty, cancellationToken);
        await writer.WriteInfoAsync("Can't decide what to play?", cancellationToken);
        await writer.WriteInfoAsync("Let the computer decide for you.", cancellationToken);
        await writer.WriteInfoAsync(string.Empty, cancellationToken);
        await writer.WriteInfoAsync("Steam library selection coming soon.", cancellationToken);
        await writer.WriteInfoAsync(string.Empty, cancellationToken);

        await writer.WriteMenuItemAsync(
            type: '1',
            display: "Back to main menu",
            selector: "",
            host: serverInfo.PublicHost,
            port: serverInfo.Port,
            cancellationToken);

        await writer.CompleteAsync(cancellationToken);

        return GopherResponseKind.Menu;
    }
}
