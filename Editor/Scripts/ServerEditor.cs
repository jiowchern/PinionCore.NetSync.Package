using PinionCore.NetSync.Extensions;
using Unity.Properties;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
namespace PinionCore.NetSync.Editor
{
    [CustomEditor(typeof(Server))]
    public class ServerEditor : UnityEditor.Editor
    {
        private Server _Target;
        

        void OnEnable()
        {
            _Target = (Server)target;
        }
        
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            
            var asset = EditorGUIUtility.Load("Packages/com.pinioncore.netsync/Editor/Resources/Layouts/Server.uxml") as VisualTreeAsset;
            
            asset.CloneTree(root);

            var binderevent = root.Q<VisualElement>("BinderEvent");

            serializedObject.Update();
            var testEventProperty = serializedObject.FindProperty("BinderEvent");
            var testEventField = new PropertyField(testEventProperty);
            testEventField.Bind(serializedObject);
            binderevent.Add(testEventField);

            var version = root.Q<Label>("VersionHesh");
            version.SetTextBinding(_Target, nameof(_Target.Hash), BindingMode.ToTarget);
           
            return root;
        }

    }
}
