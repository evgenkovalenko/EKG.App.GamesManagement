using EKG.Common.Model;

namespace EKG.App.GamesManagement.Model;

public class ImportGamesResponse : ServiceResponseBase
{
    public int GamesFound { get; set; }
    public int GamesCommitted { get; set; }
    public string? BranchName { get; set; }
    public string? PullRequestUrl { get; set; }
}
