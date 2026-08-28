using HappyGopher.Extensibility;

namespace HappyGopherPages.RandomSteam;

public class RandomSteamGamePage : IGopherPage
{
    public string Selector => "/randomsteam";

    public Task<GopherResponseKind> WriteAsync(GopherRequest request, Stream output, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
