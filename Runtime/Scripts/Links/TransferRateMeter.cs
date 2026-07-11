namespace PinionCore.NetSync
{
    /// <summary>
    /// 每秒傳輸量取樣器:以固定間隔取樣累計位元組數,計算每秒平均值,供 Inspector 顯示。
    /// </summary>
    public class TransferRateMeter
    {
        const float _SampleInterval = 1f;

        long _SampledTotal;
        float _SampleTime;

        public float BytesPerSecond { get; private set; }

        public void Reset(float now)
        {
            _SampledTotal = 0;
            _SampleTime = now;
            BytesPerSecond = 0f;
        }

        public void Update(long total, float now)
        {
            var elapsed = now - _SampleTime;
            if (elapsed < _SampleInterval)
                return;
            BytesPerSecond = (total - _SampledTotal) / elapsed;
            _SampledTotal = total;
            _SampleTime = now;
        }
    }
}
