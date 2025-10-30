using PinionCore.NetSync.Syncs.Protocols.Trackers;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace PinionCore.NetSync.Syncs.Ghosts
{
    public class TrackerReceiver : MonoBehaviour
    {
        public uint Scale;
        public readonly System.Collections.Generic.Queue<Tracker> _Tackers;
        public Vector3 Position;
        public Vector3[] Positions;
        public Vector3[] ZipPositions;
        public float Interval;
        float _Seconds;
        public float _StepSeconds;


        public TrackerReceiver()
        {
            _Tackers = new System.Collections.Generic.Queue<Tracker>();
        }
        public void Start()
        {
            
            gameObject.Query<ITracker>().Supply += _OnSupply;
            gameObject.Query<ITracker>().Unsupply += _OnUnsupply;
        }

        void OnDestroy()
        {
            gameObject.Query<ITracker>().Supply -= _OnSupply;
            gameObject.Query<ITracker>().Unsupply -= _OnUnsupply;
        }

        private void _OnUnsupply(ITracker tracker)
        {
            UnityEngine.Debug.Log($"_OnUnsupply tracker:{tracker.Id}");

            tracker.OnTrackerEvent -= _OnTrackerEvent    ;
        }

        private void _OnSupply(ITracker tracker)
        {
            UnityEngine.Debug.Log($"_OnSupply tracker:{tracker.Id}");
            tracker.OnTrackerEvent += _OnTrackerEvent;
        }


        private void _OnTrackerEvent(ZipTracker obj)
        {
            _Tackers.Enqueue(obj.Unzip(Scale));
            //Positions = _Tracker.Records.Select(r => r.Position).ToArray();
            //ZipPositions = obj.Steps.Select(r => new Vector3(r.Position.X, r.Position.Y, r.Position.Z)).ToArray();
            //_StepSeconds = 0;
        }

        public void Update()
        {
            
            
            if (_Tackers.Count == 0)
                return;
            var delta = UnityEngine.Time.deltaTime;
            _Seconds += delta;
            if (_Seconds < Interval)
            {
                return;
            }
            _StepSeconds += _Seconds;
            _Seconds = 0;

            var tracker = _Tackers.Peek();
            
            


            var position = tracker.Sample(System.DateTime.Now.Ticks, FinalState.Stop);
            
            Position = position;

            transform.position = position;

            if (tracker.Duration >= _StepSeconds)               
            {                
                return;
            }
            _StepSeconds = 0;
            _Tackers.Dequeue();
        }
    }
    
}
