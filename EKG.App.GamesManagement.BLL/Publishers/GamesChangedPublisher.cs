using EKG.Common.GamesClient.Messages;
using EKG.Common.Messages;
using MassTransit;

namespace EKG.App.GamesManagement.BLL.Publishers;

public class GamesChangedPublisher : BasePublisher<GamesChangedMessage>
{
    public GamesChangedPublisher(IPublishEndpoint publishEndpoint) : base(publishEndpoint) { }
}
