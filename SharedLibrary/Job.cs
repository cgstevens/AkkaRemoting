using System;
using System.Collections.Generic;
using Akka.Actor;
using AkkaRemoting.Commands;

namespace AkkaRemoting.States
{

    [Serializable]
    public class JobWorkerInfo
    {
        public JobWorkerInfo(string job, string host)
        {
            Job = job;
            Host = host;
            PreviousJobStatusUpdate = new List<JobStatusUpdate>();
        }

        public JobWorkerInfo()
        { }


        public string Job { get; set; }
        public string Host { get; set; }
        public JobStatusUpdate JobStatusUpdate { get; set; }
        public List<JobStatusUpdate> PreviousJobStatusUpdate { get; set; }
    }
}
