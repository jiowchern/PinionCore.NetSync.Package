using System.Linq;
using UnityEngine;

namespace PinionCore.NetSync.Syncs.Protocols
{


    namespace Trackers
    {

        public struct ZipTracker
        {
            public long Ticks;
            public uint Interval;
            public ZipStep[] Steps;
            public ZipPosition Min;
            public ZipPosition Max;

            public Tracker Unzip(uint scale)
            {
                var min = new Vector3(Min.X, Min.Y, Min.Z) / scale;
                var max = new Vector3(Max.X, Max.Y, Max.Z) / scale;
                var scalex = max.x - min.x;
                var scaley = max.y - min.y;
                var scalez = max.z - min.z;
                var minCopy = Min;
                var maxCopy = Max;
                var steps = Steps.Select(v => new Step
                {
                    Position = v.Position.Unzip(minCopy, maxCopy, scale),
                    Repeat = v.Repeat
                }).ToArray();

                var tracker = new Tracker(Interval / (float)scale, steps, Ticks);
                return tracker;
            }
        }
    }
}


