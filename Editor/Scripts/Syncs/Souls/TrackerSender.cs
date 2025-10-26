using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
namespace PinionCore.NetSync.Editor.Syncs.Souls.Trackers
{
    
    [CustomEditor(typeof(PinionCore.NetSync.Syncs.Souls.Trackers.TrackerSender))]

    public class TrackerSender: UnityEditor.Editor
    {
        private NetSync.Syncs.Souls.Trackers.TrackerSender _Target;

        public void OnEnable()
        {
            if (target == null)
                return;
            _Target = (PinionCore.NetSync.Syncs.Souls.Trackers.TrackerSender)target ;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = EditorGUIUtility.Load("Packages/com.pinioncore.netsync/Editor/Resources/Layouts/Syncs/Souls/TrackerSender.uxml") as VisualTreeAsset;
            if (root == null)
                return new VisualElement();




            var element = new VisualElement();
            root.CloneTree(element);

            var scale = element.Q<IntegerField>(nameof(_Target.Scale)) ;
            scale.SetBinding(nameof(scale.value), new DataBinding
            {
                dataSource = _Target,
                dataSourcePath = new Unity.Properties.PropertyPath(nameof(_Target.Scale)),
                bindingMode = BindingMode.TwoWay,
            });

            var interval = element.Q<FloatField>(nameof(_Target.Interval));
            interval.SetBinding(nameof(interval.value), new DataBinding
            {
                dataSource = _Target,
                dataSourcePath = new Unity.Properties.PropertyPath(nameof(_Target.Interval)),
                bindingMode = BindingMode.TwoWay,
            });

            var steps = element.Q<VisualElement>("Steps");
            serializedObject.Update();
            var instanceSteps = serializedObject.FindProperty("Steps");
            var propertySteps = new UnityEditor.UIElements.PropertyField(instanceSteps);
            
            propertySteps.Bind(serializedObject);
            steps.Add(propertySteps);


            var runSteps = element.Q<Button>("RunSteps");
            runSteps.clicked += _RunTracker;

            runSteps.SetEnabled(UnityEngine.Application.IsPlaying(_Target));



            return element;
        }

        private void _RunTracker()
        {

            _Target.Run(_Target.Steps);

        }
    }
    
}
