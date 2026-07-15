using PinionCore.NetSync.Extensions;

using PinionCore.Remote;
using System.Diagnostics.CodeAnalysis;
using Unity.Properties;

using UnityEngine;


namespace PinionCore.NetSync
{
    public class Client : QueryerHost, IConnectableAgent
    {

        PinionCore.Remote.Ghost.IAgent _Agent;

        public IProtocol Protocol => _GetProtocol();

        public ProtocolProvider Provider;
        public override PinionCore.Remote.INotifierQueryable Queryer => _QueryQueryer();

        private Remote.Ghost.IAgent _QueryQueryer()
        {

            if (_Agent == null)
            {
                var agent = new PinionCore.Remote.Ghost.Agent(_GetProtocol());
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // RPC 錯誤(如 Soul not found)訊息附上呼叫端堆疊;每次帶回傳 RPC 有捕捉成本,僅除錯環境開啟
                agent.RpcCallerStackTraceEnabled = true;
#endif
                _Agent = agent;
            }

            return _Agent;
        }



        private IProtocol _GetProtocol()
        {
            return Provider;
        }

        [CreateProperty] public string Hash => Protocol != null ? Protocol.VersionCode.ToHexString() : "null";
        float _Ping;
        //[CreateProperty] public float Ping =>  ;
        [CreateProperty] public float Ping => _Ping;

        public static bool EnableLog = false;
        
        [UnityEngine.RuntimeInitializeOnLoadMethod()]
        public static void InitialLog()
        {
            if(Server.EnableLog)
            {
                return;
            }
            EnableLog = true;
            PinionCore.Utility.Log.Instance.RecordEvent += (msg) => UnityEngine.Debug.Log($"PinionCoreLog:{msg}");
        }

        public Client()
        {
            

        }

        public void Start()
        {
            
        }

        public void Enable(Network.IStreamable streamable)
        {
            _QueryQueryer().Enable(streamable);
        }

        public void Disable()
        {
            _QueryQueryer().Disable();
        }

        // Update is called once per frame
        public void Update()
        {
            _Ping = _QueryQueryer().Ping;
            _QueryQueryer().HandleMessages();
            _QueryQueryer().HandlePackets();
        }


    }
}
