namespace PinionCore.NetSync
{
    public static partial class ProtocolCreator
    {
        public static PinionCore.Remote.IProtocol Create()
        {
            PinionCore.Remote.IProtocol protocol = null;
            _Create(ref protocol);
            return protocol;
        }
        
        [PinionCore.Remote.Protocol.Creater]
        static partial void _Create(ref PinionCore.Remote.IProtocol protocol);
    }

}
