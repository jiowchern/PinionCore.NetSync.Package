namespace PinionCore.NetSync.Syncs.Protocols
{

    public interface IObject : PinionCore.Remote.Protocolable
    {
        PinionCore.Remote.Property<int> Id { get; }
    }


    namespace Trackers
    {
    }
}


