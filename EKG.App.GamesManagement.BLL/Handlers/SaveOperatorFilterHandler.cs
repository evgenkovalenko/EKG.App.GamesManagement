using System.Text.Json;
using EKG.App.GamesManagement.DAL.Bitbucket;
using EKG.App.GamesManagement.Model;
using EKG.Common.App;
using Microsoft.Extensions.Options;

namespace EKG.App.GamesManagement.BLL.Handlers;

public class SaveOperatorFilterHandler : BaseHandler<SaveOperatorFilterRequest, SaveOperatorFilterResponse>
{
    protected override string ServiceName => nameof(SaveOperatorFilterHandler);

    private readonly IBitbucketRepository _bitbucket;
    private readonly BitbucketOptions _bitbucketOptions;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public SaveOperatorFilterHandler(
        ServiceContext context,
        IBitbucketRepository bitbucket,
        IOptions<BitbucketOptions> bitbucketOptions) : base(context)
    {
        _bitbucket = bitbucket;
        _bitbucketOptions = bitbucketOptions.Value;
    }

    protected override async Task<SaveOperatorFilterResponse> Process(SaveOperatorFilterRequest request)
    {
        Validate(request);

        var filterJson = JsonSerializer.Serialize(request.Filter, JsonOptions);
        var filePath = $"{request.DomainId}/filter.json";

        await _bitbucket.CommitFileAsync(
            _bitbucketOptions.OperatorGamesRepo,
            filePath,
            filterJson,
            $"Save filter for domain {request.DomainId}");

        return new SaveOperatorFilterResponse();
    }

    private static void Validate(SaveOperatorFilterRequest request)
    {
        if (request.DomainId <= 0)
            throw new InvalidOperationException(GamesManagementErrors.DomainIdRequired);
        if (request.Filter == null)
            throw new InvalidOperationException(GamesManagementErrors.FilterRequired);
    }
}
