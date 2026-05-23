using EKG.App.GamesManagement.DAL.Bitbucket;
using EKG.App.GamesManagement.DAL.Groq;
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

        services.Configure<GroqOptions>(configuration.GetSection("Groq"));
        services.AddSingleton<IGroqClient, GroqClient>();

        return services;
    }
}
