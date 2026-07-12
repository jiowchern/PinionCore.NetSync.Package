using PinionCore.NetSync.Extensions;
using PinionCore.Network;
using Unity.Properties;
using UnityEngine;

namespace PinionCore.NetSync.Gateways
{
    /// <summary>
    /// 封裝 PinionCore.Remote.Gateway.Agent + AgentPool 的客戶端。
    /// 透過同物件上的 Connector(Tcp/Web/Standalone)連到 GatewayRouter 的 Session 端點,
    /// 即可經由 Router 同時與多個遊戲服務通訊,用法與 Client 相同(Queryer.QueryNotifier)。
    /// </summary>
    public class GatewayClient : QueryerHost, IConnectableAgent
    {
        [Tooltip("遊戲協議;VersionCode 用於 Router 的版本隔離,需與服務端一致。")]
        public ProtocolProvider Provider;

        PinionCore.Remote.Gateway.Hosts.AgentPool _Pool;
        PinionCore.Remote.Ghost.IAgent _Agent;

        public override PinionCore.Remote.INotifierQueryable Queryer => _GetAgent();

        float _Ping;
        [CreateProperty] public float Ping => _Ping;
        [CreateProperty] public string Hash => Provider != null ? ((PinionCore.Remote.IProtocol)Provider).VersionCode.ToHexString() : "null";

        PinionCore.Remote.Ghost.IAgent _GetAgent()
        {
            if (_Agent == null)
            {
                _Pool = new PinionCore.Remote.Gateway.Hosts.AgentPool(Provider);
                _Agent = new PinionCore.Remote.Gateway.Agent(_Pool);
            }
            return _Agent;
        }

        public void Enable(IStreamable streamable)
        {
            _GetAgent().Enable(streamable);
        }

        public void Disable()
        {
            _GetAgent().Disable();
        }

        public void Update()
        {
            var agent = _GetAgent();
            _Ping = agent.Ping;
            agent.HandleMessages();
            agent.HandlePackets();
        }

        public void OnDestroy()
        {
            if (_Pool == null)
            {
                return;
            }
            _Pool.Dispose();
            _Pool = null;
            _Agent = null;
        }
    }
}
