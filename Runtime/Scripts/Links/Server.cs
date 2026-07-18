using PinionCore.NetSync.Extensions;
using PinionCore.Remote;
using PinionCore.Remote.Soul;
using System;
using Unity.Properties;
using UnityEngine;

namespace PinionCore.NetSync
{

    public class Server : MonoBehaviour , PinionCore.Remote.IEntry , IListenableHost
    {
        Linstener IListenableHost.Listener => Listener;
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
        public UnityEngine.Events.UnityEvent<BinderCommand> BinderEvent = new UnityEngine.Events.UnityEvent<BinderCommand>();
        private PinionCore.Remote.Soul.SessionEngine _Engine;
        private PinionCore.Remote.Soul.ServiceUpdateLoop _Loop;
        private readonly FrameThreading _Threading;

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
            _Threading = new FrameThreading();
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
            // 先請求後 binder,對應舊版 _Service.Update()(處理請求)先於 binder drain 的順序
            _Threading.Update();
            while (_BinderOperator.TryDequeue(out var op))
            {
                BinderEvent.Invoke(op);
            }
        }

        public void OnDestroy()
        {
            if (_Loop == null)
                return;

            IService service = _Loop;
            IListenable listenable = Listener;
            listenable.StreamableLeaveEvent -= service.Leave;
            listenable.StreamableEnterEvent -= service.Join;

            // join 背景執行緒並 dispose SessionEngine
            IDisposable loopDispose = _Loop;
            loopDispose.Dispose();
        }

        public void Start()
        {
            // 封包收送由 ServiceUpdateLoop 的背景執行緒驅動,不吃 frame 節奏;
            // 請求消化與幫浦啟動政策由 FrameThreading 決定
            _Engine = new PinionCore.Remote.Soul.SessionEngine(this, Protocol, new Serializer(Protocol.SerializeTypes), new PinionCore.Remote.InternalSerializer(), PinionCore.Memorys.PoolProvider.Shared, _Threading);
            _Loop = new PinionCore.Remote.Soul.ServiceUpdateLoop(_Engine);

            PinionCore.Remote.Soul.IService service = _Loop;
            IListenable listenable = Listener;
            listenable.StreamableLeaveEvent += service.Leave;
            listenable.StreamableEnterEvent += service.Join;
        }
    }
}
