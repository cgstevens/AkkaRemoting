using Akka.Actor;
using Topshelf;
using Akka.Configuration.Hocon;
using System.Configuration;
using log4net;
using AkkaRemoting.Actors;

namespace AkkaRemoting.ServiceWorker
{
    internal class WorkerService : ServiceControl
    {
        private IActorRef _jobMaster;
        private IActorRef _remoteJob;
        private IActorRef _remoteJobTracker;
        private IActorRef _paymentJobMaster;
        private IActorRef _payrollJobMaster;
        public HostControl _hostControl;
        
        public bool Start(HostControl hostControl)
        {
            _hostControl = hostControl;
            InitializeCluster();
            return true;
        }
        
        public bool Stop(HostControl hostControl)
        {
            //do your cleanup here
            Program.MySystem.Terminate();            
            return true;
        }

        public void InitializeCluster()
        {
            Program.MySystem = ActorSystem.Create(ActorPaths.ActorSystem);
            GlobalContext.Properties["ipaddress"] = GetIpAddressFromConfig();
            _jobMaster = Program.MySystem.ActorOf(Props.Create(() => new JobMaster()), ActorPaths.JobMasterActor.Name);
            _remoteJob = Program.MySystem.ActorOf(Props.Create(() => new RemoteJobActor()), ActorPaths.RemoteJobActor.Name);
            _remoteJobTracker = Program.MySystem.ActorOf(Props.Create(() => new RemoteJobTracker()), ActorPaths.RemoteJobTracker.Name);                 
        }

        private string GetIpAddressFromConfig()
        {            
            var section = (AkkaConfigurationSection)ConfigurationManager.GetSection("akka");
            var config = section.AkkaConfig;

            var lighthouseConfig = config.GetConfig(ActorPaths.ActorSystem);
            
            var remoteConfig = config.GetConfig("akka.remote");
            var ipAddress = remoteConfig.GetString("helios.tcp.public-hostname");
            int port = remoteConfig.GetInt("helios.tcp.port");

            if (string.IsNullOrEmpty(ipAddress)) throw new ConfigurationException("Need to specify an explicit ipaddress. Found an undefined ipaddress in App.config.");
            if (port == 0) throw new ConfigurationException("Need to specify an explicit port. Found an undefined port in App.config.");

            var selfAddress = string.Format("{0}:{1}", ipAddress, port);

            return selfAddress;
        }
    }
}