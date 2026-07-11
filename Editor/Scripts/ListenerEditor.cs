using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using PinionCore.NetSync.Extensions;
using PinionCore.NetSync.Tcp;
using System;
using Unity.Properties;
namespace PinionCore.NetSync.Editor
{
    public class ListenerEditor<T> : UnityEditor.Editor where T : class, IListenerEditor
    {
        private IListenerEditor _Target;

        [CreateProperty] public bool IsActive => _Target.IsActive;
        // 累計值由 runtime 元件持有,Inspector 重新選取(Editor 重建)時不會歸零。
        [CreateProperty] public string SendDisplay => _Target.SendDisplay;
        [CreateProperty] public string ReceiveDisplay => _Target.ReceiveDisplay;

        public void OnEnable()
        {
            _Target = target as T;
        }

        public void OnDisable()
        {
            _Target = null;
        }


        public override VisualElement CreateInspectorGUI()
        {
            var element = new VisualElement();
            var root = EditorGUIUtility.Load("Packages/com.pinioncore.netsync/Editor/Resources/Layouts/Listener.uxml") as VisualTreeAsset;
            if (root == null)
                return base.CreateInspectorGUI();
            root.CloneTree(element);

            var status = element.Q<Label>("Status");
            status.SetTextBinding(this, nameof(this.IsActive), BindingMode.ToTarget);

            var send = element.Q<Label>("Send");
            send.SetTextBinding(this, nameof(this.SendDisplay), BindingMode.ToTarget);

            var receive = element.Q<Label>("Receive");
            receive.SetTextBinding(this, nameof(this.ReceiveDisplay), BindingMode.ToTarget);

            // Config 欄位:僅在目標具有序列化的 Config 欄位時顯示 (TCP/Web)。
            // Standalone 監聽器沒有 Config,故移除該 PropertyField。
            var configField = element.Q<PropertyField>("ConfigField");
            if (configField != null)
            {
                if (serializedObject.FindProperty("Config") == null)
                {
                    configField.RemoveFromHierarchy();
                }
                else
                {
                    element.Bind(serializedObject);
                }
            }

            return element;
        }
    }
}
