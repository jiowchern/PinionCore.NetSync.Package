using UnityEngine;

namespace PinionCore.NetSync.Kits
{
    /// <summary>
    /// Start 時自動呼叫 Direct.DirectConnector.Connect()。
    /// Direct 模式的伺服器端不需 Bind(Server.Start 自己就緒),故無對應的 StartToBind。
    /// </summary>
    public class DirectStartToConnect : MonoBehaviour
    {
        public Direct.DirectConnector Connector;
        public Server Server;

        void Start()
        {
            Connector.Connect(Server);
        }
    }
}
