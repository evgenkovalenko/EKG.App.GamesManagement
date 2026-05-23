namespace EKG.App.GamesManagement.DAL.Groq;

public interface IGroqClient
{
    Task<string> ExtractGamesJsonAsync(string rawContent);
}
