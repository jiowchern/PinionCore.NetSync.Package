using PinionCore.Network;
using PinionCore.Remote;
using System;
using UnityEngine;
namespace PinionCore.NetSync.Standalone
{
    public class Connector : MonoBehaviour
    {
        


        bool _Connecting;

        System.Action _Disconnect;
        public Connector()
        {
            _Disconnect = _Empty;


        }

        private void _Empty()
        {
            
        }

        public void Connect(Listener listener)
        {
            if(_Connecting)
            {
                return;
            
            }
            var agent = GetComponent<IConnectableAgent>();
            if (agent == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(Connector)}] 找不到 {nameof(IConnectableAgent)} 元件(例如 Client / GatewayClient / GatewayRegistry),無法連線。", this);
                return;
            }
            var steam = new PinionCore.Network.Stream();
            var reverseStream = new ReverseStream(steam);
            listener.Add(reverseStream);
            agent.Enable(steam);
            _Connecting = true;

            _Disconnect = () =>
            {
                agent.Disable();
                listener.Remove(reverseStream);
                _Connecting = false;
            };

        }

        public void Disconnect()
        {
            if(!_Connecting) 
            {
                return;
            }
            _Disconnect();
            _Disconnect = _Empty;
        }

        public bool IsConnect()
        {
            return _Connecting;
        }

       
    }

}
