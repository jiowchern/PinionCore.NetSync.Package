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

        void Start()
        {
            if (Connector == null)
            {
                Connector = GetComponent<Standalone.Connector>();
            }
            if (Connector == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(StandaloneStartToConnect)}] 找不到 Standalone.Connector,無法自動連線。", this);
                return;
            }
            Connector.Connect();
        }
    }
}
