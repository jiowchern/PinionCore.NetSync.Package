using PinionCore.Network;

namespace PinionCore.NetSync
{
    /// <summary>
    /// 可被 Connector(Tcp/Web/Standalone)啟用連線的元件。
    /// 由 Client、Gateways.GatewayClient、Gateways.GatewayRegistry 實作。
    /// </summary>
    public interface IConnectableAgent
    {
        void Enable(IStreamable streamable);
        void Disable();
    }
}
