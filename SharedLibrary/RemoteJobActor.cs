using System;
using Akka.Actor;
using AkkaRemoting.Commands;

namespace AkkaRemoting.Actors
{
    /// <summary>
    /// Remote-deployed actor designed to help forward jobs to the remote hosts
    /// </summary>
    public class RemoteJobActor : ReceiveActor
    {
        public RemoteJobActor()
        {
            
            Receive<ISubscribeToAllJobs>(subscribe =>
            {
                Context.ActorSelection(ActorPaths.JobMasterActor.Path).Tell(subscribe, Sender);
            }); 
                        
            ReceiveAny(task =>
            {
                Console.WriteLine(" [x] Oh Snap! JobMaster.Ready.ReceiveAny: \r\n{0}", task);
            });
        }
    }    
}
