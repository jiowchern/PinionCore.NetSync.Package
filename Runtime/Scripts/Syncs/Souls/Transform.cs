using PinionCore.NetSync.Syncs.Protocols;
using PinionCore.Remote;
using System;
using UnityEngine;


namespace PinionCore.NetSync.Syncs.Souls
{
    public class Transform : MonoBehaviour, Protocols.ITransform 
    {
        
        readonly private PinionCore.Remote.Property<Vector3> _Position;
        readonly private PinionCore.Remote.Property<Quaternion> _Rotation;
        readonly private PinionCore.Remote.Property<Vector3> _Scale;
        public float SyncInterval = 1f;
        float _TimeCounter;
        private ISoul _Soul;

        Property<Vector3> ITransform.Position => _Position;

        Property<Quaternion> ITransform.Rotation => _Rotation;

        Property<Vector3> ITransform.Scale => _Scale;

        Property<int> IObject.Id => new Property<int>(gameObject.GetInstanceID());

        public Transform()
        {
            _Position = new Property<Vector3>();
            _Rotation = new Property<Quaternion>();
            _Scale = new Property<Vector3>();
        }
        public void Start()
        {
            _Position.Value = transform.position;
            _Rotation.Value = transform.rotation;
            _Scale.Value = transform.localScale;

            _Soul = gameObject.Bind<ITransform>(this);
                      
        }

        

        // Update is called once per frame
        void Update()
        {
            _TimeCounter += UnityEngine.Time.deltaTime;
            if (_TimeCounter < SyncInterval)
            {
                return;
            }

           _Sync(transform);
            _TimeCounter = 0;
        }

        private bool _Sync(UnityEngine.Transform transform)
        {
            // check if the transform has changed
            if (transform.position != _Position)
            {
                _Position.Value = transform.position;
                return true;
            }
            if (transform.rotation != _Rotation)
            {
                _Rotation.Value = transform.rotation;
                return true;
            }
            if (transform.localScale != _Scale)
            {
                _Scale.Value = transform.localScale;
                return true;
            }
            return false;
        }

        public void OnDestroy()
        {
           gameObject.Unbind(_Soul);
        }
    }

}
