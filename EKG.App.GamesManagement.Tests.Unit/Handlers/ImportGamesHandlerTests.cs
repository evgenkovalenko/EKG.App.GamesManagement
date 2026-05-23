using System.IO.Compression;
using System.Text;
using System.Text.Json;
using EKG.App.GamesManagement.BLL.Handlers;
using EKG.App.GamesManagement.DAL.Bitbucket;
using EKG.App.GamesManagement.DAL.Groq;
using EKG.App.GamesManagement.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute.ExceptionExtensions;

namespace EKG.App.GamesManagement.Tests.Unit.Handlers;

public class ImportGamesHandlerTests
{
    private static readonly BitbucketOptions BitbucketOpts = new()
    {
        GamesRepoToken = "token",
        OperatorGamesRepoToken = "token",
        Workspace = "ws",
        GamesRepo = "games",
        OperatorGamesRepo = "opGames",
        Branch = "main"
    };

    private static ImportGamesHandler CreateHandler(IGroqClient groq, IBitbucketRepository bitbucket) =>
        new(groq, bitbucket, Options.Create(BitbucketOpts), NullLogger<ImportGamesHandler>.Instance);

    private static Game ValidGame(string slug = "test-game", string vendor = "Netent") => new()
    {
        Id = 1,
        Slug = slug,
        Vendor = vendor,
        GameId = slug,
        GameCode = slug,
        Enabled = true,
        Url = "https://example.com"
    };

    private static string GamesJson(params Game[] games) =>
        JsonSerializer.Serialize(games, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static Stream ToStream(string content) =>
        new MemoryStream(Encoding.UTF8.GetBytes(content));

    private static Stream ToZipStream(params (string name, string content)[] entries)
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                var entry = archive.CreateEntry(name);
                using var writer = new StreamWriter(entry.Open());
                writer.Write(content);
            }
        }
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public async Task ImportAsync_HappyPathJson_CommitsAndCreatesPr()
    {
        var game = ValidGame();
        var groq = Substitute.For<IGroqClient>();
        groq.ExtractGamesJsonAsync(Arg.Any<string>()).Returns(GamesJson(game));

        var bitbucket = Substitute.For<IBitbucketRepository>();
        bitbucket.CreatePullRequestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                 .Returns("https://bitbucket.org/pr/1");

        var handler = CreateHandler(groq, bitbucket);
        var result = await handler.ImportAsync(ToStream(GamesJson(game)), "games.json");

        Assert.True(result.Success);
        Assert.Equal(1, result.GamesFound);
        Assert.Equal(1, result.GamesCommitted);
        Assert.Equal("https://bitbucket.org/pr/1", result.PullRequestUrl);
        await bitbucket.Received(1).CommitFileAsync(
            "games", "Netent_test-game.json", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await bitbucket.Received(1).CreatePullRequestAsync(
            "games", Arg.Is<string>(b => b.StartsWith("import/")), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ImportAsync_HappyPathZip_ExtractsAllJsonsAndCreatesPr()
    {
        var game1 = ValidGame("game-1", "Netent");
        var game2 = ValidGame("game-2", "Quickspin");

        var groq = Substitute.For<IGroqClient>();
        groq.ExtractGamesJsonAsync(Arg.Any<string>()).Returns(GamesJson(game1, game2));

        var bitbucket = Substitute.For<IBitbucketRepository>();
        bitbucket.CreatePullRequestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                 .Returns("https://bitbucket.org/pr/2");

        var zipStream = ToZipStream(
            ("file1.json", GamesJson(game1)),
            ("file2.json", GamesJson(game2)));

        var handler = CreateHandler(groq, bitbucket);
        var result = await handler.ImportAsync(zipStream, "batch.zip");

        Assert.True(result.Success);
        Assert.Equal(2, result.GamesFound);
        Assert.Equal(2, result.GamesCommitted);
        await bitbucket.Received(2).CommitFileAsync(
            "games", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await bitbucket.Received(1).CreatePullRequestAsync(
            "games", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ImportAsync_AiReturnsEmptyArray_ReturnsSuccessWithZeroCounts()
    {
        var groq = Substitute.For<IGroqClient>();
        groq.ExtractGamesJsonAsync(Arg.Any<string>()).Returns("[]");

        var bitbucket = Substitute.For<IBitbucketRepository>();

        var handler = CreateHandler(groq, bitbucket);
        var result = await handler.ImportAsync(ToStream("[]"), "games.json");

        Assert.True(result.Success);
        Assert.Equal(0, result.GamesFound);
        Assert.Equal(0, result.GamesCommitted);
        await bitbucket.DidNotReceive().CommitFileAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await bitbucket.DidNotReceive().CreatePullRequestAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ImportAsync_GameWithMissingSlug_IsSkipped()
    {
        var validGame = ValidGame("valid-slug", "Netent");
        var invalidGame = ValidGame("", "Netent");

        var groq = Substitute.For<IGroqClient>();
        groq.ExtractGamesJsonAsync(Arg.Any<string>()).Returns(GamesJson(validGame, invalidGame));

        var bitbucket = Substitute.For<IBitbucketRepository>();
        bitbucket.CreatePullRequestAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                 .Returns("https://bitbucket.org/pr/3");

        var handler = CreateHandler(groq, bitbucket);
        var result = await handler.ImportAsync(ToStream("content"), "games.json");

        Assert.True(result.Success);
        Assert.Equal(2, result.GamesFound);
        Assert.Equal(1, result.GamesCommitted);
        await bitbucket.Received(1).CommitFileAsync(
            "games", "Netent_valid-slug.json", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ImportAsync_BitbucketCommitFails_PropagatesException()
    {
        var game = ValidGame();
        var groq = Substitute.For<IGroqClient>();
        groq.ExtractGamesJsonAsync(Arg.Any<string>()).Returns(GamesJson(game));

        var bitbucket = Substitute.For<IBitbucketRepository>();
        bitbucket.CommitFileAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .ThrowsAsync(new HttpRequestException("Bitbucket error"));

        var handler = CreateHandler(groq, bitbucket);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            handler.ImportAsync(ToStream("content"), "games.json"));
    }
}
