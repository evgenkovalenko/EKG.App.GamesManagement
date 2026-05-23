using EKG.App.GamesManagement.Model;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Schema;

namespace EKG.App.GamesManagement.DAL.Groq;

internal class GroqClient : IGroqClient
{
    private readonly ChatClient _chatClient;

    private static readonly string SystemPrompt = BuildSystemPrompt();

    private static string BuildSystemPrompt()
    {
        var schemaOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        };
        var schema = schemaOptions.GetJsonSchemaAsNode(typeof(Game),
            new JsonSchemaExporterOptions { TreatNullObliviousAsNonNullable = true });

        return $"""
            You are a casino game data extraction assistant.
            Extract every game from the provided content and return ONLY a raw JSON array — no markdown, no explanation, no code fences.
            Return [] if no games are found.

            Map every available field from the source to the following JSON schema.
            Preserve ALL fields that exist in the source — do not drop or summarise anything.
            If a field is absent in the source, omit it from the output object entirely (do not emit nulls or empty defaults).

            If the source uses flat underscore-separated column names (e.g. CSV headers like
            bonus_contribution, playMode_fun, property_width, creation_time, presentation_gameName),
            reconstruct them as nested JSON objects using the underscore as a path separator.
            For example: bonus_contribution and bonus_overridable become nested fields inside a "bonus" object;
            playMode_fun, playMode_anonymity, playMode_realMoney become fields inside a "playMode" object;
            property_width, property_height, property_license, property_terminal become fields inside a "property" object.
            Pipe-separated strings such as "en|de|fr" or "PC|iPad|Android" must be split into JSON arrays.

            Target schema for each game object:
            {schema.ToJsonString(new JsonSerializerOptions { WriteIndented = true })}
            """;
    }

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
