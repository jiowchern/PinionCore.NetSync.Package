using UnityEngine;

namespace PinionCore.NetSync.Syncs.Protocols
{
    public interface ITransform : PinionCore.Remote.Protocolable , IObject
    {
        PinionCore.Remote.Property<Vector3> Position { get; }
        PinionCore.Remote.Property<Quaternion> Rotation { get; }
        PinionCore.Remote.Property<Vector3> Scale { get; }

        
    }

    

}
