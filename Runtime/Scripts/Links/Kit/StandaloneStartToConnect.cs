using UnityEngine;

namespace PinionCore.NetSync.Kits
{
    /// <summary>
    /// Start 時自動呼叫 Standalone.Connector.Connect()。
    /// 未指派 Connector 時,使用同物件上的 Standalone.Connector。
    /// </summary>
    public class StandaloneStartToConnect : MonoBehaviour
    {
        public Standalone.Connector Connector;
        public Standalone.Listener Listener;

        void Start()
        {            
            Connector.Connect(Listener);
        }
    }
}
