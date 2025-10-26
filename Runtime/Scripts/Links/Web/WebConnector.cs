using PinionCore.Remote.Ghost;
using PinionCore.Utility;
using System;
using UnityEngine;

namespace PinionCore.NetSync.Web
{
    [RequireComponent(typeof(Client))]
    public class WebConnector : MonoBehaviour
    {

        readonly PinionCore.Utility.StatusMachine _StatusMachine;

        public bool IsConnected { get; private set; }
        public WebConnector()
        {
            _StatusMachine = new StatusMachine();
        
        }

        public void Start()
        {
            _ToEmpty();
        }
        public void Update()
        {
            _StatusMachine.Update();
        }

        public void OnDestroy()
        {
            IsConnected = false;
            _StatusMachine.Termination();
        }

        public void Connect(string url)
        {
            _ToConnect(url);
        }

        private void _ToConnect(string url)
        {
            var stream = new WebSocketStream(url);
            IsConnected = false;
            var status = new Status.WebConnect(stream, url);
            status.SuccessEvent += () =>
            {
                _ToTransport(stream);
            };
            status.FailEvent += (err) =>
            {
                _ToEmpty();
            };
            _StatusMachine.Push(status);
        }

        private void _ToEmpty()
        {
            IsConnected = false;
            _StatusMachine.Empty();
        }

        private void _ToTransport(WebSocketStream stream)
        {
            IsConnected = true;
            var agent = GetComponent<Client>();
            var status = new Status.WebTransport(stream, agent);
            status.OfflineEvent += (err) =>
            {
                UnityEngine.Debug.Log(err);
                _ToEmpty();
            };
            _StatusMachine.Push(status);
        }

        public void Disconnect()
        {
            if (!IsConnected)
                return;
            
            _ToEmpty();
        }
    }
}
