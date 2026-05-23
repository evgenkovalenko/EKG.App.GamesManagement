using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace EKG.App.GamesManagement.DAL.Groq;

internal class GroqClient : IGroqClient
{
    private readonly ChatClient _chatClient;

    private const string SystemPrompt =
        """
        You are a casino game data extraction assistant.
        Extract every game from the provided content and return ONLY a raw JSON array — no markdown, no explanation, no code fences.
        Return [] if no games are found.

        Map every available field from the source to the target schema below.
        Preserve ALL fields that exist in the source — do not drop or summarise anything.
        If a field is absent in the source, omit it from the output object entirely (do not emit nulls or empty defaults).

        Target schema (JSON property names are exact — use them as-is):

        {
          "id": <integer>,
          "slug": <string>,
          "vendor": <string>,
          "vendorID": <integer>,
          "gameID": <string>,
          "gameCode": <string>,
          "gameBundleID": <string>,
          "contentProvider": <string>,
          "originalVendor": <string>,
          "enabled": <bool>,
          "operatorVisible": <bool>,
          "url": <string>,
          "helpUrl": <string>,
          "theoreticalPayOut": <number>,
          "fpp": <number>,
          "hash": <integer>,
          "hash2": <integer>,
          "categories": [<string>, ...],
          "languages": [<string>, ...],
          "restrictedTerritories": [<string>, ...],
          "currencies": <any — copy as-is>,
          "maintenanceWindows": <any — copy as-is>,
          "vendorLimits": <any — copy as-is>,
          "ruleUrl": { "<locale>": "<url>", ... },
          "additional": {
            "<featureName>": { "displayName": <string>, "value": <bool|string|number> }
          },
          "bonus": { "contribution": <number>, "overridable": <bool> },
          "creation": {
            "lastModified": <ISO datetime>,
            "lastModifiedUniversalID": <string>,
            "newGameExpiryTime": <ISO datetime>,
            "time": <ISO datetime>,
            "universalID": <string>
          },
          "playMode": { "anonymity": <bool>, "fun": <bool>, "realMoney": <bool> },
          "popularity": { "coefficient": <number> },
          "presentation": {
            "backgroundImage": { "<locale>": "<url>", ... },
            "backgroundImage2": { "<locale>": "<url>", ... },
            "description": { "<locale>": "<text>", ... },
            "gameName": { "<locale>": "<name>", ... },
            "iconFormat": { "<locale>": "<format>", ... },
            "icons": { "<sizePx>": { "<locale>": "<url>", ... }, ... },
            "logo": { "<locale>": "<url>", ... },
            "shortName": { "<locale>": "<name>", ... },
            "thumbnail": { "<locale>": "<url>", ... },
            "thumbnails": { "<locale>": [ { "url": "<url>" }, ... ], ... }
          },
          "property": {
            "freeSpin": { "betValues": { "selections": [<number>, ...] }, "support": <bool> },
            "height": <integer>,
            "hitFrequency": { "max": <number>, "min": <number> },
            "license": <string>,
            "terminal": [<string>, ...],
            "width": <integer>
          },
          "report": { "category": <string>, "invoicingGroup": <string> }
        }
        """;

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
