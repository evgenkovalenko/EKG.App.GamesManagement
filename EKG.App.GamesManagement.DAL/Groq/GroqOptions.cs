namespace EKG.App.GamesManagement.DAL.Groq;

public class GroqOptions
{
    public string ApiKey { get; set; } = default!;
    public string ModelName { get; set; } = "mixtral-8x7b-32768";
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
}
