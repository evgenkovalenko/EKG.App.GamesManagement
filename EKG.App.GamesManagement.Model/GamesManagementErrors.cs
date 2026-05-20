namespace EKG.App.GamesManagement.Model;

public static class GamesManagementErrors
{
    public const string GameSlugRequired = "Game slug is required.";
    public const string GameVendorRequired = "Game vendor is required.";
    public const string GameRequired = "Game is required.";
    public const string ChangedGameRequired = "ChangedGame is required.";
    public const string FilterRequired = "Filter is required.";
    public const string DomainIdRequired = "DomainId is required.";
    public const string OriginalGameNotFound = "Original game not found in repository.";
}
