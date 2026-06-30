using PinionCore.NetSync.Extensions;
using PinionCore.Remote;
using PinionCore.Remote.Soul;
using System;
using Unity.Properties;
using UnityEngine;

namespace PinionCore.NetSync
{

    public class Server : MonoBehaviour , PinionCore.Remote.IEntry
    {
        public IProtocol Protocol => Provider;
        IProtocol _Protocol;

      

        public ProtocolProvider Provider;
        public readonly Linstener Listener;
        public struct BinderCommand
        {
            public enum OperatorStatus
            {
                Add,
                Remove
            }
            public OperatorStatus Status;
            public ISessionBinder Binder;
        }
        private readonly System.Collections.Concurrent.ConcurrentQueue<BinderCommand> _BinderOperator;
        public UnityEngine.Events.UnityEvent<BinderCommand> BinderEvent;
        private PinionCore.Remote.Soul.SessionEngine _Service;

        public static bool EnableLog = false;
        [UnityEngine.RuntimeInitializeOnLoadMethod()]
        public static void InitialLog()
        {
            if (Client.EnableLog)
            {
                return;
            }
            EnableLog = true;
            PinionCore.Utility.Log.Instance.RecordEvent += (msg) => UnityEngine.Debug.Log($"PinionCoreLog:{msg}");
        }
        public Server() {
            
            _BinderOperator = new System.Collections.Concurrent.ConcurrentQueue<BinderCommand>();
            Listener = new Linstener();            
        }
         [CreateProperty] public string Hash => Protocol != null ? Protocol.VersionCode.ToHexString() : "null";

        void ISessionObserver.OnSessionOpened(ISessionBinder binder)
        {
            _BinderOperator.Enqueue(new BinderCommand { Status = BinderCommand.OperatorStatus.Add, Binder = binder });
        }

        void ISessionObserver.OnSessionClosed(ISessionBinder binder)
        {
            _BinderOperator.Enqueue(new BinderCommand { Status = BinderCommand.OperatorStatus.Remove, Binder = binder });
        }

        void IEntry.Update()
        {
            
        }
        
        public void Update()
        {            
            _Service.Update();
            while (_BinderOperator.TryDequeue(out var op))
            {
                BinderEvent.Invoke(op);                
            }
        }

        public void OnDestroy()
        {
            IService service = _Service;
            IListenable listenable = Listener;
            listenable.StreamableLeaveEvent -= service.Leave;
            listenable.StreamableEnterEvent -= service.Join;
        }

        public void Start()
        {
           
            _Service = new PinionCore.Remote.Soul.SessionEngine(this, Protocol, new Serializer(Protocol.SerializeTypes), new PinionCore.Remote.InternalSerializer(), PinionCore.Memorys.PoolProvider.Shared);

            PinionCore.Remote.Soul.IService service = _Service;
            IListenable listenable = Listener;
            listenable.StreamableLeaveEvent += service.Leave;
            listenable.StreamableEnterEvent += service.Join;
        }
    }
}
