using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using PinionCore.NetSync.Extensions;
using Unity.Properties;
namespace PinionCore.NetSync.Editor
{

    [CustomEditor(typeof(Client)) ]
    public class ClientEditor : UnityEditor.Editor
    {        
        
        Client _Target;
        void OnEnable()
        {
            _Target = (Client)target;
        }
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var asset = EditorGUIUtility.Load("Packages/com.pinioncore.netsync/Editor/Resources/Layouts/Client.uxml") as VisualTreeAsset;
            if (asset == null)
                return base.CreateInspectorGUI();

            asset.CloneTree(root);

            serializedObject.Update();
            var providerProperty = serializedObject.FindProperty("Provider");
            var providerField = new PropertyField(providerProperty);
            providerField.Bind(serializedObject);
            root.Insert(0, providerField);

            var version = root.Q<Label>("VersionHesh");
            version.SetTextBinding(_Target, nameof(_Target.Hash), BindingMode.ToTarget);

            var ping = root.Q<Label>("Ping");
            ping.SetTextBinding(_Target, nameof(_Target.Ping), BindingMode.ToTarget);

            return root;
        }


    }
}
