
using PinionCore.Network.Tcp;
using PinionCore.Remote;

using System.Net;

using Unity.Properties;
using UnityEngine;

namespace PinionCore.NetSync.Tcp
{
    [RequireComponent(typeof(Client))]
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
        public UnityEngine.Events.UnityEvent<ConnectResult> ConnectResultEvent;
        public UnityEngine.Events.UnityEvent ConnectBreakEvent;
        [CreateProperty] public ConnectorStatus CurrentStatus { get; private set; }
        [CreateProperty] public long BytesReceived { get; private set; }
        [CreateProperty] public long BytesSent { get; private set; }

        public TcpConnector()
        {
            _StatusMachine = new Utility.StatusMachine();
        }

        public void Update()
        {
            _StatusMachine.Update();

        }
        public void OnDestroy()
        {
            _StatusMachine.Termination();
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
            CurrentStatus = ConnectorStatus.Online;
            peer.ReceiveEvent += _Receive;
            peer.SendEvent += _Send;

            var agent = GetComponent<Client>();
            var status = new Status.TcpTransport(agent, peer, connector);
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
