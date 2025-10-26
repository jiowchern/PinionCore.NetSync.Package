using PinionCore.Network;
using PinionCore.Remote.Soul;
using System;
namespace PinionCore.NetSync
{


    public class Linstener : PinionCore.Remote.Soul.IListenable
    {
        readonly System.Collections.Generic.List<IListenable> _Items;

        readonly System.Collections.Generic.List<Action<IStreamable>> _Enters;
        readonly System.Collections.Generic.List<Action<IStreamable>> _Leaves;

        public Linstener() {
            _Items = new System.Collections.Generic.List<IListenable>();
            _Enters = new System.Collections.Generic.List<Action<IStreamable>>();
            _Leaves = new System.Collections.Generic.List<Action<IStreamable>>();
        }
        
        public void Add(IListenable item)
        {
            _Items.Add(item);
            for (int i = 0; i < _Enters.Count; i++)
            {
                item.StreamableEnterEvent += _Enters[i];
            }
            for (int i = 0; i < _Leaves.Count; i++)
            {
                item.StreamableLeaveEvent += _Leaves[i];
            }
        }

        public void Remove(IListenable item)
        {
            
            for (int i = 0; i < _Enters.Count; i++)
            {
                item.StreamableEnterEvent -= _Enters[i];
            }
            for (int i = 0; i < _Leaves.Count; i++)
            {
                item.StreamableLeaveEvent -= _Leaves[i];
            }
            _Items.Remove(item);
        }

        event Action<IStreamable> IListenable.StreamableEnterEvent
        {
            add
            {
                _Enters.Add(value);
                foreach (var item in _Items)
                {
                    item.StreamableEnterEvent += value;
                }
            }

            remove
            {
                foreach (var item in _Items)
                {
                    item.StreamableEnterEvent -= value;
                }
                _Enters.Remove(value);
            }
        }

        event Action<IStreamable> IListenable.StreamableLeaveEvent
        {
            add
            {
                _Leaves.Add(value);
                foreach (var item in _Items)
                {
                    item.StreamableLeaveEvent += value;
                }
            }

            remove
            {
                foreach (var item in _Items)
                {
                    item.StreamableLeaveEvent -= value;
                }
                _Leaves.Remove(value);
            }
        }
    }
}
