
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;


namespace PinionCore.NetSync.Syncs.Protocols.Trackers
{
    public class Tracker
    {
        public readonly long BeginTime;
        public readonly long EndTime;
        public readonly float Duration; // seconds
        public readonly float Interval; // seconds
        public readonly System.Collections.Generic.List<Step> Records; // 軌跡紀錄
        
        public static List<Protocols.Trackers.Step> Create(List<Vector3> path)
        {
            List<Protocols.Trackers.Step> simplifiedPath = new List<Protocols.Trackers.Step>();
            if (path.Count == 0) return simplifiedPath;

            Protocols.Trackers.Step currentPoint = new Protocols.Trackers.Step { Position = path[0], Repeat = 0 };
            Vector3? previousDirection = null;

            for (int i = 1; i < path.Count; i++)
            {
                Vector3 point = path[i];

                if (point == currentPoint.Position)
                {
                    // 如果點與當前點相同，增加 repeat
                    currentPoint.Repeat++;
                }
                else
                {
                    if (i == path.Count - 1)
                    {
                        // 如果是最後一個點，且與當前點不同，保存當前點並添加新點
                        simplifiedPath.Add(currentPoint);
                        currentPoint = new Protocols.Trackers.Step { Position = point, Repeat = 0 };
                        simplifiedPath.Add(currentPoint);
                    }
                    else
                    {
                        Vector3 direction = (point - currentPoint.Position).normalized;

                        if (previousDirection == null)
                        {
                            previousDirection = direction;
                            currentPoint.Repeat++;
                        }
                        else
                        {
                            float angle = Vector3.Angle(previousDirection.Value, direction);

                            if (angle <= 1f)
                            {
                                // 方向相同，累計 repeat，不更新位置
                                currentPoint.Repeat++;
                            }
                            else
                            {
                                // 方向不同，保存當前點並開始新段
                                simplifiedPath.Add(currentPoint);
                                currentPoint = new Protocols.Trackers.Step { Position = point, Repeat = 0 };
                                previousDirection = direction;
                            }
                        }
                    }
                }
            }

            // 如果最後一個點沒有添加到結果列表，則添加
            if (simplifiedPath.Count == 0 || simplifiedPath[^1] != currentPoint)
            {
                simplifiedPath.Add(currentPoint);
            }

            return simplifiedPath;
        }
        public Tracker(float interval, IEnumerable<Vector3> records , long begin) : this(interval, Create(records.ToList()), begin)
        {
            
        }
        public Tracker(float interval, IEnumerable<Step> records, long ticks)
        {
            
            Interval = interval;
            Records = records.ToList();
            BeginTime = ticks;
            EndTime = BeginTime + TimeSpan.FromSeconds(Interval * Records.Sum(v => 1 + v.Repeat)).Ticks;
            Duration = (float)(TimeSpan.FromTicks(EndTime - BeginTime).TotalSeconds);
        }
        public PinionCore.NetSync.Syncs.Protocols.Trackers.ZipTracker Zip(uint scale)
        {
            var r = new Protocols.Trackers.ZipTracker();

            var min = new ZipPosition();
            min.X = (int)(Records.Min(v => v.Position.x) * scale);
            min.Y = (int)(Records.Min(v => v.Position.y) * scale);
            min.Z = (int)(Records.Min(v => v.Position.z) * scale);


            var max = new ZipPosition();
            max.X = (int)(Records.Max(v => v.Position.x) * scale - min.X );
            max.Y = (int)(Records.Max(v => v.Position.y) * scale - min.Y );
            max.Z = (int)(Records.Max(v => v.Position.z) * scale - min.Z );

            var steps =  Records.Select(v => v.ToZip(min, max, scale)).ToArray();
            r.Interval = (uint)(Interval * scale);
            r.Min = min;
            r.Max = max;
            r.Steps = steps;
            r.Ticks = BeginTime;
            return r;
        }
        public Vector3 Sample(long ticks, FinalState state)
        {
            var time = System.TimeSpan.FromTicks(ticks - BeginTime);
            return Sample((float)time.TotalSeconds, state);
        }
        // 根據時間取樣位置
        public Vector3 Sample(float seconds, FinalState state)
        {
            if (Records.Count < 1)
                throw new System.InvalidOperationException("無法取樣空的 Tracker");

            if (Records.Count == 1 || seconds <= 0)
                return Records[0].Position;

            float totalElapsed = 0.0f;

            // 遍歷所有紀錄，找到對應的時間段
            for (int i = 0; i < Records.Count - 1; i++)
            {
                Step currentRecord = Records[i];
                float recordDuration = (1 + currentRecord.Repeat) * Interval;

                if (seconds >= totalElapsed && seconds < totalElapsed + recordDuration)
                {
                    // 如果 seconds 位於當前紀錄的時間段中，則在該紀錄與下一紀錄之間插值
                    Step nextRecord = Records[i + 1];
                    float t = (seconds - totalElapsed) / recordDuration;
                    return Vector3.Lerp(currentRecord.Position, nextRecord.Position, t);
                }

                totalElapsed += recordDuration;                
            }
            
            // 如果 seconds 超過所有的紀錄時間或已經是最後一個紀錄點
            return handleFinalState(seconds, totalElapsed, state);
        }

        private Vector3 handleFinalState(float seconds, float totalElapsed, FinalState state)
        {
            switch (state)
            {
                case FinalState.Stop:
                    // 停止在最後一個紀錄點
                    return Records[Records.Count - 1].Position;

                case FinalState.Continue:
                    // 繼續推測方向，取最後兩個點做向量推測
                    if (Records.Count < 2)
                        return Records[Records.Count - 1].Position;

                    Step lastRecord = Records[Records.Count - 1];
                    Step secondLastRecord = Records[Records.Count - 2];

                    Vector3 direction = lastRecord.Position - secondLastRecord.Position;
                    float remainingSeconds = seconds - totalElapsed;

                    return lastRecord.Position + direction.normalized * (remainingSeconds / Interval) * direction.magnitude;

                default:
                    throw new System.ArgumentOutOfRangeException(nameof(state), "未知的 FinalState");
            }
        }

        
    }

}
