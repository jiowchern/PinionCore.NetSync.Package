using Unity.Properties;

namespace PinionCore.NetSync
{
    public interface IListenerEditor
    {
        [CreateProperty] bool IsActive { get; }
        [CreateProperty] long BytesReceived { get; }
        [CreateProperty] long BytesSent { get; }
    }
}
