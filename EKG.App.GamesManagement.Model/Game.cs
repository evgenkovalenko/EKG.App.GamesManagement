using System.Text.Json;
using System.Text.Json.Serialization;

namespace EKG.App.GamesManagement.Model;

public class Game
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = default!;

    [JsonPropertyName("vendor")]
    public string Vendor { get; set; } = default!;

    [JsonPropertyName("vendorID")]
    public int VendorId { get; set; }

    [JsonPropertyName("gameID")]
    public string GameId { get; set; } = default!;

    [JsonPropertyName("gameCode")]
    public string GameCode { get; set; } = default!;

    [JsonPropertyName("gameBundleID")]
    public string GameBundleId { get; set; } = default!;

    [JsonPropertyName("contentProvider")]
    public string ContentProvider { get; set; } = default!;

    [JsonPropertyName("originalVendor")]
    public string OriginalVendor { get; set; } = default!;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("operatorVisible")]
    public bool OperatorVisible { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = default!;

    [JsonPropertyName("helpUrl")]
    public string HelpUrl { get; set; } = default!;

    [JsonPropertyName("theoreticalPayOut")]
    public double TheoreticalPayOut { get; set; }

    [JsonPropertyName("fpp")]
    public double Fpp { get; set; }

    [JsonPropertyName("hash")]
    public long Hash { get; set; }

    [JsonPropertyName("hash2")]
    public long Hash2 { get; set; }

    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = [];

    [JsonPropertyName("languages")]
    public List<string> Languages { get; set; } = [];

    [JsonPropertyName("restrictedTerritories")]
    public List<string> RestrictedTerritories { get; set; } = [];

    [JsonPropertyName("currencies")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement Currencies { get; set; }

    [JsonPropertyName("maintenanceWindows")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement MaintenanceWindows { get; set; }

    [JsonPropertyName("additional")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement Additional { get; set; }

    [JsonPropertyName("bonus")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement Bonus { get; set; }

    [JsonPropertyName("creation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement Creation { get; set; }

    [JsonPropertyName("playMode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement PlayMode { get; set; }

    [JsonPropertyName("popularity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement Popularity { get; set; }

    [JsonPropertyName("presentation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement Presentation { get; set; }

    [JsonPropertyName("property")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement Property { get; set; }

    [JsonPropertyName("report")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement Report { get; set; }

    [JsonPropertyName("ruleUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement RuleUrl { get; set; }

    [JsonPropertyName("vendorLimits")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement VendorLimits { get; set; }
}
