using PinionCore.NetSync.Syncs.Protocols;
using PinionCore.Remote;
using PinionCore.Utility;
using System;
using System.Reflection;
using UnityEngine;


namespace PinionCore.NetSync.Syncs.Souls
{
    public class UserBinder<T>
    {
        private readonly T _Owner;
        readonly System.Collections.Generic.Dictionary<User, ISoul> _Binders;

        public UserBinder(T owner)
        {
            _Binders = new System.Collections.Generic.Dictionary<User, ISoul>();
            this._Owner = owner;
        }
        internal void Release()
        {
            throw new NotImplementedException();
        }

        internal void Bind(User user)
        {
            var soul = user.Binder.Bind(_Owner);
            _Binders.Add(user, soul);
        }

        internal void Unbind(User user)
        {
            if(_Binders.TryGetValue(user, out var soul))
            {
                user.Binder.Unbind(soul);
                _Binders.Remove(user);
            }
        }
    }

    public class Transform : MonoBehaviour, Protocols.ITransform 
    {
        
        readonly private PinionCore.Remote.Property<Vector3> _Position;
        readonly private PinionCore.Remote.Property<Quaternion> _Rotation;
        readonly private PinionCore.Remote.Property<Vector3> _Scale;
        public float SyncInterval = 1f;
        float _TimeCounter;
        readonly UserBinder<ITransform> _Binders;        

        Property<Vector3> ITransform.Position => _Position;

        Property<Quaternion> ITransform.Rotation => _Rotation;

        Property<Vector3> ITransform.Scale => _Scale;

        Property<int> IObject.Id => new Property<int>(gameObject.GetInstanceID());

        public Transform()
        {
            _Binders = new UserBinder<ITransform>(this);
            _Position = new Property<Vector3>();
            _Rotation = new Property<Quaternion>();
            _Scale = new Property<Vector3>();
        }
        public void Start()
        {
            _Position.Value = transform.position;
            _Rotation.Value = transform.rotation;
            _Scale.Value = transform.localScale;

            
        }

        public void UserEnter(User user)
        {
            _Binders.Bind(user);
         
        }

        public void UserLeave(User user)
        {
            _Binders.Unbind(user);
         
        }

        // Update is called once per frame
        public void Update()
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
            if(!transform.hasChanged)
                return false;
            transform.hasChanged = false;
            // check if the transform has changed
            if (transform.position != _Position)
            {
                _Position.Value = transform.position;                
            }
            if (transform.rotation != _Rotation)
            {
                _Rotation.Value = transform.rotation;                
            }
            if (transform.localScale != _Scale)
            {
                _Scale.Value = transform.localScale;                
            }
            return true;
        }

        public void OnDestroy()
        {
            _Binders.Release();
          // gameObject.Unbind(_Soul);
        }
    }

}
