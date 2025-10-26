using UnityEditor;
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
        public int _DataReceiveds;
        public int _DataSends;

        [CreateProperty]public bool IsActive => _Target.IsActive;
        public void OnEnable()
        {
            _Target = target as T;
            _Target.DataSendEvent += _OnDataSendEvent;
            _Target.DataReceivedEvent += _OnDataReceivedEvent;


        }

        private void _OnDataReceivedEvent(int data)
        {
            System.Threading.Interlocked.Add(ref _DataReceiveds, data);
        }

        private void _OnDataSendEvent(int data)
        {
            System.Threading.Interlocked.Add(ref _DataSends, data);            
        }

        public void OnDisable()
        {
            _Target.DataSendEvent -= _OnDataSendEvent;
            _Target.DataReceivedEvent -= _OnDataReceivedEvent;
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
            send.SetTextBinding(this, nameof(_DataSends), BindingMode.ToTarget);

            var receive = element.Q<Label>("Receive");
            receive.SetTextBinding(this, nameof(_DataReceiveds), BindingMode.ToTarget);


            return element;
        }
    }
}
