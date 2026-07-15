using System;
using System.Collections.Generic;
using System.Threading;
using PinionCore.Network;
using PinionCore.Remote;

namespace PinionCore.NetSync
{
    /// <summary>
    /// 包裝 <see cref="IStreamable"/>,模擬單向網路延遲(秒)。
    /// Send 進來的資料先複製進佇列,到期後由 <see cref="Update"/> 依序泵入內層;Receive 直通。
    /// 延遲為 0 且無待送資料時完全直通,行為與未包裝一致。
    /// </summary>
    public class LagStreamable : IStreamable
    {
        struct Packet
        {
            public double DueSeconds;
            public byte[] Data;
        }

        private readonly IStreamable _Inner;
        private readonly Func<float> _LagSecondsProvider;
        private readonly System.Diagnostics.Stopwatch _Clock;
        private readonly Queue<Packet> _Pendings;

        public LagStreamable(IStreamable inner, Func<float> lagSecondsProvider)
        {
            _Inner = inner;
            _LagSecondsProvider = lagSecondsProvider;
            _Clock = System.Diagnostics.Stopwatch.StartNew();
            _Pendings = new Queue<Packet>();
        }

        IAwaitableSource<int> IStreamable.Send(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return 0.ToWaitableValue();

            var lag = _LagSecondsProvider();
            lock (_Pendings)
            {
                // 佇列非空時即使 lag=0 也要排隊(due = now),避免超車導致亂序。
                if (lag <= 0f && _Pendings.Count == 0)
                    return _Inner.Send(buffer, offset, count, token);

                var data = new byte[count];
                System.Buffer.BlockCopy(buffer, offset, data, 0, count);
                _Pendings.Enqueue(new Packet { DueSeconds = _Clock.Elapsed.TotalSeconds + lag, Data = data });
            }
            return count.ToWaitableValue();
        }

        IAwaitableSource<int> IStreamable.Receive(byte[] buffer, int offset, int count, CancellationToken token)
        {
            return _Inner.Receive(buffer, offset, count, token);
        }

        /// <summary>
        /// 泵出所有已到期的資料。只從頭部依序泵出,執行期間調整延遲也不會亂序。
        /// </summary>
        public void Update()
        {
            while (true)
            {
                byte[] data;
                lock (_Pendings)
                {
                    if (_Pendings.Count == 0)
                        return;
                    if (_Pendings.Peek().DueSeconds > _Clock.Elapsed.TotalSeconds)
                        return;
                    data = _Pendings.Dequeue().Data;
                }
                _Inner.Send(data, 0, data.Length, default);
            }
        }

        void IDisposable.Dispose()
        {
            lock (_Pendings)
            {
                _Pendings.Clear();
            }
            _Inner.Dispose();
        }
    }
}
