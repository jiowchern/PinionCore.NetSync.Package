using PinionCore.NetSync.Syncs.Protocols;
using PinionCore.Remote;
using System;
using System.Diagnostics.Contracts;

namespace PinionCore.NetSync.Syncs.Ghosts
{


    public class Transform : GhostMonoBehaviour<ITransform>
    {

        System.Action _Sync;

        public Transform()
        {
            _Sync = ()=> { };
        }
       

        public void Update()
        {
            _Sync();
        }

        protected override void _OnUnsupply(ITransform transform)
        {
            
        }

        protected override void _OnSupply(ITransform transform)
        {
            _Sync = () =>
            {
                gameObject.transform.position = transform.Position;
                gameObject.transform.rotation = transform.Rotation;
                gameObject.transform.localScale = transform.Scale;
            };
        }
    }
    
}
