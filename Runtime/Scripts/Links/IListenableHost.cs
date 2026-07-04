namespace PinionCore.NetSync
{
    /// <summary>
    /// 可掛接監聽器(Tcp/Web/Standalone Listener)的宿主。
    /// 由 Server 與 Gateways.GatewayRouterEndpoint 實作。
    /// </summary>
    public interface IListenableHost
    {
        Linstener Listener { get; }
    }
}
