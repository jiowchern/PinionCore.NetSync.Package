using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace PinionCore.NetSync
{
    // 擴展命名空間
    namespace Extensions
    {
        public static class RuntimeHelper
        {
            public static string ToHexString(this byte[] bytes)
            {
                return string.Join("", bytes.Select(s => string.Format("{0:X2}", s)));
            }

            public static void SetTextBinding<T>(this UnityEngine.UIElements.Label label, T instance, string property, BindingMode bindingMode)
            {
                label.SetBinding(nameof(label.text), new DataBinding
                {
                    dataSource = instance,
                    dataSourcePath = new Unity.Properties.PropertyPath(property),
                    bindingMode = bindingMode,
                });
            }
        }
    }
}
