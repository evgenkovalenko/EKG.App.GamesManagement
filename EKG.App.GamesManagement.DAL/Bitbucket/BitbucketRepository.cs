using Microsoft.Extensions.Options;

namespace EKG.App.GamesManagement.DAL.Bitbucket;

internal class BitbucketRepository : IBitbucketRepository
{
    private readonly HttpClient _http;
    private readonly BitbucketOptions _options;

    private const string ApiBase = "https://api.bitbucket.org/2.0/repositories";

    public BitbucketRepository(IHttpClientFactory httpClientFactory, IOptions<BitbucketOptions> options)
    {
        _http = httpClientFactory.CreateClient(nameof(BitbucketRepository));
        _options = options.Value;
    }

    private string ResolveToken(string repo) =>
        repo == _options.OperatorGamesRepo ? _options.OperatorGamesRepoToken : _options.GamesRepoToken;

    public async Task<string?> GetFileAsync(string repo, string filePath)
    {
        var url = $"{ApiBase}/{_options.Workspace}/{repo}/src/{_options.Branch}/{filePath}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ResolveToken(repo));
        var response = await _http.SendAsync(req);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task CommitFileAsync(string repo, string filePath, string content, string commitMessage)
    {
        var url = $"{ApiBase}/{_options.Workspace}/{repo}/src";

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ResolveToken(repo));

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(content), filePath);
        form.Add(new StringContent(commitMessage), "message");
        form.Add(new StringContent(_options.Branch), "branch");
        req.Content = form;

        var response = await _http.SendAsync(req);
        response.EnsureSuccessStatusCode();
    }
}
