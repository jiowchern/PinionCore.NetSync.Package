namespace PinionCore.NetSync.Direct
{
    /// <summary>
    /// IEntry 轉發殼:DirectStandalone 建構子需要 IEntry,但連線目標(Server)要到
    /// Connect 才確定;此殼讓 agent 可先建立,使 DirectClient.Queryer 隨時可訂閱
    /// (對齊 Client 延遲建 Agent 的語意),之後再接上實際的 entry。
    /// </summary>
    class DirectEntryRelay : PinionCore.Remote.IEntry
    {
        public PinionCore.Remote.IEntry Target;

        void PinionCore.Remote.ISessionObserver.OnSessionOpened(PinionCore.Remote.ISessionBinder binder)
        {
            PinionCore.Remote.IEntry target = Target;
            if (target == null)
            {
                return;
            }
            target.OnSessionOpened(binder);
        }

        void PinionCore.Remote.ISessionObserver.OnSessionClosed(PinionCore.Remote.ISessionBinder binder)
        {
            PinionCore.Remote.IEntry target = Target;
            if (target == null)
            {
                return;
            }
            target.OnSessionClosed(binder);
        }

        void PinionCore.Remote.IEntry.Update()
        {
            PinionCore.Remote.IEntry target = Target;
            if (target == null)
            {
                return;
            }
            target.Update();
        }
    }
}
