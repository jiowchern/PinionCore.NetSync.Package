using System.Collections;
using NUnit.Framework;
using PinionCore.NetSync.Syncs.Protocols.Trackers;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
namespace PinionCore.NetSync.Tests
{
    public class TrackerTests
    {

        // A Test behaves as an ordinary method
        [Test]
        public void TrackerTest1()
        {
            var records = new PinionCore.NetSync.Syncs.Protocols.Trackers.Step[]
            {
                new PinionCore.NetSync.Syncs.Protocols.Trackers.Step { Position = Vector3.one * 0, Repeat = 0 },
                new PinionCore.NetSync.Syncs.Protocols.Trackers.Step { Position = Vector3.one * 1, Repeat = 0 },
            };
            var time = System.TimeSpan.FromSeconds(10);
            var oneSeconds = System.TimeSpan.FromSeconds(1);
            var tracker = new PinionCore.NetSync.Syncs.Protocols.Trackers.Tracker(1.0f, records, time.Ticks);
            
            var p1 = tracker.Sample(time.Ticks + oneSeconds.Ticks * 0, PinionCore.NetSync.Syncs.Protocols.Trackers.FinalState.Continue);            
            var p2 = tracker.Sample(time.Ticks + oneSeconds.Ticks * 1, PinionCore.NetSync.Syncs.Protocols.Trackers.FinalState.Continue);
            var p3 = tracker.Sample((long)(time.Ticks + oneSeconds.Ticks * 0.5f), PinionCore.NetSync.Syncs.Protocols.Trackers.FinalState.Continue);
            var p4 = tracker.Sample(time.Ticks + oneSeconds.Ticks * 2, PinionCore.NetSync.Syncs.Protocols.Trackers.FinalState.Stop);
            var p5 = tracker.Sample(time.Ticks + oneSeconds.Ticks * 2,  PinionCore.NetSync.Syncs.Protocols.Trackers.FinalState.Continue);
            var p6 = tracker.Sample(time.Ticks + oneSeconds.Ticks * 1, PinionCore.NetSync.Syncs.Protocols.Trackers.FinalState.Continue);


            // to do Assert.AreEqual
            // 驗證結果            
            Assert.That(p1, Is.EqualTo(Vector3.one * 0));
            Assert.That(p2, Is.EqualTo(Vector3.one * 1));
            Assert.That(p3, Is.EqualTo(Vector3.one * 0.5f));
            Assert.That(p4, Is.EqualTo(Vector3.one * 1f));
            Assert.That(p5, Is.EqualTo(Vector3.one * 2f));
            Assert.That(p6, Is.EqualTo(Vector3.one * 1));

        }

        // A Test behaves as an ordinary method
        [Test]
        public void TrackerTest2()
        {
            var records = new PinionCore.NetSync.Syncs.Protocols.Trackers.Step[]
            {
            new PinionCore.NetSync.Syncs.Protocols.Trackers.Step { Position = Vector3.one * 0, Repeat = 1 },
            new PinionCore.NetSync.Syncs.Protocols.Trackers.Step { Position = Vector3.one * 1, Repeat = 1 },            

            };
            var time = System.TimeSpan.FromSeconds(10);

            var tracker = new PinionCore.NetSync.Syncs.Protocols.Trackers.Tracker(1.0f, records, time.Ticks);

            var p1 = tracker.Sample(0f, PinionCore.NetSync.Syncs.Protocols.Trackers.FinalState.Continue);
            var p2 = tracker.Sample(1f, PinionCore.NetSync.Syncs.Protocols.Trackers.FinalState.Continue);
            var p3 = tracker.Sample(0.5f, PinionCore.NetSync.Syncs.Protocols.Trackers.FinalState.Continue);
            var p4 = tracker.Sample(2f, PinionCore.NetSync.Syncs.Protocols.Trackers.FinalState.Stop);
            var p5 = tracker.Sample(2f, PinionCore.NetSync.Syncs.Protocols.Trackers.FinalState.Continue);
            var p6 = tracker.Sample(1f, PinionCore.NetSync.Syncs.Protocols.Trackers.FinalState.Continue);
            var p7 = tracker.Sample(4f, PinionCore.NetSync.Syncs.Protocols.Trackers.FinalState.Continue);
            var p8 = tracker.Sample(4f, PinionCore.NetSync.Syncs.Protocols.Trackers.FinalState.Stop);


            // 驗證結果            
            Assert.That(p1, Is.EqualTo(Vector3.one * 0));
            Assert.That(p2, Is.EqualTo(Vector3.one * 0.5f));
            Assert.That(p3, Is.EqualTo(Vector3.one * 0.25f));
            Assert.That(p4, Is.EqualTo(Vector3.one * 1f));
            Assert.That(p5, Is.EqualTo(Vector3.one * 1f));
            Assert.That(p6, Is.EqualTo(Vector3.one * 0.5f));
            Assert.That(p7, Is.EqualTo(Vector3.one * 3));
            Assert.That(p8, Is.EqualTo(Vector3.one * 1));


        }
        [Test]
        public void TrackerZipTest2()
        {
            var records = new PinionCore.NetSync.Syncs.Protocols.Trackers.Step[]
            {
            new PinionCore.NetSync.Syncs.Protocols.Trackers.Step { Position = new Vector3{x = 27.35f }, Repeat = 89 },
            new PinionCore.NetSync.Syncs.Protocols.Trackers.Step { Position = new Vector3{x= -15.95f }, Repeat = 0 },

            };
            var tracker = new PinionCore.NetSync.Syncs.Protocols.Trackers.Tracker(1.0f, records, 1);
            var zip = tracker.Zip(1000);
            var unzipTracker = zip.Unzip(1000);

            for (int i = 0; i < tracker.Records.Count; i++)
            {
                
                Assert.That(tracker.Records[i].Position.ToZip(zip.Min , zip.Max , 1000).X, Is.EqualTo(unzipTracker.Records[i].Position.ToZip(zip.Min, zip.Max, 1000).X));
                Assert.That(tracker.Records[i].Repeat, Is.EqualTo(unzipTracker.Records[i].Repeat));
            }
        }
        [Test]
        public void TrackerZipTest1()
        {
            var records = new PinionCore.NetSync.Syncs.Protocols.Trackers.Step[]
            {
                new PinionCore.NetSync.Syncs.Protocols.Trackers.Step { Position = Vector3.one * 0, Repeat = 1 },
                new PinionCore.NetSync.Syncs.Protocols.Trackers.Step { Position = Vector3.one * 1, Repeat = 0 },
            };
            var tracker = new PinionCore.NetSync.Syncs.Protocols.Trackers.Tracker(1.0f, records , 1);
            var zip = tracker.Zip(1000);
            var unzipTracker = zip.Unzip(1000);
            Assert.That(tracker.Interval, Is.EqualTo(unzipTracker.Interval));

            for (int i = 0; i < tracker.Records.Count; i++)
            {
                Assert.That(tracker.Records[i].Position, Is.EqualTo(unzipTracker.Records[i].Position));
                Assert.That(tracker.Records[i].Repeat, Is.EqualTo(unzipTracker.Records[i].Repeat));
            }

            
            Assert.That(tracker.BeginTime, Is.EqualTo(unzipTracker.BeginTime)); 


        }
        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator TrackerTestsWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }
    }

}