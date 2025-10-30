using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PinionCore.NetSync.Extensions;
using static UnityEngine.GraphicsBuffer;
using Unity.Properties;
using System.Net.NetworkInformation;
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
            return base.CreateInspectorGUI();
            /*var element = new VisualElement();
            var root = EditorGUIUtility.Load("Packages/com.pinioncore.netsync/Editor/Resources/Layouts/Client.uxml") as VisualTreeAsset ;
            if(root == null)
                return base.CreateInspectorGUI();
            root.CloneTree(element);
            var version = element.Q<Label>("VersionHesh");
            version.SetTextBinding(_Target, nameof(_Target.Hash), BindingMode.ToTarget);

            var ping = element.Q<Label>("Ping");
            ping.SetTextBinding(_Target, nameof(_Target.Ping), BindingMode.ToTarget);
            return element;*/
        }


    }
}
