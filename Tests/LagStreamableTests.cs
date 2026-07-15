using System.Threading;
using NUnit.Framework;
using PinionCore.Network;

namespace PinionCore.NetSync.Tests
{
    public class LagStreamableTests
    {
        /// <summary>
        /// 延遲下限與內容正確性:lag=0.1s 時資料不會提前到達,到期泵出後內容正確。
        /// </summary>
        [Test, Timeout(5000)]
        public void SendIsDelayedUntilLagElapsed()
        {
            var stream = new PinionCore.Network.Stream();
            var lagStreamable = new LagStreamable(stream, _Lag01);
            IStreamable sender = lagStreamable;

            var payload = new byte[] { 1, 2, 3, 4 };
            sender.Send(payload, 0, payload.Length, CancellationToken.None);

            var popBuf = new byte[4];
            var popSource = stream.Pop(popBuf, 0, popBuf.Length);
            var popAwaiter = popSource.GetAwaiter();

            lagStreamable.Update();
            Assert.IsFalse(popAwaiter.IsCompleted);

            System.Threading.Thread.Sleep(150);
            lagStreamable.Update();
            Assert.IsTrue(popAwaiter.IsCompleted);
            Assert.AreEqual(payload.Length, popAwaiter.GetResult());
            CollectionAssert.AreEqual(payload, popBuf);
        }

        /// <summary>
        /// lag=0 直通:不呼叫 Update() 也能立刻收到,行為與未包裝一致。
        /// </summary>
        [Test, Timeout(5000)]
        public void ZeroLagPassesThroughImmediately()
        {
            var stream = new PinionCore.Network.Stream();
            var lagStreamable = new LagStreamable(stream, _LagZero);
            IStreamable sender = lagStreamable;

            var payload = new byte[] { 5, 6, 7 };
            sender.Send(payload, 0, payload.Length, CancellationToken.None);

            var popBuf = new byte[3];
            var popSource = stream.Pop(popBuf, 0, popBuf.Length);
            var popAwaiter = popSource.GetAwaiter();

            Assert.IsTrue(popAwaiter.IsCompleted);
            Assert.AreEqual(payload.Length, popAwaiter.GetResult());
            CollectionAssert.AreEqual(payload, popBuf);
        }

        /// <summary>
        /// 執行期調整 lag 不亂序:lag=0.2 送 A 後改 lag=0 送 B,B 不會超車,接收順序仍是 A、B。
        /// </summary>
        [Test, Timeout(5000)]
        public void RuntimeLagChangeKeepsOrder()
        {
            var lag = 0.2f;
            var stream = new PinionCore.Network.Stream();
            var lagStreamable = new LagStreamable(stream, () => lag);
            IStreamable sender = lagStreamable;

            var first = new byte[] { 1 };
            sender.Send(first, 0, first.Length, CancellationToken.None);

            lag = 0f;
            var second = new byte[] { 2 };
            sender.Send(second, 0, second.Length, CancellationToken.None);

            // Pop 語意同 socket read:有資料就返回,兩筆 Push 要分兩次 pop 驗證順序。
            var firstBuf = new byte[1];
            var firstSource = stream.Pop(firstBuf, 0, firstBuf.Length);
            var firstAwaiter = firstSource.GetAwaiter();

            lagStreamable.Update();
            Assert.IsFalse(firstAwaiter.IsCompleted);

            System.Threading.Thread.Sleep(250);
            lagStreamable.Update();
            Assert.IsTrue(firstAwaiter.IsCompleted);
            Assert.AreEqual(1, firstAwaiter.GetResult());
            Assert.AreEqual((byte)1, firstBuf[0]);

            var secondBuf = new byte[1];
            var secondSource = stream.Pop(secondBuf, 0, secondBuf.Length);
            var secondAwaiter = secondSource.GetAwaiter();
            Assert.IsTrue(secondAwaiter.IsCompleted);
            Assert.AreEqual(1, secondAwaiter.GetResult());
            Assert.AreEqual((byte)2, secondBuf[0]);
        }

        private float _Lag01()
        {
            return 0.1f;
        }

        private float _LagZero()
        {
            return 0f;
        }
    }
}
