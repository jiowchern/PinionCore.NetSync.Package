using PinionCore.Remote.Ghost;
using PinionCore.Utility;
using System;
using UnityEngine;

namespace PinionCore.NetSync.Web
{
    public class WebConnector : MonoBehaviour
    {

        readonly PinionCore.Utility.StatusMachine _StatusMachine;

        [Tooltip("連線設定資產;呼叫無參數的 Connect() 時會使用其中的 Url。")]
        public WebConnectionConfig Config;

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

        /// <summary>
        /// 使用指派的 <see cref="Config"/> 資產的 Url 進行連線。
        /// </summary>
        public void Connect()
        {
            if (Config == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(WebConnector)}] Config 未指派,無法連線。", this);
                return;
            }
            if (string.IsNullOrEmpty(Config.Url))
            {
                UnityEngine.Debug.LogError($"[{nameof(WebConnector)}] Config 的 Url 為空,無法連線。", this);
                return;
            }
            _ToConnect(Config.Url);
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
            var agent = GetComponent<IConnectableAgent>();
            if (agent == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(WebConnector)}] 找不到 {nameof(IConnectableAgent)} 元件(例如 Client / GatewayClient / GatewayRegistry),無法連線。", this);
                _ToEmpty();
                return;
            }
            IsConnected = true;
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
