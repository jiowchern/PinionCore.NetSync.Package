namespace PinionCore.NetSync.Tests
{
    public interface IEchoable : PinionCore.Remote.Protocolable
    {
        PinionCore.Remote.Value<int> Echo(int value);
    }

    // 由 PinionCore.Remote.Tools.Protocol.Sources Source Generator 在本組件內
    // 掃描所有繼承 Protocolable 的介面,生成測試用 protocol。
    public static partial class TestProtocolCreator
    {
        public static PinionCore.Remote.IProtocol Create()
        {
            PinionCore.Remote.IProtocol protocol = null;
            _Create(ref protocol);
            return protocol;
        }

        [PinionCore.Remote.Protocol.Creator]
        static partial void _Create(ref PinionCore.Remote.IProtocol protocol);
    }

    public class TestProtocolProvider : PinionCore.NetSync.ProtocolProvider
    {
        readonly PinionCore.Remote.IProtocol _Protocol;

        public TestProtocolProvider()
        {
            _Protocol = TestProtocolCreator.Create();
        }

        public override PinionCore.Remote.IProtocol Get()
        {
            return _Protocol;
        }
    }
}
