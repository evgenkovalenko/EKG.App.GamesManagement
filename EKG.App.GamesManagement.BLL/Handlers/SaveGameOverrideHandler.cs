using System.Text.Json;
using System.Text.Json.Nodes;
using EKG.App.GamesManagement.DAL.Bitbucket;
using EKG.App.GamesManagement.Model;
using EKG.Common.App;
using Microsoft.Extensions.Options;

namespace EKG.App.GamesManagement.BLL.Handlers;

public class SaveGameOverrideHandler : BaseHandler<SaveGameOverrideRequest, SaveGameOverrideResponse>
{
    protected override string ServiceName => nameof(SaveGameOverrideHandler);

    private readonly IBitbucketRepository _bitbucket;
    private readonly BitbucketOptions _bitbucketOptions;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public SaveGameOverrideHandler(
        ServiceContext context,
        IBitbucketRepository bitbucket,
        IOptions<BitbucketOptions> bitbucketOptions) : base(context)
    {
        _bitbucket = bitbucket;
        _bitbucketOptions = bitbucketOptions.Value;
    }

    protected override async Task<SaveGameOverrideResponse> Process(SaveGameOverrideRequest request)
    {
        Validate(request);

        var changed = request.ChangedGame;
        var originalFileName = $"{changed.Vendor}_{changed.Slug}.json";

        var originalContent = await _bitbucket.GetFileAsync(_bitbucketOptions.GamesRepo, originalFileName);
        if (originalContent == null)
            throw new InvalidOperationException(GamesManagementErrors.OriginalGameNotFound);

        var diffJson = ComputeDiff(originalContent, changed);

        var overrideFileName = $"{request.DomainId}/{changed.Vendor}_{changed.Slug}.json";

        await _bitbucket.CommitFileAsync(
            _bitbucketOptions.OperatorGamesRepo,
            overrideFileName,
            diffJson,
            $"Override game {changed.Slug} for domain {request.DomainId}");

        return new SaveGameOverrideResponse();
    }

    private static string ComputeDiff(string originalJson, Game changedGame)
    {
        var originalNode = JsonNode.Parse(originalJson)!.AsObject();
        var changedNode = JsonNode.Parse(JsonSerializer.Serialize(changedGame, JsonOptions))!.AsObject();

        var diff = new JsonObject();
        foreach (var kvp in changedNode)
        {
            var changedVal = kvp.Value?.ToJsonString();
            var originalVal = originalNode[kvp.Key]?.ToJsonString();
            if (changedVal != originalVal)
                diff[kvp.Key] = kvp.Value?.DeepClone();
        }

        return diff.ToJsonString(JsonOptions);
    }

    private static void Validate(SaveGameOverrideRequest request)
    {
        if (request.DomainId <= 0)
            throw new InvalidOperationException(GamesManagementErrors.DomainIdRequired);
        if (request.ChangedGame == null)
            throw new InvalidOperationException(GamesManagementErrors.ChangedGameRequired);
        if (string.IsNullOrEmpty(request.ChangedGame.Slug))
            throw new InvalidOperationException(GamesManagementErrors.GameSlugRequired);
        if (string.IsNullOrEmpty(request.ChangedGame.Vendor))
            throw new InvalidOperationException(GamesManagementErrors.GameVendorRequired);
    }
}
