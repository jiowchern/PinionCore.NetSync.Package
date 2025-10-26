using PinionCore.NetSync.Syncs.Protocols;
using UnityEngine;
using UnityEngine.Scripting;

namespace PinionCore.NetSync.Syncs.Ghosts
{
    [RequireComponent(typeof(Ghost))]
    
    public abstract class GhostMonoBehaviour<T>: MonoBehaviour where T : class, IObject
    {
        public virtual void Start()
        {
            
            gameObject.Query<T>().Supply += _OnSupply;
            gameObject.Query<T>().Unsupply += _OnUnsupply;

        }

        public virtual void OnDestroy()
        {
            gameObject.Query<T>().Unsupply -= _OnUnsupply;
            gameObject.Query<T>().Supply -= _OnSupply;
        }

        protected abstract void _OnUnsupply(T t);
        protected abstract void _OnSupply(T t);
    }
    
}
