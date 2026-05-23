using System.IO.Compression;
using System.Text;
using System.Text.Json;
using EKG.App.GamesManagement.DAL.Bitbucket;
using EKG.App.GamesManagement.DAL.Groq;
using EKG.App.GamesManagement.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EKG.App.GamesManagement.BLL.Handlers;

public class ImportGamesHandler
{
    private readonly IGroqClient _groq;
    private readonly IBitbucketRepository _bitbucket;
    private readonly BitbucketOptions _bitbucketOptions;
    private readonly ILogger<ImportGamesHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public ImportGamesHandler(
        IGroqClient groq,
        IBitbucketRepository bitbucket,
        IOptions<BitbucketOptions> bitbucketOptions,
        ILogger<ImportGamesHandler> logger)
    {
        _groq = groq;
        _bitbucket = bitbucket;
        _bitbucketOptions = bitbucketOptions.Value;
        _logger = logger;
    }

    public async Task<ImportGamesResponse> ImportAsync(Stream fileStream, string fileName)
    {
        var rawContent = await ReadContentAsync(fileStream, fileName);

        var gamesJson = await _groq.ExtractGamesJsonAsync(rawContent);

        List<Game> games;
        try
        {
            games = JsonSerializer.Deserialize<List<Game>>(gamesJson, JsonOptions) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "AI returned invalid JSON, cannot parse games");
            return new ImportGamesResponse { Success = false, ErrorMessage = "AI returned invalid JSON.", GamesFound = 0 };
        }

        if (games.Count == 0)
            return new ImportGamesResponse { Success = true, GamesFound = 0, GamesCommitted = 0 };

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var branchName = $"import/{timestamp}";

        var committed = new List<string>();
        foreach (var game in games)
        {
            if (string.IsNullOrEmpty(game.Slug) || string.IsNullOrEmpty(game.Vendor))
            {
                _logger.LogWarning("Skipping game with missing slug or vendor (id={Id})", game.Id);
                continue;
            }

            var fileNameInRepo = $"{game.Vendor}_{game.Slug}.json";
            var content = JsonSerializer.Serialize(game, JsonOptions);
            await _bitbucket.CommitFileAsync(_bitbucketOptions.GamesRepo, fileNameInRepo, content, $"Import game {game.Slug}", branchName);
            committed.Add(fileNameInRepo);
        }

        if (committed.Count == 0)
            return new ImportGamesResponse { Success = false, ErrorMessage = "No valid games to commit.", GamesFound = games.Count };

        var description = $"Imported {committed.Count} game(s) from file `{fileName}`:\n\n" +
                          string.Join("\n", committed.Select(f => $"- {f}"));

        var prUrl = await _bitbucket.CreatePullRequestAsync(
            _bitbucketOptions.GamesRepo,
            branchName,
            $"Import {committed.Count} game(s) — {timestamp}",
            description);

        return new ImportGamesResponse
        {
            Success = true,
            GamesFound = games.Count,
            GamesCommitted = committed.Count,
            PullRequestUrl = prUrl
        };
    }

    private static async Task<string> ReadContentAsync(Stream stream, string fileName)
    {
        if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            var sb = new StringBuilder();
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            foreach (var entry in archive.Entries)
            {
                if (entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                    entry.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ||
                    entry.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
                    sb.AppendLine(await reader.ReadToEndAsync());
                }
            }
            return sb.ToString();
        }

        using var streamReader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return await streamReader.ReadToEndAsync();
    }
}
