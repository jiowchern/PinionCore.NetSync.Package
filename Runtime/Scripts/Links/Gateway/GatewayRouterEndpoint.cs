using UnityEngine;

namespace PinionCore.NetSync.Gateways
{
    public enum GatewayRouterEndpointType
    {
        Registry,
        Session
    }

    /// <summary>
    /// GatewayRouter 的端點宿主。
    /// 與 Tcp/Web/Standalone Listener 掛在同一個 GameObject 上,
    /// 讓監聽到的連線進入 Router 的 Registry 或 Session 服務。
    /// 未指派 Router 時,會往父物件尋找 GatewayRouter。
    /// </summary>
    public class GatewayRouterEndpoint : MonoBehaviour, IListenableHost
    {
        public GatewayRouter Router;

        [Tooltip("Registry:遊戲服務註冊端點;Session:客戶端連線端點。")]
        public GatewayRouterEndpointType Endpoint;

        Linstener IListenableHost.Listener => _GetRouter().GetListener(Endpoint);

        GatewayRouter _GetRouter()
        {
            if (Router == null)
            {
                Router = GetComponentInParent<GatewayRouter>();
            }
            if (Router == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(GatewayRouterEndpoint)}] 找不到 {nameof(GatewayRouter)},請指派 Router 或掛在其子物件下。", this);
            }
            return Router;
        }
    }
}
