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

        [Tooltip("模擬單向延遲(秒),雙向各套用,RTT ≈ 2×Lag。0 = 不延遲。")]
        [Min(0)] public float Lag;

        LagStreamable _ClientLag;
        LagStreamable _ServerLag;

        [CreateProperty] public ConnectorStatus CurrentStatus { get; private set; }
        [CreateProperty] public long BytesReceived { get; private set; }
        [CreateProperty] public long BytesSent { get; private set; }
        [CreateProperty] public string SendDisplay => $"{BytesSent} ({_SendRate.BytesPerSecond:0.#})";
        [CreateProperty] public string ReceiveDisplay => $"{BytesReceived} ({_ReceiveRate.BytesPerSecond:0.#})";

        readonly TransferRateMeter _SendRate = new TransferRateMeter();
        readonly TransferRateMeter _ReceiveRate = new TransferRateMeter();

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
            var serverLag = new LagStreamable(reverseStream, _GetLag);
            listener.Add(serverLag);

            BytesSent = 0;
            BytesReceived = 0;
            _SendRate.Reset(UnityEngine.Time.realtimeSinceStartup);
            _ReceiveRate.Reset(UnityEngine.Time.realtimeSinceStartup);
            var clientLag = new LagStreamable(steam, _GetLag);
            var metered = new MeteredStreamable(clientLag);
            metered.SendEvent += _Send;
            metered.ReceiveEvent += _Receive;

            agent.Enable(metered);
            _ClientLag = clientLag;
            _ServerLag = serverLag;
            _Connecting = true;
            CurrentStatus = ConnectorStatus.Online;

            _Disconnect = () =>
            {
                agent.Disable();
                listener.Remove(serverLag);
                metered.SendEvent -= _Send;
                metered.ReceiveEvent -= _Receive;
                _ClientLag = null;
                _ServerLag = null;
                _Connecting = false;
                CurrentStatus = ConnectorStatus.Offline;
            };

        }

        public void Update()
        {
            _ClientLag?.Update();
            _ServerLag?.Update();
            _SendRate.Update(BytesSent, UnityEngine.Time.realtimeSinceStartup);
            _ReceiveRate.Update(BytesReceived, UnityEngine.Time.realtimeSinceStartup);
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

        private float _GetLag()
        {
            return Lag;
        }


    }

}
