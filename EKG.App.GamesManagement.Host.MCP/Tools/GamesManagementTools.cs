using System.ComponentModel;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace EKG.App.GamesManagement.Host.MCP.Tools;

[McpServerToolType]
internal class GamesManagementTools
{
    [McpServerTool(Name = "games_save")]
    [Description("Save a game definition to the games repository. The game JSON must include at minimum: id, slug, vendor, enabled.")]
    public static async Task<string> SaveGame(
        IHttpClientFactory httpClientFactory,
        [Description("API token (X-EKG-Token) with gamesmanagement component")] string token,
        [Description("Domain ID (use 1 for the original game, or the specific operator domain ID)")] int domainId,
        [Description("Full game definition as a JSON string. Must include id, slug, vendor, enabled fields.")] string gameJson)
    {
        var client = httpClientFactory.CreateClient("gamesmanagement-api");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/games/save");
        request.Headers.Add("X-EKG-Token", token);

        JsonElement gameElement;
        try
        {
            gameElement = JsonSerializer.Deserialize<JsonElement>(gameJson);
        }
        catch (JsonException ex)
        {
            return JsonSerializer.Serialize(new { success = false, errorMessage = $"Invalid game JSON: {ex.Message}" });
        }

        request.Content = new StringContent(
            JsonSerializer.Serialize(new { DomainId = domainId, Game = gameElement }),
            Encoding.UTF8,
            "application/json");

        var response = await client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    [McpServerTool(Name = "games_save_override")]
    [Description("Save per-operator game overrides. Computes a diff of changed fields against the original and saves only the delta to the operator-games repository.")]
    public static async Task<string> SaveGameOverride(
        IHttpClientFactory httpClientFactory,
        [Description("API token (X-EKG-Token) with gamesmanagement component")] string token,
        [Description("Operator domain ID")] int domainId,
        [Description("Partial game object containing only the changed fields as a JSON string. Must include slug and vendor.")] string changedGameJson)
    {
        var client = httpClientFactory.CreateClient("gamesmanagement-api");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/games/save-override");
        request.Headers.Add("X-EKG-Token", token);

        JsonElement changedGameElement;
        try
        {
            changedGameElement = JsonSerializer.Deserialize<JsonElement>(changedGameJson);
        }
        catch (JsonException ex)
        {
            return JsonSerializer.Serialize(new { success = false, errorMessage = $"Invalid changedGame JSON: {ex.Message}" });
        }

        request.Content = new StringContent(
            JsonSerializer.Serialize(new { DomainId = domainId, ChangedGame = changedGameElement }),
            Encoding.UTF8,
            "application/json");

        var response = await client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    [McpServerTool(Name = "games_save_filter")]
    [Description("Save an operator filter that controls which vendors and games are visible for a domain.")]
    public static async Task<string> SaveFilter(
        IHttpClientFactory httpClientFactory,
        [Description("API token (X-EKG-Token) with gamesmanagement component")] string token,
        [Description("Operator domain ID")] int domainId,
        [Description("Filter object as a JSON string with optional fields: includeVendors (string[]), excludeVendors (string[]), includeGameIds (int[]), excludeGameIds (int[]). Example: {\"includeVendors\":[\"Netent\"],\"excludeGameIds\":[123]}")] string filterJson)
    {
        var client = httpClientFactory.CreateClient("gamesmanagement-api");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/games/save-filter");
        request.Headers.Add("X-EKG-Token", token);

        JsonElement filterElement;
        try
        {
            filterElement = JsonSerializer.Deserialize<JsonElement>(filterJson);
        }
        catch (JsonException ex)
        {
            return JsonSerializer.Serialize(new { success = false, errorMessage = $"Invalid filter JSON: {ex.Message}" });
        }

        request.Content = new StringContent(
            JsonSerializer.Serialize(new { DomainId = domainId, Filter = filterElement }),
            Encoding.UTF8,
            "application/json");

        var response = await client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }
}
