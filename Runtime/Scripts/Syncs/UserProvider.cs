using PinionCore.Remote;
using System.Collections.Generic;
using UnityEngine;
namespace PinionCore.NetSync.Syncs.Souls
{
    public class UserProvider : MonoBehaviour
    {
        public Server Server;
        public User UserPrefab;
        readonly System.Collections.Generic.Dictionary<IBinder, User> _Binders;

        public UserProvider()
        {
            _Binders = new System.Collections.Generic.Dictionary<IBinder, User>();
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
                
                var soul = GameObject.Instantiate(UserPrefab.gameObject, transform);
                var user = soul.GetComponent<User>();
                user.Initial(cmd.Binder);
                _Binders.Add(cmd.Binder , user);
            }
            else if (cmd.Status == Server.BinderCommand.OperatorStatus.Remove)
            {
                if (!_Binders.ContainsKey(cmd.Binder))
                {
                    return;
                }
                var user = _Binders[cmd.Binder];
                user.Final();
                _Binders.Remove(cmd.Binder);
                GameObject.Destroy(user.gameObject);
            }
        }
    }

}
