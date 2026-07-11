using PinionCore.Remote.Server.Tcp;
using System;
using Unity.Properties;
using UnityEngine;

namespace PinionCore.NetSync.Tcp
{
    
    public class TcpListener : MonoBehaviour , IListenerEditor, IBindable
    {
        private Listener _Listener;

        [Tooltip("連線設定資產;呼叫無參數的 Bind() 時會使用其中的 Port。")]
        public TcpConnectionConfig Config;

        long _BytesReceived;
        long _BytesSent;

        [CreateProperty] public long BytesReceived => System.Threading.Interlocked.Read(ref _BytesReceived);


        [CreateProperty] public long BytesSent => System.Threading.Interlocked.Read(ref _BytesSent);


        [CreateProperty] public bool CurrentStatus { get; private set; }

        bool _IsActive;
        bool IListenerEditor.IsActive => _IsActive;

        public TcpListener()
        {
        }

        /// <summary>
        /// 使用指派的 <see cref="Config"/> 資產的連接埠開始監聽。
        /// </summary>
        public void Bind()
        {
            if (Config == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(TcpListener)}] Config 未指派,無法監聽。", this);
                return;
            }
            Bind(Config.Port);
        }

        public void Bind(int port)
        {
            if (_IsActive)
            {
                return;
            }
            var host = GetComponent<IListenableHost>();
            if (host == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(TcpListener)}] 找不到 {nameof(IListenableHost)} 元件(例如 Server / GatewayRouterEndpoint),無法監聽。", this);
                return;
            }
            _Listener = new Listener();
            _IsActive = true;
            System.Threading.Interlocked.Exchange(ref _BytesReceived, 0);
            System.Threading.Interlocked.Exchange(ref _BytesSent, 0);
            _Listener.DataReceivedEvent += _Receive;
            _Listener.DataSentEvent += _Send;
            host.Listener.Add(_Listener);
            _Listener.Bind(port);
        }       

        public void Close()
        {
            if (!_IsActive)
            {
                return;
            }
            
            var host = GetComponent<IListenableHost>();
            if (host != null)
            {
                host.Listener.Remove(_Listener);
            }
            _Listener.DataReceivedEvent -= _Receive;
            _Listener.DataSentEvent -= _Send;
            _IsActive = false;
            _Listener.Close();

        }
        void _Receive(int receive)
        {
            System.Threading.Interlocked.Add(ref _BytesReceived, receive);
        }

        void _Send(int send)
        {
            System.Threading.Interlocked.Add(ref _BytesSent, send);
        }
        
    }
}
