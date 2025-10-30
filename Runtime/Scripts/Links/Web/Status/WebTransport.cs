using PinionCore.Remote.Ghost;
using PinionCore.Utility;
using System;
using System.Threading.Tasks;
namespace PinionCore.NetSync.Web.Status
{
    class WebTransport : IStatus
    {
        private readonly WebSocketStream stream;
        private readonly Client agent;
        

        public event Action<string> OfflineEvent;

        public WebTransport(WebSocketStream stream , Client agent)
        {
        
            this.stream = stream;
            this.agent = agent;
        }

        

        void IStatus.Enter()
        {            
            
            
            stream.OnError += _Error;
            
            agent.Enable(stream);
            
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
