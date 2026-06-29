using PinionCore.Remote.Server.Tcp;
using System;
using Unity.Properties;
using UnityEngine;

namespace PinionCore.NetSync.Tcp
{
    
    [RequireComponent(typeof(Server))]
    public class TcpListener : MonoBehaviour , IListenerEditor
    {
        private Listener _Listener;

        [Tooltip("連線設定資產;呼叫無參數的 Bind() 時會使用其中的 Port。")]
        public TcpConnectionConfig Config;

        [CreateProperty] public long BytesReceived { get; private set; }

        
        [CreateProperty] public long BytesSent { get; private set; }

      
        [CreateProperty] public bool CurrentStatus { get; private set; }

        bool _IsActive;
        bool IListenerEditor.IsActive => _IsActive;

        public TcpListener()
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
                _DataSendEvent +=value;
            }

            remove
            {
                _DataSendEvent -= value;
            }
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
            _Listener = new Listener();
            _IsActive = true;
            BytesReceived = 0;
            BytesSent = 0;            
            _Listener.DataReceivedEvent += _Receive;
            _Listener.DataSentEvent += _Send;
            var server = GetComponent<Server>();
            server.Listener.Add(_Listener);
            _Listener.Bind(port);
        }       

        public void Close()
        {
            if (!_IsActive)
            {
                return;
            }
            
            var server = GetComponent<Server>();
            server.Listener.Remove(_Listener);
            _Listener.DataReceivedEvent -= _Receive;
            _Listener.DataSentEvent -= _Send;
            _IsActive = false;
            _Listener.Close();

        }
        void _Receive(int receive)
        {
            _DataReceivedEvent?.Invoke(receive);
        }

        void _Send(int send)
        {
            _DataSendEvent?.Invoke(send);
        }
        
    }
}
