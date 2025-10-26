namespace PinionCore.NetSync.Syncs.Protocols.Trackers
{
    public interface ITracker :  PinionCore.Remote.Protocolable , IObject
    {
        event System.Action<ZipTracker> OnTrackerEvent;
    }

}
