using System;
using Akka.Actor;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Akka.Event;
using System.Threading.Tasks;
using System.Configuration;
using AkkaRemoting.Commands;

namespace AkkaRemoting.Actors
{
    public class RemoteJobSubscriber : ReceiveActor
    {
        public class ValidateRemote
        {
        }

        public class RemoteFound
        {
            public string Key { get; private set; }
            public ActorIdentity ActorIdentity { get; private set; }

            public RemoteFound(string key, ActorIdentity actorIdentity)
            {
                ActorIdentity = actorIdentity;
                Key = key;
            }
        }

        private readonly ILoggingAdapter _logger = Context.GetLogger();
        private ConcurrentDictionary<string, ActorSelection> _akkaWorkerActorSelection = new ConcurrentDictionary<string, ActorSelection>();
        private ConcurrentDictionary<string, IActorRef> _akkaWorkerIActorRef = new ConcurrentDictionary<string, IActorRef>();
        protected ICancelable ValidateRemoteJob;
        private IActorRef _subscriberActorRef;
        public RemoteJobSubscriber(IActorRef subscriberActorRef)
        {
            _subscriberActorRef = subscriberActorRef;

            var val = ConfigurationManager.AppSettings["AkkaWorkerNodes"];
            
            BecomeReady();
        }

        private void BecomeReady()
        {
            ValidateRemoteJob = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.FromMilliseconds(5),
                    TimeSpan.FromSeconds(10), Self, new ValidateRemote(), Self);

            Become(Ready);
        }

        protected override void PostStop()
        {
            ValidateRemoteJob.Cancel();
            base.PostStop();
        }

        public void Ready()
        {
            Receive<ValidateRemote>(ic =>
            {
                var nodes = new List<string>(ConfigurationManager.AppSettings["AkkaWorkerNodes"].Split(new char[] { ';' }));

                foreach (var node in nodes)
                {
                    ActorSelection server = Context.ActorSelection(node + "/user/jobmaster");
                    _akkaWorkerActorSelection.AddOrUpdate(node, server, (k, v) => server);
                }

                foreach (var node in _akkaWorkerActorSelection)
                {
                    if(!_akkaWorkerIActorRef.ContainsKey(node.Key))
                    {
                        _akkaWorkerIActorRef.AddOrUpdate(node.Key, ActorRefs.Nobody, (k, v) => ActorRefs.Nobody);
                    }
                    _logger.Warning("Identify: {0}", node.Key);
                    

                    node.Value.Ask(new Identify(node.Key), TimeSpan.FromSeconds(2))
                    .ContinueWith(
                        tr =>
                        {

                            if (tr.IsFaulted)
                            {
                                _logger.Error(tr.Exception, "Faulted ValidateRemote : {0} ", node.Key);
                                return new RemoteFound(node.Key, null);

                            }
                            if (tr.IsCanceled)
                            {
                                _logger.Error(tr.Exception, "Canceled ValidateRemote; Did we timeout trying to validate remote? : {0}", node.Key);
                                return new RemoteFound(node.Key, null);
                            }
                            var identity = (ActorIdentity)tr.Result;

                            return new RemoteFound(identity.MessageId.ToString(), (ActorIdentity)tr.Result);
                        }, TaskContinuationOptions.AttachedToParent & TaskContinuationOptions.ExecuteSynchronously).PipeTo(Self);

                }
            });

            Receive<RemoteFound>(ic => ic.ActorIdentity == null, ic =>
            {
                // Do nothing as we already logged the error.
            });

            Receive<RemoteFound>(ic =>
            {
                var key = ic.ActorIdentity.MessageId.ToString();

                if (_akkaWorkerIActorRef.ContainsKey(key))
                {
                    if (ic.ActorIdentity.Subject != null)
                    {
                        _akkaWorkerIActorRef.AddOrUpdate(key, ic.ActorIdentity.Subject, (k, v) => ic.ActorIdentity.Subject);
                        Context.Watch(_akkaWorkerIActorRef[key]);

                        _logger.Warning("SubscribeToAllJobs: {0}", key);
                        _akkaWorkerIActorRef[key].Tell(new SubscribeToAllJobs(_subscriberActorRef), _subscriberActorRef);
                    }
                    else
                    {
                        _logger.Warning("Unable to identify {0}", key);
                    }
                }                
            });
            

            Receive<Terminated>(ic =>
            {
                _logger.Error("Address Terminated: {0}", ic.ActorRef.Path.ToString());

                foreach (var actor in _akkaWorkerIActorRef)
                {
                    if (actor.Value.Equals(ic.ActorRef))
                    {
                        Context.Unwatch(actor.Value);
                        _akkaWorkerIActorRef.AddOrUpdate(actor.Key, ActorRefs.Nobody, (k, v) => ActorRefs.Nobody);
                    }
                }

            });

            ReceiveAny(task =>
            {
                _logger.Info(" [x] Oh Snap! RemoteJobSubscriber.Ready.ReceiveAny: \r\n{0}", task);
            });
        }
    }    
}
