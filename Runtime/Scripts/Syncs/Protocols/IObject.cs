using UnityEngine;

namespace PinionCore.NetSync.Syncs.Protocols
{

    public interface IObject : PinionCore.Remote.Protocolable
    {
        PinionCore.Remote.Property<EntityId> Id { get; }
    }
}


