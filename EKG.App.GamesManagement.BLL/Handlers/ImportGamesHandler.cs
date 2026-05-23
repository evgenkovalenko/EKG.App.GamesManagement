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

        var games = TryDirectJsonExtraction(rawContent);

        if (games != null)
        {
            _logger.LogInformation("Direct JSON extraction succeeded: {Count} game(s)", games.Count);
        }
        else
        {
            _logger.LogInformation("Direct extraction failed, falling back to AI");
            games = await ExtractViaAiAsync(rawContent);
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

        string? prUrl = null;
        try
        {
            prUrl = await _bitbucket.CreatePullRequestAsync(
                _bitbucketOptions.GamesRepo,
                branchName,
                $"Import {committed.Count} game(s) — {timestamp}",
                description);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Games committed to branch '{Branch}' but PR creation failed", branchName);
        }

        return new ImportGamesResponse
        {
            Success = true,
            GamesFound = games.Count,
            GamesCommitted = committed.Count,
            BranchName = branchName,
            PullRequestUrl = prUrl
        };
    }

    // Tries to deserialise games directly from the JSON without AI.
    // Handles: plain array, dict-of-games (keyed by id), and arbitrary wrappers (e.g. snapshot.games).
    // Returns null if the content is not recognised JSON or contains no game-like objects.
    private List<Game>? TryDirectJsonExtraction(string content)
    {
        JsonDocument doc;
        try { doc = JsonDocument.Parse(content); }
        catch { return null; }

        using (doc)
        {
            var gameElements = FindGameElements(doc.RootElement);
            if (gameElements.Count == 0) return null;

            var games = new List<Game>();
            foreach (var element in gameElements)
            {
                try
                {
                    var game = JsonSerializer.Deserialize<Game>(element.GetRawText(), JsonOptions);
                    if (game != null) games.Add(game);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Skipping unparseable game element");
                }
            }
            return games.Count > 0 ? games : null;
        }
    }

    // Recursively searches a JsonElement for a collection of game-like objects.
    private static List<JsonElement> FindGameElements(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            var items = element.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.Object && IsGameLike(e))
                .Select(e => e.Clone())
                .ToList();
            if (items.Count > 0) return items;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            // Object whose values are all game-like → it's a dict keyed by id (e.g. "41836": {...})
            var values = element.EnumerateObject().Select(p => p.Value).ToList();
            if (values.Count > 0 && values.All(v => v.ValueKind == JsonValueKind.Object && IsGameLike(v)))
                return values.Select(v => v.Clone()).ToList();

            // Otherwise recurse into each property to find the games collection
            foreach (var prop in element.EnumerateObject())
            {
                var found = FindGameElements(prop.Value);
                if (found.Count > 0) return found;
            }
        }

        return [];
    }

    // A JSON object is "game-like" when it has at least two of the key identifying fields.
    private static bool IsGameLike(JsonElement element)
    {
        int hits = 0;
        foreach (var field in new[] { "slug", "vendor", "gameCode", "gameID", "vendorID" })
            if (element.TryGetProperty(field, out _) && ++hits >= 2) return true;
        return false;
    }

    private async Task<List<Game>> ExtractViaAiAsync(string rawContent)
    {
        var gamesJson = await _groq.ExtractGamesJsonAsync(rawContent);
        try
        {
            return JsonSerializer.Deserialize<List<Game>>(gamesJson, JsonOptions) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "AI returned invalid JSON, cannot parse games");
            return [];
        }
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
