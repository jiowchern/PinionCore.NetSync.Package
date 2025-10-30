using PinionCore.Network.Tcp;
using PinionCore.Utility;
namespace PinionCore.NetSync.Tcp.Status
{
    class TcpTransport : IStatus
    {
        private readonly Client _Agent;
        private readonly Peer _Peer;
        private readonly Connector _Connector;        

        public event System.Action OfflineEvent;
        bool _Done;
        public TcpTransport(Client agent,Peer peer,Connector connector)
        {
            _Done = false;
            _Agent = agent;
            _Peer = peer;
            _Connector = connector;
        }
        void IStatus.Enter()
        {
            _Peer.BreakEvent += _Break;
            _Agent.Enable(_Peer);
        }

        private void _Break()
        {
            UnityEngine.Debug.Log("Break");
            _Done = true;
        }

        async void IStatus.Leave()
        {
            _Peer.BreakEvent -= _Break;
            _Agent.Disable();            
            await _Connector.Disconnect();
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
