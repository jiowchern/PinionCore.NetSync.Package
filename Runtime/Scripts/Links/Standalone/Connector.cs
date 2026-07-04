using PinionCore.Network;
using PinionCore.Remote;
using System;
using Unity.Properties;
using UnityEngine;
namespace PinionCore.NetSync.Standalone
{
    public class Connector : MonoBehaviour
    {
        public enum ConnectorStatus
        {
            Offline,
            Online,
        }

        bool _Connecting;

        System.Action _Disconnect;

        [CreateProperty] public ConnectorStatus CurrentStatus { get; private set; }
        [CreateProperty] public long BytesReceived { get; private set; }
        [CreateProperty] public long BytesSent { get; private set; }

        public Connector()
        {
            _Disconnect = _Empty;
            CurrentStatus = ConnectorStatus.Offline;
        }

        private void _Empty()
        {

        }

        public void Connect(Listener listener)
        {
            if(_Connecting)
            {
                return;

            }
            var agent = GetComponent<IConnectableAgent>();
            if (agent == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(Connector)}] 找不到 {nameof(IConnectableAgent)} 元件(例如 Client / GatewayClient / GatewayRegistry),無法連線。", this);
                return;
            }
            var steam = new PinionCore.Network.Stream();
            var reverseStream = new ReverseStream(steam);
            listener.Add(reverseStream);

            BytesSent = 0;
            BytesReceived = 0;
            var metered = new MeteredStreamable(steam);
            metered.SendEvent += _Send;
            metered.ReceiveEvent += _Receive;

            agent.Enable(metered);
            _Connecting = true;
            CurrentStatus = ConnectorStatus.Online;

            _Disconnect = () =>
            {
                agent.Disable();
                listener.Remove(reverseStream);
                metered.SendEvent -= _Send;
                metered.ReceiveEvent -= _Receive;
                _Connecting = false;
                CurrentStatus = ConnectorStatus.Offline;
            };

        }

        public void Disconnect()
        {
            if(!_Connecting)
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
