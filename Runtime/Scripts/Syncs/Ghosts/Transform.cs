using PinionCore.NetSync.Syncs.Protocols;
using PinionCore.Remote;
using System;
using System.Diagnostics.Contracts;
using UnityEngine;

namespace PinionCore.NetSync.Syncs.Ghosts
{
    public class Transform : MonoBehaviour 
    {

        System.Action _Sync;

        public Transform()
        {
            _Sync = ()=> { };
        }
        public void Start()
        {
            gameObject.Query<ITransform>().Supply += _OnSupply;
            gameObject.Query<ITransform>().Unsupply += _OnUnsupply;

        }

        public void OnDestroy()
        {
            gameObject.Query<ITransform>().Unsupply -= _OnUnsupply;
            gameObject.Query<ITransform>().Supply -= _OnSupply;
        }

        private void _OnSupply(ITransform transform)
        {
            _Sync = () =>
            {
                gameObject.transform.position = transform.Position;
                gameObject.transform.rotation = transform.Rotation;
                gameObject.transform.localScale = transform.Scale;
            };
        }

        private void _OnUnsupply(ITransform transform)
        {
            _Sync = () => { };
        }

        private void Update()
        {
            _Sync();
        }
    }
    
}
