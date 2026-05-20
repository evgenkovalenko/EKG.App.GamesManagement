using EKG.App.GamesManagement.BLL.Handlers;
using EKG.App.GamesManagement.DAL.Bitbucket;
using EKG.App.GamesManagement.Model;
using EKG.Common.App;
using EKG.Common.Model.Monitoring;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EKG.App.GamesManagement.Tests.Unit.Handlers;

public class SaveGameOverrideHandlerTests
{
    private static SaveGameOverrideHandler CreateHandler(IBitbucketRepository bitbucket) =>
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

    private static string OriginalGameJson(string slug = "test-game", string vendor = "Netent") =>
        $$"""
        {
          "id": 1,
          "slug": "{{slug}}",
          "vendor": "{{vendor}}",
          "vendorID": 1,
          "gameID": "{{slug}}",
          "gameCode": "{{slug}}",
          "gameBundleID": "{{slug}}",
          "contentProvider": "Provider",
          "originalVendor": "{{vendor}}",
          "enabled": true,
          "operatorVisible": true,
          "url": "https://example.com",
          "helpUrl": "",
          "theoreticalPayOut": 0.95,
          "fpp": 0.2,
          "hash": 1,
          "hash2": 1,
          "categories": [],
          "languages": [],
          "restrictedTerritories": [],
          "currencies": [],
          "maintenanceWindows": []
        }
        """;

    [Fact]
    public async Task SaveOverride_HappyPath_CommitsDiffToOperatorRepo()
    {
        var bitbucket = Substitute.For<IBitbucketRepository>();
        bitbucket.GetFileAsync("games", "Netent_test-game.json").Returns(OriginalGameJson());

        var changedGame = new Game { Id = 1, Slug = "test-game", Vendor = "Netent", Enabled = false };
        var response = await CreateHandler(bitbucket).Handle(new SaveGameOverrideRequest
        {
            DomainId = 1001,
            ChangedGame = changedGame,
        });

        Assert.True(response.Success);
        await bitbucket.Received(1).CommitFileAsync(
            "opGames",
            "1001/Netent_test-game.json",
            Arg.Is<string>(s => s.Contains("enabled") && s.Contains("false")),
            Arg.Any<string>());
    }

    [Fact]
    public async Task SaveOverride_OriginalNotFound_ReturnsError()
    {
        var bitbucket = Substitute.For<IBitbucketRepository>();
        bitbucket.GetFileAsync(Arg.Any<string>(), Arg.Any<string>()).Returns((string?)null);

        var response = await CreateHandler(bitbucket).Handle(new SaveGameOverrideRequest
        {
            DomainId = 1001,
            ChangedGame = new Game { Slug = "missing-game", Vendor = "Netent" },
        });

        Assert.False(response.Success);
        Assert.Contains("not found", response.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveOverride_MissingDomainId_ReturnsError()
    {
        var response = await CreateHandler(Substitute.For<IBitbucketRepository>()).Handle(new SaveGameOverrideRequest
        {
            DomainId = 0,
            ChangedGame = new Game { Slug = "g1", Vendor = "V1" },
        });

        Assert.False(response.Success);
    }

    [Fact]
    public async Task SaveOverride_MissingChangedGame_ReturnsError()
    {
        var response = await CreateHandler(Substitute.For<IBitbucketRepository>()).Handle(new SaveGameOverrideRequest
        {
            DomainId = 1001,
        });

        Assert.False(response.Success);
    }

    [Fact]
    public async Task SaveOverride_FilePathIncludesDomainId()
    {
        var bitbucket = Substitute.For<IBitbucketRepository>();
        bitbucket.GetFileAsync("games", "Netent_g1.json").Returns(OriginalGameJson("g1", "Netent"));

        var changedGame = new Game { Slug = "g1", Vendor = "Netent", Enabled = false };
        await CreateHandler(bitbucket).Handle(new SaveGameOverrideRequest { DomainId = 2002, ChangedGame = changedGame });

        await bitbucket.Received(1).CommitFileAsync("opGames", "2002/Netent_g1.json", Arg.Any<string>(), Arg.Any<string>());
    }
}
