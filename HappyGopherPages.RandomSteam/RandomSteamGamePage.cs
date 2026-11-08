
using HappyGopher.Extensibility;
using HappyGopherPages.RandomSteam.Integrations.RandomSteamGame;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HappyGopherPages.RandomSteam;

[AutoRegisterGopherPage]
public sealed class RandomSteamGamePage(
    GopherServerInfo serverInfo,
    IRandomSteamClient randomSteamClient,
    ILogger<RandomSteamGamePage> logger) : IGopherPage
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

        if (request.Input is null)
        {
            await writer.WriteInfoAsync("Can't decide what to play?", cancellationToken);
            await writer.WriteInfoAsync("Let the computer decide for you.", cancellationToken);
        }
        else if (!SteamIdInputParser.TryParse(request.Input, out long steamId))
        {
            await writer.WriteInfoAsync("Invalid Steam ID.", cancellationToken);
            await writer.WriteInfoAsync("Enter a 17-digit SteamID64.", cancellationToken);
        }
        else
        {
            try
            {
                GameDetails? game = await randomSteamClient.GetRandomGameAsync(
                    steamId, cancellationToken);

                if (game is null)
                {
                    await writer.WriteInfoAsync(
                        "No game found. Check your Steam library privacy settings.",
                        cancellationToken);
                }
                else
                {
                    await writer.WriteInfoAsync($"Your game: {game.Name}", cancellationToken);
                    await writer.WriteInfoAsync($"Steam App ID: {game.Id}", cancellationToken);
                    await writer.WriteInfoAsync(
                        $"Playtime: {game.PlaytimeForever / 60.0:0.0} hours",
                        cancellationToken);

                    string[] descriptionLines = SteamDescriptionFormatter
                        .Format(game.Description)
                        .ToArray();

                    if (descriptionLines.Length > 0)
                    {
                        await writer.WriteInfoAsync(string.Empty, cancellationToken);
                        await writer.WriteInfoAsync("Description:", cancellationToken);

                        foreach (string line in descriptionLines)
                        {
                            await writer.WriteInfoAsync(line, cancellationToken);
                        }
                    }

                    if (game.Id > 0)
                    {
                        await writer.WriteMenuItemAsync(
                            type: 'h',
                            display: "View game on Steam",
                            selector: $"URL:https://store.steampowered.com/app/{game.Id}/",
                            host: serverInfo.PublicHost,
                            port: serverInfo.Port,
                            cancellationToken);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "RandomSteam API request failed.");
                await writer.WriteInfoAsync(
                    "RandomSteam is temporarily unavailable.",
                    cancellationToken);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "RandomSteam returned invalid JSON.");
                await writer.WriteInfoAsync(
                    "RandomSteam returned an invalid response.",
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("RandomSteam API request timed out.");
                await writer.WriteInfoAsync(
                    "RandomSteam request timed out.",
                    cancellationToken);
            }
        }

        await writer.WriteInfoAsync(string.Empty, cancellationToken);

        await writer.WriteMenuItemAsync(
            type: '7',
            display: "Enter Steam ID / Pick another game",
            selector: "/randomsteam",
            host: serverInfo.PublicHost,
            port: serverInfo.Port,
            cancellationToken);

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
