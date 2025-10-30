using PinionCore.NetSync.Extensions;

using PinionCore.Remote;

using Unity.Properties;

using UnityEngine;


namespace PinionCore.NetSync
{
    public class Client : MonoBehaviour
    {
        
        PinionCore.Remote.Ghost.IAgent _Agent;
        IProtocol _Protocol;
        public PinionCore.Remote.INotifierQueryable Queryer => _QueryQueryer();

        private Remote.Ghost.IAgent _QueryQueryer()
        {
            
            if (_Agent == null)
            {
                _Agent = PinionCore.Remote.Client.Provider.CreateAgent(_QueryProtocol());                
            }

            return _Agent;
        }

        public IProtocol Protocol => _QueryProtocol();

        private IProtocol _QueryProtocol()
        {
            if (_Protocol == null)
            {
                _Protocol = ProtocolCreator.Create();
            }
            return _Protocol;
        }

        [CreateProperty] public string Hash => _QueryProtocol().VersionCode.ToHexString();
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
            _QueryQueryer().HandleMessage();
            _QueryQueryer().HandlePackets();
        }


    }
}
