using PinionCore.Remote;
using System;
using UnityEngine;
namespace PinionCore.NetSync.Syncs.Souls
{
    public class User : MonoBehaviour
    {
        public ISessionBinder _Binder;
        public ISessionBinder Binder => _Binder;

        internal void Initial(ISessionBinder binder)
        {
            throw new NotImplementedException();
        }

        internal void Final()
        {
            throw new NotImplementedException();
        }
    }

}
