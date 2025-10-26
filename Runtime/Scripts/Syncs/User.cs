using PinionCore.Remote;
using System;
using UnityEngine;
namespace PinionCore.NetSync.Syncs.Souls
{
    public class User : MonoBehaviour
    {
        public IBinder _Binder;
        public IBinder Binder => _Binder;

        internal void Initial(IBinder binder)
        {
            throw new NotImplementedException();
        }

        internal void Final()
        {
            throw new NotImplementedException();
        }
    }

}
