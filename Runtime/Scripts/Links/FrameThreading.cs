using System.Collections.Concurrent;
using System.Threading;

namespace PinionCore.NetSync
{
    /// <summary>
    /// Unity host 的執行緒政策:
    /// Dispatch 的工作排入佇列,由主執行緒每 frame 的 Update() 依序消化(遊戲邏輯僅存活於主執行緒);
    /// Launch 以空 SynchronizationContext 啟動幫浦,讓 await 續跑落在 thread pool,不被 frame 節奏錨定。
    /// </summary>
    public class FrameThreading : PinionCore.Network.IThreading
    {
        private readonly ConcurrentQueue<System.Action> _Works;

        public FrameThreading()
        {
            _Works = new ConcurrentQueue<System.Action>();
        }

        void PinionCore.Network.IThreading.Dispatch(System.Action work)
        {
            _Works.Enqueue(work);
        }

        void PinionCore.Network.IThreading.Launch(System.Action pump)
        {
            SynchronizationContext previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);
            try
            {
                pump();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        public void Update()
        {
            while (_Works.TryDequeue(out System.Action work))
            {
                work();
            }
        }
    }
}
