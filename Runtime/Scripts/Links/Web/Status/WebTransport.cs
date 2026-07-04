using PinionCore.Network;
using PinionCore.Remote.Ghost;
using PinionCore.Utility;
using System;
namespace PinionCore.NetSync.Web.Status
{
    class WebTransport : IStatus
    {
        private readonly WebSocketStream stream;
        private readonly IStreamable transport;
        private readonly IConnectableAgent agent;


        public event Action<string> OfflineEvent;

        public WebTransport(WebSocketStream stream , IStreamable transport, IConnectableAgent agent)
        {

            this.stream = stream;
            this.transport = transport;
            this.agent = agent;
        }



        void IStatus.Enter()
        {


            stream.OnError += _Error;

            agent.Enable(transport);

        }

        private void _Error(string obj)
        {
            OfflineEvent(obj);
        }

        void IStatus.Leave()
        {
            
            agent.Disable();
            stream.Close();
        }

        void IStatus.Update()
        {
            
        }
    }
}
