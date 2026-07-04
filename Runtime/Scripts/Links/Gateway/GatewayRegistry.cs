using PinionCore.NetSync.Extensions;
using PinionCore.Network;
using Unity.Properties;
using UnityEngine;

namespace PinionCore.NetSync.Gateways
{
    /// <summary>
    /// 封裝 PinionCore.Remote.Gateway.Registry 的遊戲服務註冊代理。
    /// 與 Server 掛在同一個 GameObject 上,會自動把 Router 路由過來的玩家連線餵給 Server。
    /// 透過同物件上的 Connector(Tcp/Web/Standalone)連到 GatewayRouter 的 Registry 端點。
    /// </summary>
    public class GatewayRegistry : MonoBehaviour, IConnectableAgent
    {
        [Tooltip("遊戲協議;VersionCode 用於 Router 的版本隔離,需與客戶端一致。")]
        public ProtocolProvider Provider;

        [Tooltip("Router 路由決策用的群組編號。相同群組視為同類服務(負載平衡),不同群組視為不同服務。")]
        public uint Group = 1;

        PinionCore.Remote.Gateway.Registry _Registry;
        Server _AttachedServer;

        [CreateProperty] public float Ping => _Registry != null ? _Registry.Agent.Ping : 0f;
        [CreateProperty] public string Hash => Provider != null ? ((PinionCore.Remote.IProtocol)Provider).VersionCode.ToHexString() : "null";

        /// <summary>
        /// 供遊戲服務接收玩家連線的 Listener。
        /// </summary>
        public PinionCore.Remote.Soul.IListenable Listener => _GetRegistry().Listener;

        PinionCore.Remote.Gateway.Registry _GetRegistry()
        {
            if (_Registry == null)
            {
                _Registry = new PinionCore.Remote.Gateway.Registry(Provider, Group);
            }
            return _Registry;
        }

        public void Start()
        {
            _AttachedServer = GetComponent<Server>();
            if (_AttachedServer != null)
            {
                _AttachedServer.Listener.Add(_GetRegistry().Listener);
            }
        }

        public void Update()
        {
            if (_Registry == null)
            {
                return;
            }
            _Registry.Agent.HandleMessages();
            _Registry.Agent.HandlePackets();
        }

        public void Enable(IStreamable streamable)
        {
            _GetRegistry().Agent.Enable(streamable);
        }

        public void Disable()
        {
            _GetRegistry().Agent.Disable();
        }

        public void OnDestroy()
        {
            if (_Registry == null)
            {
                return;
            }
            if (_AttachedServer != null)
            {
                _AttachedServer.Listener.Remove(_Registry.Listener);
                _AttachedServer = null;
            }
            _Registry.Dispose();
            _Registry = null;
        }
    }
}
