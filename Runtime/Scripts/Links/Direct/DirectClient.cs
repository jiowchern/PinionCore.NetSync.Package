namespace PinionCore.NetSync.Direct
{
    /// <summary>
    /// 零序列化直通模式的 ghost 查詢入口:內部以 DirectStandalone 取代 Ghost.Agent,
    /// 不需要 ProtocolProvider,Supply 取得的 ghost 即伺服器端 Soul 實例本身(共用參考)。
    /// 注意:本模式不驗證可序列化性,上線前仍應以 Standalone/Tcp 模式整合測試。
    /// </summary>
    public class DirectClient : QueryerHost, IDirectAgent
    {
        PinionCore.Remote.Ghost.IAgent _Agent;
        DirectEntryRelay _Relay;
        System.IDisposable _AgentDispose;

        public override PinionCore.Remote.INotifierQueryable Queryer => _QueryAgent();

        private PinionCore.Remote.Ghost.IAgent _QueryAgent()
        {
            if (_Agent == null)
            {
                _Relay = new DirectEntryRelay();
                var direct = new PinionCore.Remote.Standalone.DirectStandalone(_Relay);
                _Agent = direct;
                _AgentDispose = direct;
            }

            return _Agent;
        }

        void IDirectAgent.Launch(PinionCore.Remote.IEntry entry)
        {
            PinionCore.Remote.Ghost.IAgent agent = _QueryAgent();
            _Relay.Target = entry;
            // 直通模式沒有 stream,Enable 映射為 DirectStandalone.Launch
            agent.Enable(null);
        }

        void IDirectAgent.Shutdown()
        {
            if (_Agent == null)
            {
                return;
            }

            _Agent.Disable();
            _Relay.Target = null;
        }

        public void Update()
        {
            PinionCore.Remote.Ghost.IAgent agent = _QueryAgent();
            agent.HandleMessages();
            agent.HandlePackets();
        }

        public void OnDestroy()
        {
            IDirectAgent self = this;
            self.Shutdown();
            if (_AgentDispose != null)
            {
                _AgentDispose.Dispose();
            }
        }
    }
}
