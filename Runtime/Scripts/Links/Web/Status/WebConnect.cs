using PinionCore.Utility;
namespace PinionCore.NetSync.Web.Status
{
    class WebConnect : IStatus
    {
        private readonly WebSocketStream stream;
        private readonly string url;

        public event System.Action SuccessEvent;
        public event System.Action<string> FailEvent;

        public WebConnect(WebSocketStream stream , string url)
        {
            this.stream = stream;
            this.url = url;
        }

        void IStatus.Enter()
        {
            stream.OnOpen += _ConnectSuccess;
            stream.OnError += OnStreamError;
            stream.Connect();
        }

        private void _ConnectSuccess()
        {
            SuccessEvent();
        }

        private void OnStreamError(string obj)
        {
            FailEvent(obj);
        }

        void IStatus.Leave()
        {
            stream.OnOpen -= _ConnectSuccess;
            stream.OnError -= OnStreamError;
        }

        void IStatus.Update()
        {
            
        }
    }
}
