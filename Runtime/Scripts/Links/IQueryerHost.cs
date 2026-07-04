namespace PinionCore.NetSync
{
    /// <summary>
    /// 提供 ghost 查詢入口(Queryer)的元件。
    /// 由 Client 與 Gateways.GatewayClient 實作,供使用端以 QueryNotifier 取得代理物件通知。
    /// </summary>
    public interface IQueryerHost
    {
        PinionCore.Remote.INotifierQueryable Queryer { get; }
    }
}
