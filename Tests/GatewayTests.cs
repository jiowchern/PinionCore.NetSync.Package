using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PinionCore.NetSync.Tests
{
    public class GatewayTests
    {
        class EchoSoul : IEchoable
        {
            PinionCore.Remote.Value<int> IEchoable.Echo(int value)
            {
                return value;
            }
        }

        /// <summary>
        /// Gateway 全流程整合測試(Standalone 連線):
        /// GatewayRouter(Registry/Session 端點) ← GatewayRegistry+Server 註冊、GatewayClient 連線,
        /// 驗證客戶端可透過 Router 取得遊戲服務介面並呼叫方法。
        /// </summary>
        [UnityTest]
        public IEnumerator GatewayRouterRegistryClientEndToEnd()
        {
            var provider = ScriptableObject.CreateInstance<TestProtocolProvider>();

            // ── Router ──
            var routerGo = new GameObject("GatewayRouter");
            routerGo.AddComponent<Gateways.GatewayRouter>();

            var registryEndpointGo = new GameObject("RegistryEndpoint");
            registryEndpointGo.transform.SetParent(routerGo.transform);
            registryEndpointGo.AddComponent<Gateways.GatewayRouterEndpoint>().Endpoint = Gateways.GatewayRouterEndpointType.Registry;
            var registryEndpointListener = registryEndpointGo.AddComponent<Standalone.Listener>();
            registryEndpointListener.Bind();

            var sessionEndpointGo = new GameObject("SessionEndpoint");
            sessionEndpointGo.transform.SetParent(routerGo.transform);
            sessionEndpointGo.AddComponent<Gateways.GatewayRouterEndpoint>().Endpoint = Gateways.GatewayRouterEndpointType.Session;
            var sessionEndpointListener = sessionEndpointGo.AddComponent<Standalone.Listener>();
            sessionEndpointListener.Bind();

            // ── 遊戲服務 + Registry ──
            var serverGo = new GameObject("Server");
            var server = serverGo.AddComponent<Server>();
            server.Provider = provider;
            if (server.BinderEvent == null)
            {
                server.BinderEvent = new UnityEngine.Events.UnityEvent<Server.BinderCommand>();
            }
            var soul = new EchoSoul();
            server.BinderEvent.AddListener(command =>
            {
                if (command.Status == Server.BinderCommand.OperatorStatus.Add)
                {
                    command.Binder.Bind<IEchoable>(soul);
                }
            });

            var registry = serverGo.AddComponent<Gateways.GatewayRegistry>();
            registry.Provider = provider;
            registry.Group = 1;
            var registryConnector = serverGo.AddComponent<Standalone.Connector>();
            

            // ── 客戶端 ──
            var clientGo = new GameObject("GatewayClient");
            var client = clientGo.AddComponent<Gateways.GatewayClient>();
            client.Provider = provider;
            var clientConnector = clientGo.AddComponent<Standalone.Connector>();
            

            IEchoable echo = null;
            client.Queryer.QueryNotifier<IEchoable>().Supply += e => echo = e;

            // 等待 Start
            yield return null;

            registryConnector.Connect(registryEndpointListener);
            yield return null;
            clientConnector.Connect(sessionEndpointListener);

            for (var i = 0; i < 600 && echo == null; ++i)
            {
                yield return null;
            }
            Assert.NotNull(echo, "客戶端未能透過 Gateway 取得 IEchoable 服務");

            int? result = null;
            echo.Echo(42).OnValue += v => result = v;
            for (var i = 0; i < 600 && !result.HasValue; ++i)
            {
                yield return null;
            }
            Assert.IsTrue(result.HasValue, "Echo 呼叫未收到回傳值");
            Assert.AreEqual(42, result.Value);

            Object.Destroy(clientGo);
            Object.Destroy(serverGo);
            Object.Destroy(routerGo);
            Object.Destroy(provider);
            yield return null;
        }

        /// <summary>
        /// Gateway 真實 TCP 整合測試:
        /// Router 端點掛標準 TcpListener(IListenableHost 架構),服務端與客戶端以 TcpConnector 連入。
        /// </summary>
        [UnityTest]
        public IEnumerator GatewayRouterTcpEndToEnd()
        {
            var provider = ScriptableObject.CreateInstance<TestProtocolProvider>();

            var ports = System.Linq.Enumerable.ToArray(PinionCore.Network.Tcp.Tools.GetAvailablePorts(2));
            var registryPort = ports[0];
            var sessionPort = ports[1];

            // ── Router(標準 IListenableHost + TcpListener 架構)──
            var routerGo = new GameObject("GatewayRouter");
            routerGo.AddComponent<Gateways.GatewayRouter>();

            var registryEndpointGo = new GameObject("RegistryEndpoint");
            registryEndpointGo.transform.SetParent(routerGo.transform);
            registryEndpointGo.AddComponent<Gateways.GatewayRouterEndpoint>().Endpoint = Gateways.GatewayRouterEndpointType.Registry;
            registryEndpointGo.AddComponent<Tcp.TcpListener>().Bind(registryPort);

            var sessionEndpointGo = new GameObject("SessionEndpoint");
            sessionEndpointGo.transform.SetParent(routerGo.transform);
            sessionEndpointGo.AddComponent<Gateways.GatewayRouterEndpoint>().Endpoint = Gateways.GatewayRouterEndpointType.Session;
            sessionEndpointGo.AddComponent<Tcp.TcpListener>().Bind(sessionPort);

            // ── 遊戲服務 + Registry ──
            var serverGo = new GameObject("Server");
            var server = serverGo.AddComponent<Server>();
            server.Provider = provider;
            if (server.BinderEvent == null)
            {
                server.BinderEvent = new UnityEngine.Events.UnityEvent<Server.BinderCommand>();
            }
            var soul = new EchoSoul();
            server.BinderEvent.AddListener(command =>
            {
                if (command.Status == Server.BinderCommand.OperatorStatus.Add)
                {
                    command.Binder.Bind<IEchoable>(soul);
                }
            });
            var registry = serverGo.AddComponent<Gateways.GatewayRegistry>();
            registry.Provider = provider;
            registry.Group = 1;
            var registryConnector = serverGo.AddComponent<Tcp.TcpConnector>();

            // ── 客戶端 ──
            var clientGo = new GameObject("GatewayClient");
            var client = clientGo.AddComponent<Gateways.GatewayClient>();
            client.Provider = provider;
            var clientConnector = clientGo.AddComponent<Tcp.TcpConnector>();

            IEchoable echo = null;
            client.Queryer.QueryNotifier<IEchoable>().Supply += e => echo = e;

            // 等待 Start
            yield return null;

            registryConnector.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, registryPort));
            yield return null;
            clientConnector.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, sessionPort));

            for (var i = 0; i < 600 && echo == null; ++i)
            {
                yield return null;
            }
            Assert.NotNull(echo, "客戶端未能透過 Gateway(TCP)取得 IEchoable 服務");

            int? result = null;
            echo.Echo(7).OnValue += v => result = v;
            for (var i = 0; i < 600 && !result.HasValue; ++i)
            {
                yield return null;
            }
            Assert.IsTrue(result.HasValue, "Echo 呼叫未收到回傳值");
            Assert.AreEqual(7, result.Value);

            Object.Destroy(clientGo);
            Object.Destroy(serverGo);
            Object.Destroy(routerGo);
            Object.Destroy(provider);
            yield return null;
        }
    }
}
