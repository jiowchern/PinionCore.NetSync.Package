using PinionCore.Remote;
using System;
using System.Reflection;
using UnityEngine;

namespace PinionCore.NetSync
{
    
    public abstract class ProtocolProvider : ScriptableObject , IProtocol
    {
        Assembly IProtocol.Base => Get().Base;

        Type[] IProtocol.SerializeTypes => Get().SerializeTypes;

        byte[] IProtocol.VersionCode => Get().VersionCode;

        public abstract PinionCore.Remote.IProtocol Get();

        Remote.EventProvider IProtocol.GetEventProvider()
        {
            return Get().GetEventProvider();
        }

        InterfaceProvider IProtocol.GetInterfaceProvider()
        {
            return Get().GetInterfaceProvider();
        }

        MemberMap IProtocol.GetMemberMap()
        {
            return Get().GetMemberMap();
        }
    }
}
