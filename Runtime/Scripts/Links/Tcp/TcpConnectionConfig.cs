using System.Net;
using UnityEngine;

namespace PinionCore.NetSync.Tcp
{
    /// <summary>
    /// TCP 連線設定資產。保存位址與連接埠,供 <see cref="TcpConnector"/> 連線
    /// 及 <see cref="TcpListener"/> 監聽使用。
    ///
    /// 同一顆資產可同時指派給 Server 端的 <see cref="TcpListener"/> (使用 <see cref="Port"/>)
    /// 與 Client 端的 <see cref="TcpConnector"/> (使用 <see cref="Host"/> + <see cref="Port"/>),
    /// 藉此保證兩端使用相同連接埠。
    /// </summary>
    [CreateAssetMenu(
        fileName = "TcpConnectionConfig",
        menuName = "PinionCore/NetSync/Tcp Connection Config")]
    public class TcpConnectionConfig : ConnectionConfig
    {
        [SerializeField]
        [Tooltip("連線目標位址 (IPv4),Listener 監聽時忽略此欄位。")]
        private string _Host = "127.0.0.1";

        [SerializeField]
        [Tooltip("連接埠 (1-65535)。")]
        private int _Port = 7777;

        public string Host => _Host;

        public int Port => _Port;

        /// <summary>
        /// 依設定建立 <see cref="IPEndPoint"/>;若 <see cref="Host"/> 不是合法 IPv4 位址則回傳 null。
        /// </summary>
        public IPEndPoint ToEndPoint()
        {
            if (!IPAddress.TryParse(_Host, out var ip))
            {
                return null;
            }
            return new IPEndPoint(ip, _Port);
        }

        public override string Describe()
        {
            return $"{_Host}:{_Port}";
        }
    }
}
