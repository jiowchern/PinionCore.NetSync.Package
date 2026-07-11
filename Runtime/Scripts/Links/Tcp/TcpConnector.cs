
using PinionCore.Network.Tcp;
using PinionCore.Remote;

using System.Net;

using Unity.Properties;
using UnityEngine;

namespace PinionCore.NetSync.Tcp
{
    public class TcpConnector  :MonoBehaviour
    {
        public enum ConnectResult
        {
            ConnectSuccess,
            ConnectFaild,           
        }   
        public enum ConnectorStatus
        {
            Offline,
            Connect,
            Online,            
        }

        readonly PinionCore.Utility.StatusMachine _StatusMachine;

        [Tooltip("連線設定資產;呼叫無參數的 Connect() 時會使用此設定。")]
        public TcpConnectionConfig Config;

        public UnityEngine.Events.UnityEvent<ConnectResult> ConnectResultEvent = new UnityEngine.Events.UnityEvent<ConnectResult>();
        public UnityEngine.Events.UnityEvent ConnectBreakEvent = new UnityEngine.Events.UnityEvent();
        [CreateProperty] public ConnectorStatus CurrentStatus { get; private set; }
        [CreateProperty] public long BytesReceived { get; private set; }
        [CreateProperty] public long BytesSent { get; private set; }
        [CreateProperty] public string SendDisplay => $"{BytesSent} ({_SendRate.BytesPerSecond:0.#})";
        [CreateProperty] public string ReceiveDisplay => $"{BytesReceived} ({_ReceiveRate.BytesPerSecond:0.#})";

        readonly TransferRateMeter _SendRate = new TransferRateMeter();
        readonly TransferRateMeter _ReceiveRate = new TransferRateMeter();

        public TcpConnector()
        {
            _StatusMachine = new Utility.StatusMachine();
        }

        public void Update()
        {
            _StatusMachine.Update();
            _SendRate.Update(BytesSent, UnityEngine.Time.realtimeSinceStartup);
            _ReceiveRate.Update(BytesReceived, UnityEngine.Time.realtimeSinceStartup);
        }
        public void OnDestroy()
        {
            _StatusMachine.Termination();
        }
      
        /// <summary>
        /// 使用指派的 <see cref="Config"/> 資產進行連線。
        /// </summary>
        public void Connect()
        {
            if (Config == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(TcpConnector)}] Config 未指派,無法連線。", this);
                return;
            }
            var endPoint = Config.ToEndPoint();
            if (endPoint == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(TcpConnector)}] Config 的位址 '{Config.Host}' 不是合法的 IPv4 位址。", this);
                return;
            }
            _ToConnect(endPoint);
        }

        public void Connect(EndPoint endPoint)
        {
            _ToConnect(endPoint);
        }
        public void Disconnect()
        {
            _ToEmpry();
        }
        private void _ToConnect(EndPoint endPoint)
        {
            CurrentStatus = ConnectorStatus.Connect;
            var status = new Status.TcpConnect( endPoint);
            status.ConnectResultEvent += _OnConnectResult;
            _StatusMachine.Push(status);
        }

        private void _OnConnectResult(Connector connector, Peer peer)
        {
            if (peer != null)
            {

                _ToOnline(connector, peer);
                ConnectResultEvent.Invoke( ConnectResult.ConnectSuccess);
            }
            else
            {
                ConnectResultEvent.Invoke(ConnectResult.ConnectFaild);
                _ToEmpry();
            }

        }
        
        private void _ToOnline(Connector connector, Peer peer)
        {
            BytesReceived = 0;
            BytesSent = 0;
            _SendRate.Reset(UnityEngine.Time.realtimeSinceStartup);
            _ReceiveRate.Reset(UnityEngine.Time.realtimeSinceStartup);
            CurrentStatus = ConnectorStatus.Online;

            var agent = GetComponent<IConnectableAgent>();
            if (agent == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(TcpConnector)}] 找不到 {nameof(IConnectableAgent)} 元件(例如 Client / GatewayClient / GatewayRegistry),無法連線。", this);
                _ToEmpry();
                return;
            }

            var metered = new MeteredStreamable(peer);
            metered.SendEvent += _Send;
            metered.ReceiveEvent += _Receive;

            var status = new Status.TcpTransport(agent, peer, metered, connector);
            status.OfflineEvent += () =>
            {
                ConnectBreakEvent.Invoke();
                _ToEmpry();
                
            };
            _StatusMachine.Push(status);
        }

        private void _ToEmpry()
        {
            _StatusMachine.Empty();
            CurrentStatus = ConnectorStatus.Offline;
        }

        private void _Send(int bytes)
        {
            BytesSent += bytes;
        }

        private void _Receive(int bytes)
        {
            BytesReceived += bytes;
        }
    }
}
