using System.Text.Json;
using System.Text.Json.Serialization;

namespace EKG.App.GamesManagement.Model;

public class GameBonus
{
    [JsonPropertyName("contribution")]
    public double Contribution { get; set; }

    [JsonPropertyName("overridable")]
    public bool Overridable { get; set; }
}

public class GameCreation
{
    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; set; }

    [JsonPropertyName("lastModifiedUniversalID")]
    public string LastModifiedUniversalId { get; set; } = default!;

    [JsonPropertyName("newGameExpiryTime")]
    public DateTime NewGameExpiryTime { get; set; }

    [JsonPropertyName("time")]
    public DateTime Time { get; set; }

    [JsonPropertyName("universalID")]
    public string UniversalId { get; set; } = default!;
}

public class GamePlayMode
{
    [JsonPropertyName("anonymity")]
    public bool Anonymity { get; set; }

    [JsonPropertyName("fun")]
    public bool Fun { get; set; }

    [JsonPropertyName("realMoney")]
    public bool RealMoney { get; set; }
}

public class GamePopularity
{
    [JsonPropertyName("coefficient")]
    public double Coefficient { get; set; }
}

public class GameReport
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = default!;

    [JsonPropertyName("invoicingGroup")]
    public string InvoicingGroup { get; set; } = default!;
}

public class AdditionalFeature
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = default!;

    // value can be bool, string, or number depending on the feature
    [JsonPropertyName("value")]
    public JsonElement Value { get; set; }
}

public class GamePresentation
{
    // Localized string fields: key is locale code ("*" = all locales)
    [JsonPropertyName("backgroundImage")]
    public Dictionary<string, string> BackgroundImage { get; set; } = [];

    [JsonPropertyName("backgroundImage2")]
    public Dictionary<string, string> BackgroundImage2 { get; set; } = [];

    [JsonPropertyName("description")]
    public Dictionary<string, string> Description { get; set; } = [];

    [JsonPropertyName("gameName")]
    public Dictionary<string, string> GameName { get; set; } = [];

    [JsonPropertyName("iconFormat")]
    public Dictionary<string, string> IconFormat { get; set; } = [];

    // Key = size in px (e.g. "22", "44"), value = locale → URL
    [JsonPropertyName("icons")]
    public Dictionary<string, Dictionary<string, string>> Icons { get; set; } = [];

    [JsonPropertyName("logo")]
    public Dictionary<string, string> Logo { get; set; } = [];

    [JsonPropertyName("shortName")]
    public Dictionary<string, string> ShortName { get; set; } = [];

    [JsonPropertyName("thumbnail")]
    public Dictionary<string, string> Thumbnail { get; set; } = [];

    // Key = locale code, value = ordered list of thumbnails
    [JsonPropertyName("thumbnails")]
    public Dictionary<string, List<ThumbnailItem>> Thumbnails { get; set; } = [];
}

public class ThumbnailItem
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = default!;
}

public class GameProperty
{
    [JsonPropertyName("freeSpin")]
    public FreeSpinProperty? FreeSpin { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("hitFrequency")]
    public HitFrequency? HitFrequency { get; set; }

    [JsonPropertyName("license")]
    public string? License { get; set; }

    [JsonPropertyName("terminal")]
    public List<string> Terminal { get; set; } = [];

    [JsonPropertyName("width")]
    public int Width { get; set; }
}

public class FreeSpinProperty
{
    [JsonPropertyName("betValues")]
    public FreeSpinBetValues? BetValues { get; set; }

    [JsonPropertyName("support")]
    public bool Support { get; set; }
}

public class FreeSpinBetValues
{
    [JsonPropertyName("selections")]
    public List<double> Selections { get; set; } = [];
}

public class HitFrequency
{
    [JsonPropertyName("max")]
    public double Max { get; set; }

    [JsonPropertyName("min")]
    public double Min { get; set; }
}
