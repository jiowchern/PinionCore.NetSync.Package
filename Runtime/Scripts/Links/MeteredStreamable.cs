using System;
using System.Runtime.CompilerServices;
using System.Threading;
using PinionCore.Network;
using PinionCore.Remote;

namespace PinionCore.NetSync
{
    /// <summary>
    /// 包裝 <see cref="IStreamable"/>,統計實際送出/接收的位元組數,供 Inspector 顯示。
    /// 不改變資料流內容,只在每次 Send/Receive 完成時回報位元組數。
    /// </summary>
    public class MeteredStreamable : IStreamable
    {
        private readonly IStreamable _Inner;

        public event Action<int> SendEvent;
        public event Action<int> ReceiveEvent;

        public MeteredStreamable(IStreamable inner)
        {
            _Inner = inner;
        }

        IAwaitableSource<int> IStreamable.Receive(byte[] buffer, int offset, int count, CancellationToken token)
        {
            return new MeteredSource(_Inner.Receive(buffer, offset, count, token), _OnReceive);
        }

        IAwaitableSource<int> IStreamable.Send(byte[] buffer, int offset, int count, CancellationToken token)
        {
            return new MeteredSource(_Inner.Send(buffer, offset, count, token), _OnSend);
        }

        private void _OnSend(int bytes)
        {
            SendEvent?.Invoke(bytes);
        }

        private void _OnReceive(int bytes)
        {
            ReceiveEvent?.Invoke(bytes);
        }

        void IDisposable.Dispose()
        {
            _Inner.Dispose();
        }

        private class MeteredSource : IAwaitableSource<int>
        {
            private readonly IAwaitableSource<int> _Source;
            private readonly Action<int> _Report;

            public MeteredSource(IAwaitableSource<int> source, Action<int> report)
            {
                _Source = source;
                _Report = report;
            }

            IAwaitable<int> IAwaitableSource<int>.GetAwaiter()
            {
                return new MeteredAwaitable(_Source.GetAwaiter(), _Report);
            }
        }

        private class MeteredAwaitable : IAwaitable<int>
        {
            private readonly IAwaitable<int> _Awaitable;
            private readonly Action<int> _Report;

            public MeteredAwaitable(IAwaitable<int> awaitable, Action<int> report)
            {
                _Awaitable = awaitable;
                _Report = report;
            }

            bool IAwaitable<int>.IsCompleted => _Awaitable.IsCompleted;

            void INotifyCompletion.OnCompleted(Action continuation)
            {
                _Awaitable.OnCompleted(continuation);
            }

            int IAwaitable<int>.GetResult()
            {
                var bytes = _Awaitable.GetResult();
                _Report(bytes);
                return bytes;
            }
        }
    }
}
