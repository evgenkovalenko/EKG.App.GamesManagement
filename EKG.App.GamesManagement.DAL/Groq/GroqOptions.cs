namespace EKG.App.GamesManagement.DAL.Groq;

public class GroqOptions
{
    public string ApiKey { get; set; } = default!;
    public string ModelName { get; set; } = "llama-3.3-70b-versatile";
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
}
