using System.Collections.Generic;
using System.Net;

using PinionCore.NetSync.Extensions;

using UnityEngine;

namespace PinionCore.NetSync.Consoles
{
    /// <summary>
    /// 將客戶端(<see cref="Client"/> 或 <see cref="Gateways.GatewayClient"/>)接上
    /// <see cref="ConsoleView"/> 的主控台。
    /// 由 <see cref="NetSync.QueryerHost.Resolve"/> 取得實際連線物件,
    /// 並從該物件解析同物件上的 Connector 提供連線指令
    /// (WebGL 平台請使用 Web 或 Standalone,TCP 無法在瀏覽器使用):
    ///   Tcp        → connect ip port / connect-config
    ///   Web        → connect-web url / connect-web-config
    ///   Standalone → connect-standalone(目標由同物件上的 Standalone.ListenerLocator 描述)
    ///   共用       → disconnect / status;另有 ping / hash
    /// 並監看協議中的所有介面,介面送達後自動註冊「介面名.成員名」指令,
    /// 可直接從主控台呼叫遠端方法與讀取屬性。
    /// </summary>
    public class ClientConsole : MonoBehaviour
    {
        [Tooltip("主控台視窗。")]
        public ConsoleView View;

        [Tooltip("要監看與操作的客戶端;可為轉發型 QueryerHost,連線指令依 Resolve 後物件上的 Connector 提供。")]
        public QueryerHost QueryerHost;

        [Tooltip("協議;用來列舉要監看的介面。")]
        public ProtocolProvider Protocol;

        // Connector 於 OnEnable 時從 Resolve 後的連線物件快取;
        // 執行期改變轉發目標不會重新導向,切換連線系統屬編輯期決策。
        QueryerHost _Host;
        Tcp.TcpConnector _TcpConnector;
        Web.WebConnector _WebConnector;
        Standalone.Connector _StandaloneConnector;

        readonly List<GhostTypeWatcher> _Watchers = new List<GhostTypeWatcher>();
        readonly List<string> _CommandNames = new List<string>();

        void OnEnable()
        {
            if (View == null || Protocol == null || QueryerHost == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(ClientConsole)}] 需要指派 View、Protocol 與 QueryerHost。", this);
                enabled = false;
                return;
            }

            _Host = QueryerHost.Resolve();
            if (_Host == null || _Host.Queryer == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(ClientConsole)}] QueryerHost 未解析出有效的連線物件(轉發型請確認 Host 欄位)。", this);
                enabled = false;
                return;
            }

            _TcpConnector = _Host.GetComponent<Tcp.TcpConnector>();
            _WebConnector = _Host.GetComponent<Web.WebConnector>();
            _StandaloneConnector = _Host.GetComponent<Standalone.Connector>();

            PinionCore.Utility.Command command = View.Command;

            _Register("ping", _ShowPing);
            _Register("hash", _ShowHash);

            if (_TcpConnector != null)
            {
                command.Register<string, int>("connect", _Connect);
                _CommandNames.Add("connect");
                _Register("connect-config", _ConnectByConfig);

                _TcpConnector.ConnectResultEvent.AddListener(_OnTcpConnectResult);
                _TcpConnector.ConnectBreakEvent.AddListener(_OnTcpConnectBreak);
            }

            if (_WebConnector != null)
            {
                command.Register<string>("connect-web", _ConnectWeb);
                _CommandNames.Add("connect-web");
                _Register("connect-web-config", _ConnectWebByConfig);

                _WebConnector.ConnectResultEvent.AddListener(_OnWebConnectResult);
                _WebConnector.ConnectBreakEvent.AddListener(_OnWebConnectBreak);
            }

            if (_StandaloneConnector != null)
            {
                _Register("connect-standalone", _ConnectStandalone);
            }

            if (_TcpConnector != null || _WebConnector != null || _StandaloneConnector != null)
            {
                _Register("disconnect", _Disconnect);
                _Register("status", _ShowStatus);
            }

            PinionCore.Remote.IProtocol protocol = Protocol;
            PinionCore.Utility.Console.IViewer viewer = View;
            foreach (System.Type type in protocol.GetInterfaceProvider().Types)
            {
                _Watchers.Add(new GhostTypeWatcher(_Host.Queryer, type, command, viewer));
            }
        }

        void OnDisable()
        {
            foreach (GhostTypeWatcher watcher in _Watchers)
            {
                watcher.Dispose();
            }

            _Watchers.Clear();

            if (View != null)
            {
                foreach (var name in _CommandNames)
                {
                    View.Command.Unregister(name);
                }
            }

            _CommandNames.Clear();

            if (_TcpConnector != null)
            {
                _TcpConnector.ConnectResultEvent.RemoveListener(_OnTcpConnectResult);
                _TcpConnector.ConnectBreakEvent.RemoveListener(_OnTcpConnectBreak);
            }

            if (_WebConnector != null)
            {
                _WebConnector.ConnectResultEvent.RemoveListener(_OnWebConnectResult);
                _WebConnector.ConnectBreakEvent.RemoveListener(_OnWebConnectBreak);
            }

            _Host = null;
            _TcpConnector = null;
            _WebConnector = null;
            _StandaloneConnector = null;
        }

        void _Register(string name, System.Action executer)
        {
            View.Command.Register(name, executer);
            _CommandNames.Add(name);
        }

        void _ShowPing()
        {
            if (_Host is Client client)
            {
                View.WriteLine($"ping:{client.Ping}");
                return;
            }

            if (_Host is Gateways.GatewayClient gateway)
            {
                View.WriteLine($"ping:{gateway.Ping}");
                return;
            }

            View.WriteLine("ping:n/a");
        }

        void _ShowHash()
        {
            PinionCore.Remote.IProtocol protocol = Protocol;
            View.WriteLine($"protocol:{protocol.VersionCode.ToHexString()}");
        }

        void _Connect(string ip, int port)
        {
            if (!IPAddress.TryParse(ip, out IPAddress address))
            {
                View.WriteLine($"invalid ip: {ip}");
                return;
            }

            View.WriteLine($"tcp connecting {ip}:{port} ...");
            _TcpConnector.Connect(new IPEndPoint(address, port));
        }

        void _ConnectByConfig()
        {
            View.WriteLine("tcp connecting by config ...");
            _TcpConnector.Connect();
        }

        void _ConnectWeb(string url)
        {
            View.WriteLine($"web connecting {url} ...");
            _WebConnector.Connect(url);
        }

        void _ConnectWebByConfig()
        {
            View.WriteLine("web connecting by config ...");
            _WebConnector.Connect();
        }

        void _ConnectStandalone()
        {
            Standalone.ListenerLocator locator = _Host.GetComponent<Standalone.ListenerLocator>();
            if (locator == null)
            {
                View.WriteLine($"standalone listener locator not found on {_Host.gameObject.name}");
                return;
            }

            Standalone.Listener listener = locator.Find();
            if (listener == null)
            {
                View.WriteLine($"standalone listener not found (scene:{locator.SceneName} object:{locator.ObjectName})");
                return;
            }

            _StandaloneConnector.Connect(listener);
            View.WriteLine(_StandaloneConnector.IsConnect() ? "standalone connected." : "standalone connect failed.");
        }

        void _Disconnect()
        {
            if (_TcpConnector != null && _TcpConnector.CurrentStatus != Tcp.TcpConnector.ConnectorStatus.Offline)
            {
                _TcpConnector.Disconnect();
                View.WriteLine("tcp disconnected.");
            }

            if (_WebConnector != null && _WebConnector.IsConnected)
            {
                _WebConnector.Disconnect();
                View.WriteLine("web disconnected.");
            }

            if (_StandaloneConnector != null && _StandaloneConnector.IsConnect())
            {
                _StandaloneConnector.Disconnect();
                View.WriteLine("standalone disconnected.");
            }
        }

        void _ShowStatus()
        {
            if (_TcpConnector != null)
            {
                View.WriteLine($"tcp:{_TcpConnector.CurrentStatus} recv:{_TcpConnector.BytesReceived} sent:{_TcpConnector.BytesSent}");
            }

            if (_WebConnector != null)
            {
                View.WriteLine($"web:{_WebConnector.CurrentStatus} recv:{_WebConnector.BytesReceived} sent:{_WebConnector.BytesSent}");
            }

            if (_StandaloneConnector != null)
            {
                View.WriteLine($"standalone:{_StandaloneConnector.CurrentStatus} recv:{_StandaloneConnector.BytesReceived} sent:{_StandaloneConnector.BytesSent}");
            }
        }

        void _OnTcpConnectResult(Tcp.TcpConnector.ConnectResult result)
        {
            View.WriteLine($"tcp connect result: {result}");
        }

        void _OnTcpConnectBreak()
        {
            View.WriteLine("tcp connection break.");
        }

        void _OnWebConnectResult(Web.WebConnector.ConnectResult result)
        {
            View.WriteLine($"web connect result: {result}");
        }

        void _OnWebConnectBreak()
        {
            View.WriteLine("web connection break.");
        }
    }
}
