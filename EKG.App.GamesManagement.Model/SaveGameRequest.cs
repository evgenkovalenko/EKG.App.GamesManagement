using EKG.Common.Model;

namespace EKG.App.GamesManagement.Model;

public class SaveGameRequest : ServiceRequestBase
{
    public Game Game { get; set; } = default!;
}
