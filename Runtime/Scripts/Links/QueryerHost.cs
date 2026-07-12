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
    }
}
