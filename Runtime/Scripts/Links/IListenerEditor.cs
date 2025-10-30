using Unity.Properties;

namespace PinionCore.NetSync
{
    public interface IListenerEditor
    {
        event System.Action<int> DataReceivedEvent;
        event System.Action<int> DataSendEvent;
        [CreateProperty] bool IsActive { get; }
    }
}
