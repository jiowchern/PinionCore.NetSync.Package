using UnityEngine;

namespace PinionCore.NetSync
{
    /// <summary>
    /// 提供 ghost 查詢入口(Queryer)的元件基底。
    /// 使用端(handlers)以此型別序列化引用,即可在
    /// Client(直連)與 Gateways.GatewayClient(經 Router)之間隨時替換。
    /// </summary>
    public abstract class QueryerHost : MonoBehaviour, IQueryerHost
    {
        public abstract PinionCore.Remote.INotifierQueryable Queryer { get; }

        /// <summary>
        /// 實際承載連線的 host;轉發型(wrapper)子類覆寫此方法回傳被包裝的 host,
        /// 讓使用端能在其 GameObject 上以 GetComponent 解析同物件的 Connector 等元件。
        /// </summary>
        public virtual QueryerHost Resolve() => this;
    }
}
