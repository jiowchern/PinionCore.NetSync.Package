using PinionCore.NetSync.Extensions;
using PinionCore.NetSync.Web;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace PinionCore.NetSync.Editor.Web
{

    [CustomEditor(typeof(WebConnector))]
    public class ConnectorEditor : UnityEditor.Editor
    {
        private WebConnector _Target;

        private void OnEnable()
        {
            _Target = (WebConnector)target;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var element = new VisualElement();
            var root = EditorGUIUtility.Load("Packages/com.pinioncore.netsync/Editor/Resources/Layouts/Web/Connector.uxml") as VisualTreeAsset;
            if (root == null)
                return base.CreateInspectorGUI();
            root.CloneTree(element);

            var status = element.Q<Label>("Status");
            status.SetTextBinding(_Target, nameof(_Target.CurrentStatus), BindingMode.ToTarget);

            var send = element.Q<Label>("Send");
            send.SetTextBinding(_Target, nameof(_Target.BytesSent), BindingMode.ToTarget);

            var receive = element.Q<Label>("Receive");
            receive.SetTextBinding(_Target, nameof(_Target.BytesReceived), BindingMode.ToTarget);

            // 綁定序列化欄位 (Config) 至 uxml 中的 PropertyField。
            element.Bind(serializedObject);

            return element;
        }
    }

}
