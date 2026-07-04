using PinionCore.NetSync.Extensions;
using PinionCore.NetSync.Standalone;
using UnityEditor;
using UnityEngine.UIElements;

namespace PinionCore.NetSync.Editor.Standalone
{

    [CustomEditor(typeof(Connector))]
    public class ConnectorEditor : UnityEditor.Editor
    {
        private Connector _Target;

        private void OnEnable()
        {
            _Target = (Connector)target;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var element = new VisualElement();
            var root = EditorGUIUtility.Load("Packages/com.pinioncore.netsync/Editor/Resources/Layouts/Standalone/Connector.uxml") as VisualTreeAsset;
            if (root == null)
                return base.CreateInspectorGUI();
            root.CloneTree(element);

            var status = element.Q<Label>("Status");
            status.SetTextBinding(_Target, nameof(_Target.CurrentStatus), BindingMode.ToTarget);

            var send = element.Q<Label>("Send");
            send.SetTextBinding(_Target, nameof(_Target.BytesSent), BindingMode.ToTarget);

            var receive = element.Q<Label>("Receive");
            receive.SetTextBinding(_Target, nameof(_Target.BytesReceived), BindingMode.ToTarget);

            return element;
        }
    }

}
