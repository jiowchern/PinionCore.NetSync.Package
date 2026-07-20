using PinionCore.NetSync.Direct;
using PinionCore.NetSync.Extensions;
using UnityEditor;
using UnityEngine.UIElements;

namespace PinionCore.NetSync.Editor.Direct
{

    [CustomEditor(typeof(DirectConnector))]
    public class DirectConnectorEditor : UnityEditor.Editor
    {
        private DirectConnector _Target;

        private void OnEnable()
        {
            _Target = (DirectConnector)target;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var element = new VisualElement();
            var root = EditorGUIUtility.Load("Packages/com.pinioncore.netsync/Editor/Resources/Layouts/Direct/Connector.uxml") as VisualTreeAsset;
            if (root == null)
                return base.CreateInspectorGUI();
            root.CloneTree(element);

            var status = element.Q<Label>("Status");
            status.SetTextBinding(_Target, nameof(_Target.CurrentStatus), BindingMode.ToTarget);

            return element;
        }
    }

}
