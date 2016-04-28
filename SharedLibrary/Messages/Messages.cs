using Akka.Actor;
using System;

namespace AkkaRemoting.Commands
{
    public enum JobStatus
    {
        Running,
        Starting,
        Finished,
        Terminated,
        IsFaulted,
        New
    }

    public class SubscriptionStatus
    {
        public SubscriptionStatus(bool isSubscribed, string message)
        {
            IsSubscribed = isSubscribed;
            Message = message;
        }

        public bool IsSubscribed { get; private set; }
        public string Message { get; private set; }
    }

    public class SubscribeToAllJobs : ISubscribeToAllJobs
    {
        public SubscribeToAllJobs(IActorRef requestor)
        {
            Requestor = requestor;
        }

        public IActorRef Requestor { get; private set; }
        public object ConsistentHashKey { get { return Requestor; } }
    }

    public class UnSubscribeToAllJobs : IUnSubscribeToAllJobs
    {
        public UnSubscribeToAllJobs(IActorRef requestor)
        {
            Requestor = requestor;
        }

        public IActorRef Requestor { get; private set; }
        public object ConsistentHashKey { get { return Requestor; } }
    }

    [Serializable]
    public class JobStatusUpdate : IStatusUpdate
    {
        public JobStatusUpdate() { }

        public JobStatusUpdate(string job)
        {
            Job = job;
            StartTime = DateTime.UtcNow;
            CurrentTime = DateTime.UtcNow;
            Status = JobStatus.New;
        }

        public string Job { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime CurrentTime { get; set; }
        public DateTime? EndTime { get; set; }
        public JobStatus Status { get; set; }

        public TimeSpan WorkingElapsed
        {
            get
            {
                return ((EndTime ?? CurrentTime) - StartTime);
            }
        }

        public TimeSpan TotalElapsed
        {
            get
            {
                return ((EndTime ?? DateTime.UtcNow) - StartTime);
            }
        }

        public string StatusToString
        {
            get { return Status.ToString(); }
        }
    }
}
