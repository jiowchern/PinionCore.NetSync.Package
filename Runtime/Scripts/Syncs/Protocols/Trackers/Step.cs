using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;


namespace PinionCore.NetSync.Syncs.Protocols.Trackers
{

    [System.Serializable]
    public class Step
    {
        public Vector3 Position;
        public uint Repeat;  // 重複的次數, 代表這個紀錄維持時間為 (1 + Repate) * Interval

        public PinionCore.NetSync.Syncs.Protocols.Trackers.ZipStep ToZip(PinionCore.NetSync.Syncs.Protocols.Trackers.ZipPosition min , PinionCore.NetSync.Syncs.Protocols.Trackers.ZipPosition max , uint scale)
        {
         
            return new PinionCore.NetSync.Syncs.Protocols.Trackers.ZipStep
            {
                Position = Position.ToZip(min, max, scale),
                Repeat = Repeat
            };
        }

        
    }

}
