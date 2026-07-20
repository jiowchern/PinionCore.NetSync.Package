namespace PinionCore.NetSync
{
    /// <summary>
    /// 可被 Direct.DirectConnector 以零序列化直通模式啟用的元件,由 Direct.DirectClient 實作。
    /// 不同於 IConnectableAgent,直通模式不經 IStreamable,
    /// 連線參數是伺服器端的 IEntry(Server 本身即實作 IEntry)。
    /// </summary>
    public interface IDirectAgent
    {
        void Launch(PinionCore.Remote.IEntry entry);
        void Shutdown();
    }
}
