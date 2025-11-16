using PinionCore.Network.Tcp;
using PinionCore.Utility;
using System.Net;
namespace PinionCore.NetSync.Tcp.Status
{
    
    class TcpConnect : IStatus
    {
        
        private readonly EndPoint _EndPoint;
        private readonly Connector _Connector;

        public event System.Action<Connector,PinionCore.Network.Tcp.Peer> ConnectResultEvent;
        bool _Done;
        PinionCore.Network.Tcp.Peer _Peer;
        public TcpConnect(EndPoint endPoint)
        {
            _Done = false;
            _EndPoint = endPoint;
            _Connector = new Connector();
        }

        public async void Enter()
        {
            try
            {
                var result = await _Connector.ConnectAsync(_EndPoint);
                if (result.Exception == null)
                {
                    _Peer = result.Peer;
                }
                else
                {
                    UnityEngine.Debug.Log(result.Exception);
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.Log(e);
            }
            finally
            {
                _Done = true;
            }
        }

        public void Leave()
        {

        }

        public void Update()
        {
            if (_Done)
            {
                ConnectResultEvent?.Invoke(_Connector, _Peer);
            }
        }
    }
}
