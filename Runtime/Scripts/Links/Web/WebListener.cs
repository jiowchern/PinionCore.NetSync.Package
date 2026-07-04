using PinionCore.Network;
using PinionCore.Remote.Soul;
using System.Net.WebSockets;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using System;

namespace PinionCore.NetSync.Web
{

    public class WebListener : MonoBehaviour , IListenerEditor , IBindable
    {
        

        [Tooltip("連線設定資產;呼叫無參數的 Bind() 時會使用其 Url 解析出的連接埠。")]
        public WebConnectionConfig Config;

        public bool IsListening { get; private set; }

        bool IListenerEditor.IsActive => IsListening;

        System.Action _Disconnect;  

        public WebListener()
        {
            _Disconnect = _Empty;
        }

        private void _Empty()
        {
            
        }

        event Action<int> _DataReceivedEvent;
        event Action<int> IListenerEditor.DataReceivedEvent
        {
            add
            {
                _DataReceivedEvent += value;
            }

            remove
            {
                _DataReceivedEvent -= value;
            }
        }

        event Action<int> _DataSendEvent;
        event Action<int> IListenerEditor.DataSendEvent
        {
            add
            {
                _DataSendEvent += value;
            }

            remove
            {
                _DataSendEvent -= value;
            }
        }

        /// <summary>
        /// 使用指派的 <see cref="Config"/> 資產 (從 Url 解析出的連接埠) 開始監聽。
        /// </summary>
        public void Bind()
        {
            if (Config == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(WebListener)}] Config 未指派,無法監聽。", this);
                return;
            }
            var port = Config.Port;
            if (port < 0)
            {
                UnityEngine.Debug.LogError($"[{nameof(WebListener)}] 無法從 Url '{Config.Url}' 解析出連接埠。", this);
                return;
            }
            Bind(port);
        }

        public void Bind(int port)
        {
            if (IsListening)
            {
                return;
            }
            
            var host = GetComponent<IListenableHost>();
            if (host == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(WebListener)}] 找不到 {nameof(IListenableHost)} 元件(例如 Server / GatewayRouterEndpoint),無法監聽。", this);
                return;
            }
            var listener = new Listener();
            host.Listener.Add(listener);
            listener.Tcp.Bind(port,5);

            listener.DataReceivedEvent += _Receive;
            listener.DataSentEvent += _Send;

            IsListening = true;
            _Disconnect = () =>
            {
                listener.DataReceivedEvent -= _Receive;
                listener.DataSentEvent -= _Send;
                host.Listener.Remove(listener);
                listener.Tcp.Close();
                IsListening = false;
            };
        }

        

        public void Close()
        {
            if (!IsListening)
                return;

            _Disconnect();
            _Disconnect = _Empty;
        }

        private void _Send(int bytes)
        {
            _DataSendEvent?.Invoke(bytes);
        }

        private void _Receive(int bytes)
        {
            _DataReceivedEvent?.Invoke(bytes);
        }
    }
}
