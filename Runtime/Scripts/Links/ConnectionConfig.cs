using UnityEngine;

namespace PinionCore.NetSync
{
    /// <summary>
    /// 連線設定資產的抽象基底。將端點等「純設定資料」從場景上的連接器
    /// (<see cref="Tcp.TcpConnector"/> / <see cref="Web.WebConnector"/> 等) 抽出,
    /// 改以可共用、可序列化的 <see cref="ScriptableObject"/> 資產保存。
    ///
    /// 具體型別依傳輸層而異:TCP 需要位址 + 連接埠 (<see cref="Tcp.TcpConnectionConfig"/>),
    /// WebSocket 需要 URL (<see cref="Web.WebConnectionConfig"/>)。
    /// 此模式與 <see cref="ProtocolProvider"/> 一致:Package 只定義抽象,
    /// 應用端 (Develop) 建立 .asset 並指派到連接器的 Config 欄位。
    /// </summary>
    public abstract class ConnectionConfig : ScriptableObject
    {
        /// <summary>
        /// 人類可讀的端點描述,供 Inspector 顯示與 log 使用 (例如 "127.0.0.1:7777")。
        /// </summary>
        public abstract string Describe();
    }
}
