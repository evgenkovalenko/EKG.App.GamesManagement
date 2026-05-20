using EKG.Common.Model;

namespace EKG.App.GamesManagement.Model;

public class SaveGameOverrideRequest : ServiceRequestBase
{
    public Game ChangedGame { get; set; } = default!;
}
