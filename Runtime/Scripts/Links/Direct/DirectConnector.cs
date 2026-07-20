using Unity.Properties;
using UnityEngine;

namespace PinionCore.NetSync.Direct
{
    /// <summary>
    /// 零序列化直通模式的連線元件:把同物件上的 IDirectAgent(DirectClient)
    /// 直接接上目標 Server(IEntry),無 stream、無序列化、無延遲/流量統計。
    /// </summary>
    public class DirectConnector : MonoBehaviour
    {
        public enum ConnectorStatus
        {
            Offline,
            Online,
        }

        bool _Connecting;

        System.Action _Disconnect;

        [CreateProperty] public ConnectorStatus CurrentStatus { get; private set; }

        public DirectConnector()
        {
            _Disconnect = _Empty;
            CurrentStatus = ConnectorStatus.Offline;
        }

        private void _Empty()
        {

        }

        public void Connect(Server server)
        {
            if (_Connecting)
            {
                return;
            }
            var agent = GetComponent<IDirectAgent>();
            if (agent == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(DirectConnector)}] 找不到 {nameof(IDirectAgent)} 元件(例如 DirectClient),無法連線。", this);
                return;
            }
            if (server == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(DirectConnector)}] 未指定 Server,無法連線。", this);
                return;
            }

            PinionCore.Remote.IEntry entry = server;
            agent.Launch(entry);
            _Connecting = true;
            CurrentStatus = ConnectorStatus.Online;

            _Disconnect = () =>
            {
                agent.Shutdown();
                _Connecting = false;
                CurrentStatus = ConnectorStatus.Offline;
            };
        }

        public void Disconnect()
        {
            if (!_Connecting)
            {
                return;
            }
            _Disconnect();
            _Disconnect = _Empty;
        }

        public bool IsConnect()
        {
            return _Connecting;
        }
    }
}
