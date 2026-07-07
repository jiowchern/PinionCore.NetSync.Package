using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PinionCore.NetSync.Consoles
{
    /// <summary>
    /// 將一個 ghost 實例的方法與屬性註冊為主控台指令。
    /// 方法指令格式:「前綴.方法名 參數...」;回傳 Value&lt;T&gt; 的方法會在結果回來時輸出。
    /// 屬性指令格式:「前綴.屬性名」,輸出 Property&lt;T&gt; 目前的值。
    /// Dispose 時取消所有註冊。
    /// </summary>
    public class GhostCommandBinder : IDisposable
    {
        readonly PinionCore.Utility.Command _Command;
        readonly PinionCore.Utility.Console.IViewer _Viewer;
        readonly List<Guid> _Ids;

        public readonly string Prefix;

        public GhostCommandBinder(string prefix, object ghost, Type type, PinionCore.Utility.Command command, PinionCore.Utility.Console.IViewer viewer)
        {
            Prefix = prefix;
            _Command = command;
            _Viewer = viewer;
            _Ids = new List<Guid>();

            foreach (Type interfaceType in _AllInterfaces(type))
            {
                _RegisterMethods(ghost, interfaceType);
                _RegisterProperties(ghost, interfaceType);
            }
        }

        static IEnumerable<Type> _AllInterfaces(Type type)
        {
            yield return type;
            foreach (Type inherited in type.GetInterfaces())
            {
                yield return inherited;
            }
        }

        void _RegisterMethods(object ghost, Type type)
        {
            foreach (MethodInfo method in type.GetMethods())
            {
                if (method.IsSpecialName || method.IsGenericMethodDefinition)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Any(p => p.ParameterType.IsByRef))
                {
                    continue;
                }

                Type[] paramTypes = parameters.Select(p => p.ParameterType).ToArray();
                var name = $"{Prefix}.{method.Name}";
                MethodInfo target = method;

                Func<string[], object> handler = args =>
                {
                    if (args.Length != paramTypes.Length)
                    {
                        throw new ArgumentException($"The number of command arguments is {paramTypes.Length}");
                    }

                    var values = new object[paramTypes.Length];
                    for (var i = 0; i < paramTypes.Length; ++i)
                    {
                        PinionCore.Utility.Command.TryConversion(args[i], out values[i], paramTypes[i]);
                    }

                    var ret = target.Invoke(ghost, values);
                    if (ret is PinionCore.Remote.IValue value)
                    {
                        value.QueryValue(result => _Viewer.WriteLine($"{name} return {result}"));
                        return $"waiting {name} ...";
                    }

                    return ret;
                };

                Guid id = _Command.Register(name, handler, target.ReturnType, paramTypes);
                _Ids.Add(id);
            }
        }

        void _RegisterProperties(object ghost, Type type)
        {
            foreach (PropertyInfo property in type.GetProperties())
            {
                if (!property.CanRead || property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                var name = $"{Prefix}.{property.Name}";
                PropertyInfo target = property;

                Func<string[], object> handler = args =>
                {
                    if (args.Length != 0)
                    {
                        throw new ArgumentException("The number of command arguments is 0");
                    }

                    var holder = target.GetValue(ghost);
                    if (holder == null)
                    {
                        return null;
                    }

                    // Property<T> 這類容器輸出內部的 Value,其餘直接輸出。
                    PropertyInfo inner = holder.GetType().GetProperty("Value");
                    if (inner != null)
                    {
                        return inner.GetValue(holder);
                    }

                    return holder;
                };

                Guid id = _Command.Register(name, handler, target.PropertyType, Array.Empty<Type>());
                _Ids.Add(id);
            }
        }

        public void Dispose()
        {
            foreach (Guid id in _Ids)
            {
                _Command.Unregister(id);
            }

            _Ids.Clear();
        }
    }
}
