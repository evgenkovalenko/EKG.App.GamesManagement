using EKG.App.GamesManagement.BLL.Handlers;
using EKG.App.GamesManagement.DAL.Bitbucket;
using EKG.App.GamesManagement.Model;
using EKG.Common.App;
using EKG.Common.Model.Monitoring;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EKG.App.GamesManagement.Tests.Unit.Handlers;

public class SaveOperatorFilterHandlerTests
{
    private static SaveOperatorFilterHandler CreateHandler(IBitbucketRepository bitbucket) =>
        new(new ServiceContext(NullLogger<ServiceContext>.Instance, Substitute.For<IMonitoring>()),
            bitbucket,
            Options.Create(new BitbucketOptions
            {
                GamesRepoToken = "token",
                OperatorGamesRepoToken = "token",
                Workspace = "ws",
                GamesRepo = "games",
                OperatorGamesRepo = "opGames",
                Branch = "main",
            }));

    [Fact]
    public async Task SaveFilter_HappyPath_CommitsToOperatorRepo()
    {
        var bitbucket = Substitute.For<IBitbucketRepository>();
        var filter = new GameFilter
        {
            IncludeVendors = ["Netent", "Quickspin"],
            ExcludeVendors = ["Wazdan"],
        };

        var response = await CreateHandler(bitbucket).Handle(new SaveOperatorFilterRequest
        {
            DomainId = 1001,
            Filter = filter,
        });

        Assert.True(response.Success);
        await bitbucket.Received(1).CommitFileAsync(
            "opGames",
            "1001/filter.json",
            Arg.Is<string>(s => s.Contains("Netent")),
            Arg.Any<string>());
    }

    [Fact]
    public async Task SaveFilter_MissingDomainId_ReturnsError()
    {
        var response = await CreateHandler(Substitute.For<IBitbucketRepository>()).Handle(new SaveOperatorFilterRequest
        {
            DomainId = 0,
            Filter = new GameFilter(),
        });

        Assert.False(response.Success);
    }

    [Fact]
    public async Task SaveFilter_MissingFilter_ReturnsError()
    {
        var response = await CreateHandler(Substitute.For<IBitbucketRepository>()).Handle(new SaveOperatorFilterRequest
        {
            DomainId = 1001,
        });

        Assert.False(response.Success);
    }

    [Fact]
    public async Task SaveFilter_FilePathIncludesDomainId()
    {
        var bitbucket = Substitute.For<IBitbucketRepository>();
        await CreateHandler(bitbucket).Handle(new SaveOperatorFilterRequest
        {
            DomainId = 3003,
            Filter = new GameFilter { ExcludeGameIds = [1, 2, 3] },
        });

        await bitbucket.Received(1).CommitFileAsync("opGames", "3003/filter.json", Arg.Any<string>(), Arg.Any<string>());
    }
}
