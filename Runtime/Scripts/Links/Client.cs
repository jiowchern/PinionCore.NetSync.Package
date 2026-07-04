using PinionCore.NetSync.Extensions;

using PinionCore.Remote;
using System.Diagnostics.CodeAnalysis;
using Unity.Properties;

using UnityEngine;


namespace PinionCore.NetSync
{
    public class Client : MonoBehaviour, IConnectableAgent, IQueryerHost
    {
        
        PinionCore.Remote.Ghost.IAgent _Agent;

        public IProtocol Protocol => _GetProtocol();

        public ProtocolProvider Provider;
        public PinionCore.Remote.INotifierQueryable Queryer => _QueryQueryer();

        private Remote.Ghost.IAgent _QueryQueryer()
        {

            if (_Agent == null)
            {
                _Agent = new PinionCore.Remote.Ghost.Agent(_GetProtocol());
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
