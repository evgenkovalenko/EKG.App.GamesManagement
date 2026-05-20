namespace EKG.App.GamesManagement.Model;

public class GameFilter
{
    public List<string>? IncludeVendors { get; set; }
    public List<string>? ExcludeVendors { get; set; }
    public List<int>? IncludeGameIds { get; set; }
    public List<int>? ExcludeGameIds { get; set; }
}
