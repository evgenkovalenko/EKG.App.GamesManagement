using System.Text.Json;
using EKG.App.GamesManagement.BLL.Publishers;
using EKG.App.GamesManagement.DAL.Bitbucket;
using EKG.App.GamesManagement.Model;
using EKG.Common.App;
using EKG.Common.GamesClient.Messages;
using Microsoft.Extensions.Options;

namespace EKG.App.GamesManagement.BLL.Handlers;

public class SaveGameHandler : BaseHandler<SaveGameRequest, SaveGameResponse>
{
    protected override string ServiceName => nameof(SaveGameHandler);

    private readonly IBitbucketRepository _bitbucket;
    private readonly GamesChangedPublisher _publisher;
    private readonly BitbucketOptions _bitbucketOptions;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public SaveGameHandler(
        ServiceContext context,
        IBitbucketRepository bitbucket,
        GamesChangedPublisher publisher,
        IOptions<BitbucketOptions> bitbucketOptions) : base(context)
    {
        _bitbucket = bitbucket;
        _publisher = publisher;
        _bitbucketOptions = bitbucketOptions.Value;
    }

    protected override async Task<SaveGameResponse> Process(SaveGameRequest request)
    {
        Validate(request);

        var game = request.Game;
        var fileName = $"{game.Vendor}_{game.Slug}.json";
        var content = JsonSerializer.Serialize(game, JsonOptions);

        await _bitbucket.CommitFileAsync(
            _bitbucketOptions.GamesRepo,
            fileName,
            content,
            $"Save game {game.Slug}");

        await _publisher.PublishAsync(new GamesChangedMessage());

        return new SaveGameResponse();
    }

    private static void Validate(SaveGameRequest request)
    {
        if (request.Game == null)
            throw new InvalidOperationException(GamesManagementErrors.GameRequired);
        if (string.IsNullOrEmpty(request.Game.Slug))
            throw new InvalidOperationException(GamesManagementErrors.GameSlugRequired);
        if (string.IsNullOrEmpty(request.Game.Vendor))
            throw new InvalidOperationException(GamesManagementErrors.GameVendorRequired);
    }
}
