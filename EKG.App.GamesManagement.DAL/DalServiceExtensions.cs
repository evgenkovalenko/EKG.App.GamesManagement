using EKG.App.GamesManagement.DAL.Bitbucket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EKG.App.GamesManagement.DAL;

public static class DalServiceExtensions
{
    public static IServiceCollection AddGamesManagementDal(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BitbucketOptions>(configuration.GetSection("Bitbucket"));
        services.AddHttpClient(nameof(BitbucketRepository));
        services.AddSingleton<IBitbucketRepository>(sp => new BitbucketRepository(
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<IOptions<BitbucketOptions>>()));
        return services;
    }
}
