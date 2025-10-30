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
        readonly PinionCore.Remote.NotifiableCollection<IStreamable> _NotifiableCollection;
        public event System.Action<int> DataReceivedEvent;
        public event System.Action<int> DataSentEvent;
        public Listener()
        {
            DataReceivedEvent += (size) => { };
            DataSentEvent += (size) => { };
            _NotifiableCollection = new NotifiableCollection<IStreamable>();
            Tcp = new Network.Tcp.Listener();
            Handshark = new WebHandshark(Tcp);

            Handshark.AcceptEvent += _Join;
        }

        private void _Join(WebPeer peer)
        {
            peer.TcpPeer.BreakEvent += () =>
            {
                lock (_NotifiableCollection)
                {
                    peer.TcpPeer.ReceiveEvent -= _Receive;
                    peer.TcpPeer.SendEvent -= _Send;
                    _NotifiableCollection.Items.Remove(peer);
                }

            };

            lock (_NotifiableCollection)
            {
                peer.TcpPeer.SendEvent += _Send;
                peer.TcpPeer.ReceiveEvent += _Receive;
                _NotifiableCollection.Items.Add(peer);
            }
        }

        event Action<IStreamable> IListenable.StreamableEnterEvent
        {
            add
            {
                _NotifiableCollection.Notifier.Supply += value;
            }

            remove
            {
                _NotifiableCollection.Notifier.Supply -= value;
            }
        }

        

        event Action<IStreamable> IListenable.StreamableLeaveEvent
        {
            add
            {
                _NotifiableCollection.Notifier.Unsupply += value;
            }

            remove
            {
                _NotifiableCollection.Notifier.Unsupply -= value;
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
