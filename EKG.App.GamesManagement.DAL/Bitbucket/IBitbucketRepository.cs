namespace EKG.App.GamesManagement.DAL.Bitbucket;

public interface IBitbucketRepository
{
    Task<string?> GetFileAsync(string repo, string filePath);
    Task CommitFileAsync(string repo, string filePath, string content, string commitMessage);
    Task CommitFileAsync(string repo, string filePath, string content, string commitMessage, string branch);
    Task<string> CreatePullRequestAsync(string repo, string sourceBranch, string title, string description);
}
