using NUnit.Framework;
using System.Linq;
namespace PinionCore.NetSync.Tests
{
    public class NetworkTests
    {
        // A Test behaves as an ordinary method
        /*[Test]
        public void NetworkTest1()
        {
            // Use the Assert class to test conditions
            var listener = new PinionCore.Network.Tcp.Listener();
            listener.AcceptEvent += (peer) =>
            {
                var webPeer = new PinionCore.NetSync.Web.WebPeer(peer);
                webPeer.Receive(new byte[1024], 0, 1024);
            };
            listener.Bind(12345);
            Assert.Pass("Your first passing test");
        }
        public async System.Threading.Tasks.Task WebConnectTest()
        {
            var listebner = new PinionCore.Network.Tcp.Listener();
            var handshaker = new PinionCore.Network.Tcp.WebHandshark(listebner);
            var port = PinionCore.Network.Tcp.Tools.GetAvailablePort();
            listebner.Bind(port);
            var peers = new System.Collections.Generic.List<PinionCore.Network.Tcp.WebPeer>();
            handshaker.AcceptEvent += (peer) =>
            {
                peer.Peer.BreakEvent += () => { NUnit.Framework.Assert.Fail(); };
                peers.Add(peer);
            };

            var connector = new PinionCore.Network.Web.Connecter(new System.Net.WebSockets.ClientWebSocket());
            var result = await connector.ConnectAsync($"ws://localhost:{port}/");
            NUnit.Framework.Assert.IsTrue(result);

            while (peers.Count == 0)
            {
                await System.Threading.Tasks.Task.Delay(100);
            }
            var peer = peers.Single();

            IStreamable client = connector;
            IStreamable server = peer;
            {
                var sbuffer = PinionCore.Memorys.PoolProvider.Shared.Alloc(5);


                sbuffer[0] = 1;
                sbuffer[1] = 2;
                sbuffer[2] = 3;
                sbuffer[3] = 4;
                sbuffer[4] = 5;
                var sbytes = sbuffer.Bytes;
                var count = await client.Send(sbytes.Array, sbytes.Offset, sbytes.Count);


                var rbuffer = PinionCore.Memorys.PoolProvider.Shared.Alloc(5);
                var rbytes = rbuffer.Bytes;
                var count2 = await server.Receive(rbytes.Array, rbytes.Offset, rbytes.Count);
                var r1 = rbuffer[0];
                NUnit.Framework.Assert.AreEqual(r1, 1);
                NUnit.Framework.Assert.AreEqual(rbuffer[1], 2);
                NUnit.Framework.Assert.AreEqual(rbuffer[2], 3);
                NUnit.Framework.Assert.AreEqual(rbuffer[3], 4);
                NUnit.Framework.Assert.AreEqual(rbuffer[4], 5);
            }
            {
                var sbuffer = PinionCore.Memorys.PoolProvider.Shared.Alloc(5);


                sbuffer[0] = 1;
                sbuffer[1] = 2;
                sbuffer[2] = 3;
                sbuffer[3] = 4;
                sbuffer[4] = 5;
                var sbytes = sbuffer.Bytes;
                var count = await server.Send(sbytes.Array, sbytes.Offset, sbytes.Count);


                var rbuffer = PinionCore.Memorys.PoolProvider.Shared.Alloc(5);
                var rbytes = rbuffer.Bytes;
                var count2 = await client.Receive(rbytes.Array, rbytes.Offset, rbytes.Count);
                var r1 = rbuffer[0];
                NUnit.Framework.Assert.AreEqual(r1, 1);
                NUnit.Framework.Assert.AreEqual(rbuffer[1], 2);
                NUnit.Framework.Assert.AreEqual(rbuffer[2], 3);
                NUnit.Framework.Assert.AreEqual(rbuffer[3], 4);
                NUnit.Framework.Assert.AreEqual(rbuffer[4], 5);
            }

        }

        */
    }

}