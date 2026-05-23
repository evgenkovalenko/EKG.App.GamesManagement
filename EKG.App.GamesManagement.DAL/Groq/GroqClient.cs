using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace EKG.App.GamesManagement.DAL.Groq;

internal class GroqClient : IGroqClient
{
    private readonly ChatClient _chatClient;

    private const string SystemPrompt =
        "You are a game data extraction assistant. Extract all casino games from the provided content " +
        "and return ONLY a valid JSON array of game objects. Each object must include at minimum: " +
        "id, slug, vendor, vendorID, gameID, gameCode, enabled, url, categories. " +
        "Return empty array [] if no games found. No explanation, no markdown, just raw JSON.";

    public GroqClient(IOptions<GroqOptions> options)
    {
        var opts = options.Value;
        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(opts.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(opts.BaseUrl) });
        _chatClient = openAiClient.GetChatClient(opts.ModelName);
    }

    public async Task<string> ExtractGamesJsonAsync(string rawContent)
    {
        var response = await _chatClient.CompleteChatAsync(
        [
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(rawContent)
        ]);
        return response.Value.Content[0].Text;
    }
}
