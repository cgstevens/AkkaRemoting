namespace AkkaRemoting
{
    /// <summary>
    /// Static helper class used to define paths to fixed-name actors
    /// (helps eliminate errors when using <see cref="ActorSelection"/>)
    /// </summary>
    public static class ActorPaths
    {
        public static readonly string ActorSystem = "myservice";
        public static readonly ActorMetaData JobMasterActor = new ActorMetaData("jobmaster", "akka://myservice/user/jobmaster");
        public static readonly ActorMetaData RemoteJobActor = new ActorMetaData("remotejob", "akka://myservice/user/remotejob");
        public static readonly ActorMetaData RemoteJobTracker = new ActorMetaData("remotejobtracker", "akka://myservice/user/remotejobtracker");
        public static readonly ActorMetaData RemoteJobSubscriber = new ActorMetaData("remotejobsubscriber", "akka://myservice/user/remotejobsubscriber");
    }

    /// <summary>
    /// Meta-data class
    /// </summary>
    public class ActorMetaData
    {
        public ActorMetaData(string name, string path)
        {
            Name = name;
            Path = path;
        }

        public string Name { get; private set; }
        public string Path { get; private set; }
    }
}
