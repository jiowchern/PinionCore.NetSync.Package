using System;
using System.Collections.Generic;
using System.Reflection;

namespace PinionCore.NetSync.Consoles
{
    /// <summary>
    /// 監看單一協議介面型別的 Supply/Unsupply。
    /// 介面實例送達時,將其方法與屬性註冊為主控台指令(見 <see cref="GhostCommandBinder"/>);
    /// 離開時取消註冊。
    /// </summary>
    public class GhostTypeWatcher : IDisposable
    {
        readonly Type _Type;
        readonly PinionCore.Utility.Command _Command;
        readonly PinionCore.Utility.Console.IViewer _Viewer;
        readonly Dictionary<object, GhostCommandBinder> _Binders;
        readonly PinionCore.Remote.INotifier<object> _Notifier;
        int _Serial;

        public GhostTypeWatcher(PinionCore.Remote.INotifierQueryable queryer, Type type, PinionCore.Utility.Command command, PinionCore.Utility.Console.IViewer viewer)
        {
            _Type = type;
            _Command = command;
            _Viewer = viewer;
            _Binders = new Dictionary<object, GhostCommandBinder>();

            MethodInfo queryMethod = typeof(PinionCore.Remote.INotifierQueryable).GetMethod(nameof(PinionCore.Remote.INotifierQueryable.QueryNotifier)).MakeGenericMethod(type);
            var notifier = queryMethod.Invoke(queryer, null);

            // INotifier<out T> 為 covariant,協議介面皆為參考型別,可以 INotifier<object> 接出。
            if (notifier is PinionCore.Remote.INotifier<object> objectNotifier)
            {
                _Notifier = objectNotifier;
                _Notifier.Supply += _Supply;
                _Notifier.Unsupply += _Unsupply;
            }
        }

        void _Supply(object ghost)
        {
            _Serial++;
            var prefix = _Serial == 1 ? _Type.Name : $"{_Type.Name}{_Serial}";
            _Viewer.WriteLine($"[supply] {_Type.Name} -> '{prefix}.*'");
            _Binders.Add(ghost, new GhostCommandBinder(prefix, ghost, _Type, _Command, _Viewer));
        }

        void _Unsupply(object ghost)
        {
            if (!_Binders.TryGetValue(ghost, out GhostCommandBinder binder))
            {
                return;
            }

            _Viewer.WriteLine($"[unsupply] {_Type.Name} ({binder.Prefix})");
            binder.Dispose();
            _Binders.Remove(ghost);
        }

        public void Dispose()
        {
            if (_Notifier != null)
            {
                _Notifier.Supply -= _Supply;
                _Notifier.Unsupply -= _Unsupply;
            }

            foreach (GhostCommandBinder binder in _Binders.Values)
            {
                binder.Dispose();
            }

            _Binders.Clear();
        }
    }
}
