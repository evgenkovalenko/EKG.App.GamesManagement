using EKG.Common.Model;

namespace EKG.App.GamesManagement.Model;

public class SaveOperatorFilterRequest : ServiceRequestBase
{
    public GameFilter Filter { get; set; } = default!;
}
