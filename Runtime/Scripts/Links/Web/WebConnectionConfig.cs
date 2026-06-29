using UnityEngine;

namespace PinionCore.NetSync.Web
{
    /// <summary>
    /// WebSocket 連線設定資產。保存連線 URL,供 <see cref="WebConnector"/> 使用。
    /// </summary>
    [CreateAssetMenu(
        fileName = "WebConnectionConfig",
        menuName = "PinionCore/NetSync/Web Connection Config")]
    public class WebConnectionConfig : ConnectionConfig
    {
        [SerializeField]
        [Tooltip("WebSocket 連線位址,例如 ws://127.0.0.1:7777 或 wss://example.com/path。")]
        private string _Url = "ws://127.0.0.1:7777";

        public string Url => _Url;

        /// <summary>
        /// 從 <see cref="Url"/> 解析出的連接埠,供 Server 端 <see cref="WebListener"/> 監聽使用;
        /// 無法解析時回傳 -1。Client 端連線請直接使用 <see cref="Url"/>。
        /// </summary>
        public int Port
        {
            get
            {
                if (System.Uri.TryCreate(_Url, System.UriKind.Absolute, out var uri) && uri.Port > 0)
                {
                    return uri.Port;
                }
                return -1;
            }
        }

        public override string Describe()
        {
            return _Url;
        }
    }
}
