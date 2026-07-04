using System;
using PinionCore.Remote.Soul;
using UnityEngine;

namespace PinionCore.NetSync.Gateways
{
    /// <summary>
    /// 封裝 PinionCore.Remote.Gateway.Router 的中央路由閘道。
    /// 提供兩個端點:
    /// - Registry:供遊戲服務(GatewayRegistry)註冊。
    /// - Session:供客戶端(GatewayClient)連線。
    /// 透過 GatewayRouterEndpoint 元件掛接 Tcp/Web/Standalone Listener。
    /// 路由為事件驅動,不需 Update 迴圈。
    /// </summary>
    public class GatewayRouter : MonoBehaviour
    {
        public readonly Linstener RegistryListener;
        public readonly Linstener SessionListener;

        PinionCore.Remote.Gateway.Router _Router;
        Action _Dispose;

        public GatewayRouter()
        {
            RegistryListener = new Linstener();
            SessionListener = new Linstener();
            _Dispose = () => { };
        }

        public Linstener GetListener(GatewayRouterEndpointType endpoint)
        {
            return endpoint == GatewayRouterEndpointType.Registry ? RegistryListener : SessionListener;
        }

        public void Awake()
        {
            _Router = new PinionCore.Remote.Gateway.Router(new PinionCore.Remote.Gateway.Hosts.RoundRobinSelector());

            IListenable registryListenable = RegistryListener;
            IListenable sessionListenable = SessionListener;

            registryListenable.StreamableEnterEvent += _Router.Registry.Join;
            registryListenable.StreamableLeaveEvent += _Router.Registry.Leave;
            sessionListenable.StreamableEnterEvent += _Router.Session.Join;
            sessionListenable.StreamableLeaveEvent += _Router.Session.Leave;

            _Dispose = () =>
            {
                registryListenable.StreamableEnterEvent -= _Router.Registry.Join;
                registryListenable.StreamableLeaveEvent -= _Router.Registry.Leave;
                sessionListenable.StreamableEnterEvent -= _Router.Session.Join;
                sessionListenable.StreamableLeaveEvent -= _Router.Session.Leave;
                _Router.Dispose();
                _Router = null;
            };
        }

        public void OnDestroy()
        {
            _Dispose();
            _Dispose = () => { };
        }
    }
}
