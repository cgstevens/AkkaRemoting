using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using AkkaRemoting.Commands;
using AkkaRemoting.States;
using log4net;

namespace AkkaRemoting.Actors
{
    public class JobMaster : ReceiveActor, IWithUnboundedStash
    {
        public class TellJobSubscribersTheStatus
        {
        }

        public IStash Stash { get; set; }
        protected ConcurrentDictionary<string, IActorRef> JobSubscribers = new ConcurrentDictionary<string, IActorRef>();
        private readonly ILoggingAdapter _logger = Context.GetLogger();
        protected ICancelable ValidateRemoteJob;

        public JobMaster()
        {
            ValidateRemoteJob = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.FromMilliseconds(5),
                    TimeSpan.FromSeconds(10), Self, new TellJobSubscribersTheStatus(), Self);
            Ready();
        }

        protected override void PostStop()
        {
            ValidateRemoteJob.Cancel();
            base.PostStop();
        }

        private void Ready()
        {           

            Receive<Terminated>(terminated =>
            {
                var jobSubscribers = JobSubscribers.ToList();

                if (JobSubscribers.ContainsKey(terminated.ActorRef.Path.ToString()))
                {
                    Context.Unwatch(terminated.ActorRef);
                    IActorRef actor;
                    JobSubscribers.TryRemove(terminated.ActorRef.Path.ToString(), out actor);
                    _logger.Error("JobSuscriber: Remote connection [{0}] died", actor);
                }
                else
                {
                    _logger.Error("Remote connection [{0}] died", terminated.ActorRef);
                }
            });
            
            Receive<ISubscribeToAllJobs>(ic =>
            {
                var sender = Sender;
                _logger.Info("{0} : Subscribing to all jobs", ic.Requestor);
                if (!JobSubscribers.ContainsKey(sender.Path.ToString()))
                {
                    if (JobSubscribers.TryAdd(sender.Path.ToString(), sender))
                    {
                        Context.Watch(sender);
                        sender.Tell(new SubscriptionStatus(true, string.Format("Successfully subscribed to all jobs: {0}", sender)));
                    }
                }
                else
                {
                    sender.Tell(new SubscriptionStatus(true, string.Format("Already subscribed to all jobs: {0}", sender)));
                }
            });

            Receive<IUnSubscribeToAllJobs>(ic =>
            {
                _logger.Info("{0} : UnSubscribing to all jobs", ic.Requestor);
                Context.Unwatch(ic.Requestor);
                IActorRef actor;
                JobSubscribers.TryRemove(ic.Requestor.Path.ToString(), out actor);
                ic.Requestor.Tell(new SubscriptionStatus(false, string.Format("Successfully unsubscribed to all jobs: {0}", ic.Requestor.Path)));
            });

            Receive<TellJobSubscribersTheStatus>(ic =>
            {
                var subscribers = JobSubscribers.ToList();

                foreach (var jobSubscriber in subscribers)
                {
                    var workerInfo = new JobWorkerInfo(GlobalContext.Properties["ipaddress"].ToString(), GlobalContext.Properties["ipaddress"].ToString());
                    workerInfo.JobStatusUpdate = new JobStatusUpdate(GlobalContext.Properties["ipaddress"].ToString());
                    workerInfo.JobStatusUpdate.EndTime = DateTime.UtcNow;
                    workerInfo.JobStatusUpdate.Status = JobStatus.Finished;

                    jobSubscriber.Value.Tell(workerInfo);

                }
            });

            ReceiveAny(task =>
            {
                _logger.Error(" [x] Oh Snap! JobMaster.Ready.ReceiveAny: \r\n{0}", task);
            });
        }

    }
}