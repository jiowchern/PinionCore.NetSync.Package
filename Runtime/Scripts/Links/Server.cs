using PinionCore.NetSync.Extensions;
using PinionCore.Remote;
using PinionCore.Remote.Soul;

using Unity.Properties;
using UnityEngine;

namespace PinionCore.NetSync
{

    public class Server : MonoBehaviour , PinionCore.Remote.IEntry
    {
        public readonly IProtocol Protocol;
        public readonly Linstener Listener;
        readonly IListenable _Listener;
        public struct BinderCommand
        {
            public enum OperatorStatus
            {
                Add,
                Remove
            }
            public OperatorStatus Status;
            public IBinder Binder;
        }
        private readonly System.Collections.Concurrent.ConcurrentQueue<BinderCommand> _BinderOperator;
        public UnityEngine.Events.UnityEvent<BinderCommand> BinderEvent;                
        private readonly SyncService _Service;

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
            _Listener = Listener;
            Protocol = ProtocolCreator.Create();
            _Service = new PinionCore.Remote.Soul.SyncService(this, Protocol, new Serializer(Protocol.SerializeTypes), new PinionCore.Remote.InternalSerializer(), PinionCore.Memorys.PoolProvider.Shared);            
        }
         [CreateProperty] public string Hash => Protocol.VersionCode.ToHexString();

        void IBinderProvider.RegisterClientBinder(IBinder binder)
        {            
            _BinderOperator.Enqueue(new BinderCommand { Status = BinderCommand.OperatorStatus.Add, Binder = binder });
        }

        void IBinderProvider.UnregisterClientBinder(IBinder binder)
        {
            _BinderOperator.Enqueue(new BinderCommand { Status = BinderCommand.OperatorStatus.Remove, Binder = binder });
        }

        void IEntry.Update()
        {
            
        }
        public void Start()
        {
            IService service = _Service;
            _Listener.StreamableLeaveEvent += service.Join;
            _Listener.StreamableEnterEvent += service.Leave;
        }
        public void OnDestroy()
        {
            IService service = _Service;
            _Listener.StreamableLeaveEvent -= service.Join;
            _Listener.StreamableEnterEvent -= service.Leave;
        }

        public void Update()
        {            
            _Service.Update();
            while (_BinderOperator.TryDequeue(out var op))
            {
                BinderEvent.Invoke(op);                
            }
        }
    }
}
