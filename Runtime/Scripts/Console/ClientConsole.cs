using System.Collections.Generic;
using System.Linq;
using System.Net;

using PinionCore.NetSync.Extensions;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace PinionCore.NetSync.Consoles
{
    /// <summary>
    /// 將客戶端(<see cref="Client"/> 或 <see cref="Gateways.GatewayClient"/>)接上
    /// <see cref="ConsoleView"/> 的主控台。
    /// 依指派的 Connector 提供連線指令(WebGL 平台請使用 Web 或 Standalone,TCP 無法在瀏覽器使用):
    ///   Tcp        → connect ip port / connect-config
    ///   Web        → connect-web url / connect-web-config
    ///   Standalone → connect-standalone(未指派 Listener 時依場景名稱查找)
    ///   共用       → disconnect / status;另有 ping / hash
    /// 並監看協議中的所有介面,介面送達後自動註冊「介面名.成員名」指令,
    /// 可直接從主控台呼叫遠端方法與讀取屬性。
    /// </summary>
    public class ClientConsole : MonoBehaviour
    {
        [Tooltip("主控台視窗。")]
        public ConsoleView View;

        [Tooltip("要監看與操作的客戶端;需為實作 IQueryerHost 的元件(Client 或 GatewayClient)。")]
        public MonoBehaviour QueryerHost;

        [Tooltip("協議;用來列舉要監看的介面。")]
        public ProtocolProvider Protocol;

        [Tooltip("選填;指派後提供 connect/connect-config 指令。需與客戶端在同一個物件上。WebGL 不支援 TCP。")]
        public Tcp.TcpConnector TcpConnector;

        [Tooltip("選填;指派後提供 connect-web/connect-web-config 指令。需與客戶端在同一個物件上。")]
        public Web.WebConnector WebConnector;

        [Tooltip("選填;指派後提供 connect-standalone 指令。需與客戶端在同一個物件上。")]
        public Standalone.Connector StandaloneConnector;

        [Tooltip("Standalone 連線目標;有指派時優先使用(僅限同場景參照)。")]
        public Standalone.Listener StandaloneListener;

        [Tooltip("StandaloneListener 未指派時,從此場景查找 Standalone.Listener。")]
        public string StandaloneSceneName = "Gateway";

        [Tooltip("查找時比對掛載 Listener 的物件名稱;留空取場景中第一個。同場景有多個 Listener 時必須指定。")]
        public string StandaloneObjectName = "";

        readonly List<GhostTypeWatcher> _Watchers = new List<GhostTypeWatcher>();
        readonly List<string> _CommandNames = new List<string>();

        void OnEnable()
        {
            if (View == null || Protocol == null || !(QueryerHost is IQueryerHost host))
            {
                UnityEngine.Debug.LogError($"[{nameof(ClientConsole)}] 需要指派 View、Protocol 與實作 {nameof(IQueryerHost)} 的 QueryerHost。", this);
                enabled = false;
                return;
            }

            PinionCore.Utility.Command command = View.Command;

            _Register("ping", _ShowPing);
            _Register("hash", _ShowHash);

            if (TcpConnector != null)
            {
                command.Register<string, int>("connect", _Connect);
                _CommandNames.Add("connect");
                _Register("connect-config", _ConnectByConfig);

                TcpConnector.ConnectResultEvent.AddListener(_OnTcpConnectResult);
                TcpConnector.ConnectBreakEvent.AddListener(_OnTcpConnectBreak);
            }

            if (WebConnector != null)
            {
                command.Register<string>("connect-web", _ConnectWeb);
                _CommandNames.Add("connect-web");
                _Register("connect-web-config", _ConnectWebByConfig);

                WebConnector.ConnectResultEvent.AddListener(_OnWebConnectResult);
                WebConnector.ConnectBreakEvent.AddListener(_OnWebConnectBreak);
            }

            if (StandaloneConnector != null)
            {
                _Register("connect-standalone", _ConnectStandalone);
            }

            if (TcpConnector != null || WebConnector != null || StandaloneConnector != null)
            {
                _Register("disconnect", _Disconnect);
                _Register("status", _ShowStatus);
            }

            PinionCore.Remote.IProtocol protocol = Protocol;
            PinionCore.Utility.Console.IViewer viewer = View;
            foreach (System.Type type in protocol.GetInterfaceProvider().Types)
            {
                _Watchers.Add(new GhostTypeWatcher(host.Queryer, type, command, viewer));
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

            if (TcpConnector != null)
            {
                TcpConnector.ConnectResultEvent.RemoveListener(_OnTcpConnectResult);
                TcpConnector.ConnectBreakEvent.RemoveListener(_OnTcpConnectBreak);
            }

            if (WebConnector != null)
            {
                WebConnector.ConnectResultEvent.RemoveListener(_OnWebConnectResult);
                WebConnector.ConnectBreakEvent.RemoveListener(_OnWebConnectBreak);
            }
        }

        void _Register(string name, System.Action executer)
        {
            View.Command.Register(name, executer);
            _CommandNames.Add(name);
        }

        void _ShowPing()
        {
            if (QueryerHost is Client client)
            {
                View.WriteLine($"ping:{client.Ping}");
                return;
            }

            if (QueryerHost is Gateways.GatewayClient gateway)
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
            TcpConnector.Connect(new IPEndPoint(address, port));
        }

        void _ConnectByConfig()
        {
            View.WriteLine("tcp connecting by config ...");
            TcpConnector.Connect();
        }

        void _ConnectWeb(string url)
        {
            View.WriteLine($"web connecting {url} ...");
            WebConnector.Connect(url);
        }

        void _ConnectWebByConfig()
        {
            View.WriteLine("web connecting by config ...");
            WebConnector.Connect();
        }

        void _ConnectStandalone()
        {
            Standalone.Listener listener = StandaloneListener != null ? StandaloneListener : _FindStandaloneListener();
            if (listener == null)
            {
                View.WriteLine($"standalone listener not found (scene:{StandaloneSceneName} object:{StandaloneObjectName})");
                return;
            }

            StandaloneConnector.Connect(listener);
            View.WriteLine(StandaloneConnector.IsConnect() ? "standalone connected." : "standalone connect failed.");
        }

        Standalone.Listener _FindStandaloneListener()
        {
            Scene scene = SceneManager.GetSceneByName(StandaloneSceneName);
            GameObject[] roots = scene.isLoaded ? scene.GetRootGameObjects() : System.Array.Empty<GameObject>();
            IEnumerable<Standalone.Listener> listeners = roots.SelectMany(root => root.GetComponentsInChildren<Standalone.Listener>(true));
            return string.IsNullOrEmpty(StandaloneObjectName)
                ? listeners.FirstOrDefault()
                : listeners.FirstOrDefault(listener => listener.gameObject.name == StandaloneObjectName);
        }

        void _Disconnect()
        {
            if (TcpConnector != null && TcpConnector.CurrentStatus != Tcp.TcpConnector.ConnectorStatus.Offline)
            {
                TcpConnector.Disconnect();
                View.WriteLine("tcp disconnected.");
            }

            if (WebConnector != null && WebConnector.IsConnected)
            {
                WebConnector.Disconnect();
                View.WriteLine("web disconnected.");
            }

            if (StandaloneConnector != null && StandaloneConnector.IsConnect())
            {
                StandaloneConnector.Disconnect();
                View.WriteLine("standalone disconnected.");
            }
        }

        void _ShowStatus()
        {
            if (TcpConnector != null)
            {
                View.WriteLine($"tcp:{TcpConnector.CurrentStatus} recv:{TcpConnector.BytesReceived} sent:{TcpConnector.BytesSent}");
            }

            if (WebConnector != null)
            {
                View.WriteLine($"web:{WebConnector.CurrentStatus} recv:{WebConnector.BytesReceived} sent:{WebConnector.BytesSent}");
            }

            if (StandaloneConnector != null)
            {
                View.WriteLine($"standalone:{StandaloneConnector.CurrentStatus} recv:{StandaloneConnector.BytesReceived} sent:{StandaloneConnector.BytesSent}");
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
