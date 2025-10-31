using PinionCore.Network;
using PinionCore.Remote.Soul;
using PinionCore.Remote;
using System;

namespace PinionCore.NetSync.Web
{
    public class Listener :  Remote.Soul.IListenable , IDisposable
    {
        public readonly PinionCore.Network.Tcp.Listener Tcp;
        public readonly WebHandshark Handshark;
        readonly PinionCore.Remote.Depot<IStreamable> _Depot;
        public event System.Action<int> DataReceivedEvent;
        public event System.Action<int> DataSentEvent;
        public Listener()
        {
            DataReceivedEvent += (size) => { };
            DataSentEvent += (size) => { };
            _Depot = new Depot<IStreamable>();
            Tcp = new Network.Tcp.Listener();
            Handshark = new WebHandshark(Tcp);

            Handshark.AcceptEvent += _Join;
        }

        private void _Join(WebPeer peer)
        {
            peer.TcpPeer.BreakEvent += () =>
            {
                lock (_Depot)
                {
                    peer.TcpPeer.ReceiveEvent -= _Receive;
                    peer.TcpPeer.SendEvent -= _Send;
                    _Depot.Items.Remove(peer);
                }

            };

            lock (_Depot)
            {
                peer.TcpPeer.SendEvent += _Send;
                peer.TcpPeer.ReceiveEvent += _Receive;
                _Depot.Items.Add(peer);
            }
        }

        event Action<IStreamable> IListenable.StreamableEnterEvent
        {
            add
            {
                _Depot.Notifier.Supply += value;
            }

            remove
            {
                _Depot.Notifier.Supply -= value;
            }
        }

        

        event Action<IStreamable> IListenable.StreamableLeaveEvent
        {
            add
            {
                _Depot.Notifier.Unsupply += value;
            }

            remove
            {
                _Depot.Notifier.Unsupply -= value;
            }
        }

        private void _Send(int bytes)
        {
            DataSentEvent(bytes);
        }

        private void _Receive(int bytes)
        {
            DataReceivedEvent(bytes);
        }

        void IDisposable.Dispose()
        {
            Handshark.AcceptEvent -= _Join;
            (Handshark as IDisposable).Dispose();
        }
    }
}
