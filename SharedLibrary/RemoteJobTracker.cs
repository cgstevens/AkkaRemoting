using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;
using Akka.Actor;
using AkkaRemoting.States;
using Akka.Event;
using log4net;
using AkkaRemoting.Commands;

namespace AkkaRemoting.Actors
{
    public class RemoteJobTracker : ReceiveActor
    {
        public class GetAllJobWorkers { }

        public class AllJobWorkers
        {
            public AllJobWorkers(Dictionary<string, JobWorkerInfo> allJobs)
            {
                AllJobs = allJobs;
            }
            public Dictionary<string, JobWorkerInfo> AllJobs { get; set; }
        }

        private readonly ILoggingAdapter _logger = Context.GetLogger();
        private ConcurrentDictionary<string, JobWorkerInfo> AllJobs = new ConcurrentDictionary<string, JobWorkerInfo>();
        protected IActorRef JobSubscriberRef;
        private string whoAmI = GlobalContext.Properties["ipaddress"].ToString();

        public RemoteJobTracker()
        {
            JobSubscriberRef = Context.ActorOf(Props.Create(() => new RemoteJobSubscriber(Self)), ActorPaths.RemoteJobSubscriber.Name);
            Become(Ready);
        }

        public void Ready()
        {
            Receive<JobWorkerInfo>(workerInfo =>
            {
                _logger.Info("Received JobWorkerInfo from: {0}", workerInfo.Job);
                var currentJobWorkerInfo = new JobWorkerInfo();
                AllJobs.TryGetValue(workerInfo.Job, out currentJobWorkerInfo);

                if (currentJobWorkerInfo == null)
                {
                    AllJobs.AddOrUpdate(workerInfo.Job, workerInfo, (k, v) => workerInfo);
                }
                else if (workerInfo.JobStatusUpdate.StartTime >= currentJobWorkerInfo.JobStatusUpdate.StartTime)
                {
                    AllJobs[workerInfo.Job] = workerInfo;
                }

                var removeJob = new JobWorkerInfo();
                if (AllJobs.Count(x => x.Key == "No Job Found" && x.Value.Host == workerInfo.Host) == 1 && workerInfo.Job != "No Job Found")
                {
                    AllJobs.TryRemove("No Job Found", out removeJob);
                }

            });

            Receive<GetAllJobWorkers>(ic =>
            {
                foreach (var job in AllJobs.Values.ToList())
                {
                    Sender.Tell(job);
                }
            });

            Receive<SubscriptionStatus>(ic =>
            {
                _logger.Info(ic.Message);
            });

            ReceiveAny(task =>
            {
                _logger.Info(" [x] Oh Snap! RemoteJobTracker.Ready.ReceiveAny: \r\n{0}", task);
            });
        }
    }
}
