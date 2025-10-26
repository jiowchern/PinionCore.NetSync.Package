namespace PinionCore.NetSync.Syncs.Protocols.Trackers
{
    public enum FinalState
    {
        Stop, // 當取樣到最後一個點則維持最後的點
        Continue // 從最後一個點推測其向量回傳最新的位置
    }

}
