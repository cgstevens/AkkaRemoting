using Akka.Actor;
using Akka.Routing;

namespace AkkaRemoting.Commands
{
    public interface ISubscribeToAllJobs : IConsistentHashable
    {
        IActorRef Requestor { get; }
    }
    public interface IUnSubscribeToAllJobs : IConsistentHashable
    {
        IActorRef Requestor { get; }
    }

    public interface IStatusUpdate
    {
    }

}