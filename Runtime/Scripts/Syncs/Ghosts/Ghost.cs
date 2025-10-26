using PinionCore.NetSync.Syncs.Protocols;
using PinionCore.Remote;
using System;
using Unity.Properties;
using UnityEngine;

namespace PinionCore.NetSync.Syncs.Ghosts
{
    public class Ghost : MonoBehaviour
    {
        class Notifier<T> : IDisposable, INotifier<T> where T : class, IObject
        {
            private readonly int Id_;
            private readonly INotifierQueryable Queryable_;
            readonly PinionCore.Remote.Depot<T> _Depot ;

            public Notifier(int id,INotifierQueryable queryable)
            {
                
                _Depot = new PinionCore.Remote.Depot<T>();
                Id_ = id;
                Queryable_ = queryable;
                Queryable_.QueryNotifier<T>().Supply += _OnSupply;
                Queryable_.QueryNotifier<T>().Unsupply += _OnUnsupply;                
            }

            private void _OnUnsupply(T t)
            {
                if (t.Id != Id_)
                {
                    return;
                }
                _Depot.Items.Remove(t);
            }

            private void _OnSupply(T t)
            {
                if (t.Id != Id_)
                {
                    return;
                }
                _Depot.Items.Add(t);
            }

            
            event Action<T> INotifier<T>.Supply
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

            
            event Action<T> INotifier<T>.Unsupply
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

            void IDisposable.Dispose()
            {
                Queryable_.QueryNotifier<T>().Supply -= _OnSupply;
                Queryable_.QueryNotifier<T>().Unsupply -= _OnUnsupply;
            }
        }

        readonly System.Collections.Generic.Dictionary<Type, IDisposable> _Notifiers;
        INotifierQueryable _Notifier;

        [CreateProperty] public int Id { get; private set; }

        public Ghost()
        {
            _Notifiers = new System.Collections.Generic.Dictionary<Type, IDisposable>();
        }
        public void Initial(IObject obj , INotifierQueryable notifier)
        {
            
            _Notifier = notifier;
            Id = obj.Id;
        }

        public void Finial(IObject obj, INotifierQueryable notifier)
        {
            if (Id != obj.Id)
            {
                return;
            }
            Id = 0;
        }

        public INotifier<T> Query<T>() where T : class, IObject
        {
            if (_Notifiers.ContainsKey(typeof(T)))
            {
                return _Notifiers[typeof(T)] as INotifier<T>;
            }
          
            var notifier = new Notifier<T>(Id, _Notifier);
            _Notifiers.Add(typeof(T), notifier);
            return notifier;
        }
        public void OnDestroy()
        {
            foreach (var notifier in _Notifiers)
            {
                notifier.Value.Dispose();
            }
            _Notifiers.Clear();
        }
    }
}
