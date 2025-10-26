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

    [RequireComponent(typeof(Server))]
    public class WebListener : MonoBehaviour , IListenerEditor
    {
        

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

        public void Bind(int port)
        {
            if (IsListening)
            {
                return;
            }
            
            var listener = new Listener();
            var server = GetComponent<Server>();
            server.Listener.Add(listener);
            listener.Tcp.Bind(port,5);
            
            listener.DataReceivedEvent += _Receive;
            listener.DataSentEvent += _Send;

            IsListening = true;
            _Disconnect = () =>
            {
                listener.DataReceivedEvent -= _Receive;
                listener.DataSentEvent -= _Send;
                server.Listener.Remove(listener);
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
