using EKG.App.GamesManagement.BLL.Handlers;
using EKG.App.GamesManagement.BLL.Publishers;
using EKG.App.GamesManagement.DAL.Bitbucket;
using EKG.App.GamesManagement.Model;
using EKG.Common.App;
using EKG.Common.Model.Monitoring;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EKG.App.GamesManagement.Tests.Unit.Handlers;

public class SaveGameHandlerTests
{
    private static SaveGameHandler CreateHandler(IBitbucketRepository bitbucket) =>
        new(new ServiceContext(NullLogger<ServiceContext>.Instance, Substitute.For<IMonitoring>()),
            bitbucket,
            new GamesChangedPublisher(Substitute.For<IPublishEndpoint>()),
            Options.Create(new BitbucketOptions
            {
                GamesRepoToken = "token",
                OperatorGamesRepoToken = "token",
                Workspace = "ws",
                GamesRepo = "games",
                OperatorGamesRepo = "opGames",
                Branch = "main",
            }));

    private static Game ValidGame(string slug = "test-game", string vendor = "Netent") => new()
    {
        Id = 1,
        Slug = slug,
        Vendor = vendor,
        GameId = slug,
        GameCode = slug,
        GameBundleId = slug,
        ContentProvider = "Provider",
        OriginalVendor = vendor,
        Enabled = true,
        OperatorVisible = true,
        Url = "https://example.com",
    };

    [Fact]
    public async Task SaveGame_HappyPath_CommitsToRepo()
    {
        var bitbucket = Substitute.For<IBitbucketRepository>();
        var response = await CreateHandler(bitbucket).Handle(new SaveGameRequest { Game = ValidGame(), DomainId = 1 });

        Assert.True(response.Success);
        await bitbucket.Received(1).CommitFileAsync("games", "Netent_test-game.json", Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SaveGame_MissingGame_ReturnsError()
    {
        var response = await CreateHandler(Substitute.For<IBitbucketRepository>())
            .Handle(new SaveGameRequest { DomainId = 1 });

        Assert.False(response.Success);
    }

    [Fact]
    public async Task SaveGame_MissingSlug_ReturnsError()
    {
        var game = ValidGame();
        game.Slug = "";

        var response = await CreateHandler(Substitute.For<IBitbucketRepository>())
            .Handle(new SaveGameRequest { Game = game, DomainId = 1 });

        Assert.False(response.Success);
    }

    [Fact]
    public async Task SaveGame_MissingVendor_ReturnsError()
    {
        var game = ValidGame();
        game.Vendor = "";

        var response = await CreateHandler(Substitute.For<IBitbucketRepository>())
            .Handle(new SaveGameRequest { Game = game, DomainId = 1 });

        Assert.False(response.Success);
    }

    [Fact]
    public async Task SaveGame_FileNameIsVendorUndersoreSlug()
    {
        var bitbucket = Substitute.For<IBitbucketRepository>();
        await CreateHandler(bitbucket).Handle(new SaveGameRequest { Game = ValidGame("book-of-dead", "PlaynGo"), DomainId = 1 });

        await bitbucket.Received(1).CommitFileAsync("games", "PlaynGo_book-of-dead.json", Arg.Any<string>(), Arg.Any<string>());
    }
}
