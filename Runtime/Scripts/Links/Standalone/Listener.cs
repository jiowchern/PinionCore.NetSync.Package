using PinionCore.Network;
using PinionCore.Remote;
using PinionCore.Remote.Soul;
using System;
using UnityEngine;
namespace PinionCore.NetSync.Standalone
{
    [RequireComponent(typeof(Server))]

    public class Listener : MonoBehaviour , IListenable , IListenerEditor
    {
        class Peer : IStreamable
        {
            private readonly IStreamable _Stream;
            public event Action<int> ReceivedEvent;
            public event Action<int> SendEvent;
            public Peer(IStreamable stream)
            {
                _Stream = stream;
            }
            IWaitableValue<int> IStreamable.Receive(byte[] buffer, int offset, int count)
            {
                var result = _Stream.Receive(buffer, offset, count);

                var awaiter = result.GetAwaiter();
                awaiter.OnCompleted(() => _Receive(awaiter.GetResult()));

                
                return result;
            }

            

            IWaitableValue<int> IStreamable.Send(byte[] buffer, int offset, int count)
            {
                var result = _Stream.Send(buffer, offset, count);
                var awaiter = result.GetAwaiter();
                awaiter.OnCompleted(() => _Send(awaiter.GetResult()));
                return result;
            }
            private void _Receive(int obj)
            {
                ReceivedEvent(obj);
            }
            private void _Send(int obj)
            {
                SendEvent(obj);
            }
        }
        private readonly NotifiableCollection<IStreamable> _Notice;
        
        bool _Listening;

        bool IListenerEditor.IsActive => _Listening;
        readonly System.Collections.Generic.Dictionary<IStreamable, Peer> _Peers ;
        public Listener()
        {
            _Notice = new PinionCore.Remote.NotifiableCollection<IStreamable>();
            _Peers= new System.Collections.Generic.Dictionary<IStreamable, Peer>();
            _DataReceivedEvent += _Empty;
            _DataSendEvent += _Empty;

        }

        private void _Empty(int obj)
        {
            
        }

        public void Add(IStreamable streamable)
        {
            var peer = new Peer(streamable);
            peer.ReceivedEvent += _Receive;
            peer.SendEvent += _Send;
            _Peers.Add(streamable, peer);
            _Notice.Items.Add(peer);
        }

        private void _Receive(int obj)
        {
            _DataReceivedEvent(obj);
        }

        private void _Send(int obj)
        {
            _DataSendEvent(obj);
        }

        public void Remove(IStreamable streamable)
        {
            if (!_Peers.ContainsKey(streamable))
            {
                PinionCore.Utility.Log.Instance.WriteInfo($"Remove {streamable.GetHashCode()} fail");
                return;
            }
            var peer = _Peers[streamable];
            peer.ReceivedEvent -= _Receive;
            peer.SendEvent -= _Send;
            _Peers.Remove(streamable);
            _Notice.Items.Remove(peer);

        }
        event Action<IStreamable> IListenable.StreamableEnterEvent
        {
            add
            {
                _Notice.Notifier.Supply += value;
            }

            remove
            {
                _Notice.Notifier.Supply -= value;
            }
        }

        event Action<IStreamable> IListenable.StreamableLeaveEvent
        {
            add
            {
                _Notice.Notifier.Unsupply += value;
            }

            remove
            {
                _Notice.Notifier.Unsupply -= value;
            }
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

        public void Bind()
        {            
            var server = GetComponent<Server>();
            server.Listener.Add(this);
            _Listening = true;
        }

        public void Close()
        {
            _Listening = false;
            var server = GetComponent<Server>();
            server.Listener.Remove(this);
            
        }

        

        public bool IsConnect()
        {
            return _Listening;
        }
    }

}
