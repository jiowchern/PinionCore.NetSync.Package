using PinionCore.Remote;
using System;
using UnityEngine;
namespace PinionCore.NetSync.Sessions
{
    public abstract class User : MonoBehaviour
    {
        

        public abstract void Initial(ISessionBinder binder);

        public abstract void Final(ISessionBinder binder);
        
    }

}
