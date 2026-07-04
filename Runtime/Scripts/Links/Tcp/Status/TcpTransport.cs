using PinionCore.Network;
using PinionCore.Network.Tcp;
using PinionCore.Utility;
namespace PinionCore.NetSync.Tcp.Status
{
    class TcpTransport : IStatus
    {
        private readonly IConnectableAgent _Agent;
        private readonly Peer _Peer;
        private readonly IStreamable _Transport;
        private readonly Connector _Connector;

        public event System.Action OfflineEvent;
        bool _Done;
        public TcpTransport(IConnectableAgent agent,Peer peer,IStreamable transport,Connector connector)
        {
            _Done = false;
            _Agent = agent;
            _Peer = peer;
            _Transport = transport;
            _Connector = connector;
        }
        void IStatus.Enter()
        {
            _Peer.BreakEvent += _Break;
            _Agent.Enable(_Transport);
        }

        private void _Break()
        {
            UnityEngine.Debug.Log("Break");
            _Done = true;
        }

        void IStatus.Leave()
        {
            _Peer.BreakEvent -= _Break;
            _Agent.Disable();
            System.IDisposable peerDispose = _Peer;
            peerDispose?.Dispose();
        }
        void IStatus.Update()
        {
            if (_Done)
            {
                OfflineEvent();
            }
        }
    }
}
