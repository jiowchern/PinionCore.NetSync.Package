using PinionCore.Remote;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace PinionCore.NetSync.Syncs.Souls
{
    
    public class SoulProvider : MonoBehaviour
    {
        public Server Server;
        public Soul SoulPrefab;
        readonly System.Collections.Generic.Dictionary<IBinder, Soul> _Binders;

        public SoulProvider()
        {
            _Binders = new System.Collections.Generic.Dictionary<IBinder, Soul>();
        }
        public void Start()
        {
            Server.BinderEvent.AddListener(_GetBinder);
        }
        public void OnDestroy()
        {
            Server.BinderEvent.RemoveListener(_GetBinder);
        }
        private void _GetBinder(Server.BinderCommand cmd)
        {
            if (cmd.Status == Server.BinderCommand.OperatorStatus.Add)
            {
                
                var soul = GameObject.Instantiate(SoulPrefab.gameObject, transform);
                var soulBinder = soul.GetComponent<Soul>();
                soulBinder.Initial(cmd.Binder);
                _Binders.Add(cmd.Binder , soulBinder);
            }
            else if (cmd.Status == Server.BinderCommand.OperatorStatus.Remove)
            {
                if (!_Binders.ContainsKey(cmd.Binder))
                {
                    return;
                }
                var soulBinder = _Binders[cmd.Binder];
                soulBinder.Final(cmd.Binder);
                _Binders.Remove(cmd.Binder);
                GameObject.Destroy(soulBinder.gameObject);
            }
        }
    }

}
